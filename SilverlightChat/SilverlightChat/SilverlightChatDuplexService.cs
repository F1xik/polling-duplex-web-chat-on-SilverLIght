using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.ServiceModel.Channels;
using System.ServiceModel.Activation;
using System.Diagnostics;
using System.Web.UI.MobileControls;

namespace SilverlightChatDuplexService
{
    // NOTE: If you change the class name "SilverlightChatDuplexService" here, you must also update the reference to "SilverlightChatDuplexService" in Web.config.
    // [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    /// <summary>
    /// </summary>
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,
        ConcurrencyMode = ConcurrencyMode.Single,
        AutomaticSessionShutdown = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class SilverlightChatDuplexService1 : ISilverlightChatDuplexService
    {
        /// <summary>
        /// The list of connected clients. When a timeout occurs, they are removed from the client list.
        /// </summary>
        public static List<IDuplexClient> clients = new List<IDuplexClient>();
        public IDuplexClient localClient;
        private object lockObject = new object();
        public Timer stayAliveTimer;    

        private bool hasIp;
        public string ipAddress;
        private readonly DataContractSerializer serializer = new DataContractSerializer(typeof(ChatData));

        public SilverlightChatDuplexService1()
        {
            Debug.WriteLine("Created SilverlightChatDuplexService at: " + DateTime.Now.ToString());

            // Prevent inactivity timeout from occurring, allowing clients to idle in the chat room.
            this.stayAliveTimer = new Timer(StayAlivePing, null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); 
        }
                
        public void InitiateDuplex(Message receivedMessage)
        {
            Debug.WriteLine(receivedMessage.ToString());
            lock (clients)
            {
                localClient = OperationContext.Current.GetCallbackChannel<IDuplexClient>();
                clients.Add(localClient);
            }
        }

        public void SendMessage(Message message)
        {
            ChatData toSend = message.GetBody<ChatData>();  // Deserialize the message into a string.
            Debug.WriteLine("Message to push to clients: " + toSend.Data);
            if (!this.hasIp)
            {
                this.ipAddress = GetIpAddress();
            }

            toSend.IpAddress = this.ipAddress;
            this.SendData(toSend);
        }

        public string GetIpAddress()
        {
            try
            {
                OperationContext context = OperationContext.Current;
                MessageProperties properties = context.IncomingMessageProperties;
                RemoteEndpointMessageProperty endpoint = properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.hasIp = true;
                return endpoint.Address;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return "<failed to get ip>";
            }
        }

        /// <summary>
        /// Ping packet used to offset the inactivity timeout.
        /// </summary>
        /// <param name="notUsed">Not Used.</param>
        public void StayAlivePing(object notUsed)
        {
            lock (lockObject)
            {
                Message gameDataMsg = Message.CreateMessage(
                    MessageVersion.Soap11,
                    "Silverlight/ISilverlightChatDuplexService/Receive",
                    "stayalive");
                gameDataMsg.Properties.Add("Type", "StayAlive");

                try
                {
                    if (this.localClient != null)
                    {
                        this.localClient.BeginReceive(gameDataMsg, EndSend, this.localClient);
                    }                    
                }
                catch (Exception ex)
                {
                    // If an exception is thrown, remove this client from the static client list.
                    // This can happen if a client just closes the browser.
                    Debug.WriteLine(ex.ToString());
                    this.SafeRemove(this.localClient);
                    this.localClient = null;
                }
            }
        }

        public void SendData(ChatData data)
        {
            Debug.WriteLine("Sending Data from thread: " + Thread.CurrentThread.ManagedThreadId);
            List<IDuplexClient> clientsToRemove = new List<IDuplexClient>();
            lock (clients)
            {
                foreach (IDuplexClient client in clients)
                {
                    try
                    {
                        //Send data to the client
                        if (client != null)
                        {
                            Message gameDataMsg = Message.CreateMessage(
                                MessageVersion.Soap11,
                                "Silverlight/ISilverlightChatDuplexService/Receive",
                                data,
                                this.serializer);

                            gameDataMsg.Headers.Add(MessageHeader.CreateHeader("Type", "", "DataWrapper"));                            
                            client.BeginReceive(gameDataMsg, EndSend, client);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Exception caught when trying to send message to client so remove them from client list.
                        // Should probably catch a more specific exception but I'll leave that as an exercise for the reader.
                        Debug.WriteLine(ex.ToString());
                        clientsToRemove.Add(client);
                    }
                }

                foreach (IDuplexClient client in clientsToRemove)
                {
                    clients.Remove(client);
                }
            }
        }

        public void EndSend(IAsyncResult result)
        {
            IDuplexClient client = result.AsyncState as IDuplexClient;
            try
            {
                client.EndReceive(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                this.SafeRemove(client);
            }
        }

        private void SafeRemove(IDuplexClient client)
        {
            lock (clients)
            {
                clients.Remove(client);
            }
        }

        public void CleanUp()
        {
            if (this.stayAliveTimer != null)
            {
                this.stayAliveTimer.Dispose();
                this.stayAliveTimer = null;
            }

            // this.client = null;
        }
    }
}
