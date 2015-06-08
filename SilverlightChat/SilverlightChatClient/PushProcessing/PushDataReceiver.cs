using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Diagnostics;

namespace SilverlightChatClient
{

    public class PushDataReceiver
    {
        SynchronizationContext uiThread = null;
        public IPushDataProcessor Client { get; set; }
        public string ServiceUrl { get; set; }
        public Message InitializeMessage
        {
            get;
            private set;
        }

        private IDuplexChannel channel;

        public PushDataReceiver(IPushDataProcessor client, string url, string action, string actionData)
        {
            this.InitializeMessage = Message.CreateMessage(MessageVersion.Soap11, action, actionData);

            this.Client = client;
            this.ServiceUrl = url;
            this.uiThread = SynchronizationContext.Current;
        }

        public PushDataReceiver(IPushDataProcessor client, string url, Message message)
        {
            this.Client = client;
            this.ServiceUrl = url;
            this.InitializeMessage = message;
            this.uiThread = SynchronizationContext.Current;
        }

        public MessageVersion MessageVersion
        {
            get
            {
                return this.channel.GetProperty<MessageVersion>();
            }
        }

        public void Start()
        {
            // Instantiate the binding and set the time-outs
            PollingDuplexHttpBinding binding = new PollingDuplexHttpBinding()
            {
                // InactivityTimeout = TimeSpan.FromMinutes(10) // this is the default.
            };

            // Instantiate and open channel factory from binding
            IChannelFactory<IDuplexSessionChannel> factory =
                binding.BuildChannelFactory<IDuplexSessionChannel>(new BindingParameterCollection());

            IAsyncResult factoryOpenResult =
                factory.BeginOpen(new AsyncCallback(OnOpenCompleteFactory), factory);
            if (factoryOpenResult.CompletedSynchronously)
            {
                this.CompleteOpenFactory(factoryOpenResult);
            }
        }

        void OnOpenCompleteFactory(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
                return;
            else
                this.CompleteOpenFactory(result);
        }

        void CompleteOpenFactory(IAsyncResult result)
        {
            IChannelFactory<IDuplexSessionChannel> factory =
                (IChannelFactory<IDuplexSessionChannel>)result.AsyncState;

            factory.EndOpen(result);

            // The factory is now open. Create and open a channel from the channel factory.
            this.channel = factory.CreateChannel(new EndpointAddress(ServiceUrl));

            IAsyncResult channelOpenResult =
                this.channel.BeginOpen(new AsyncCallback(OnOpenCompleteChannel), this.channel);
            if (channelOpenResult.CompletedSynchronously)
            {
                this.CompleteOpenChannel(channelOpenResult);
            }
        }

        void OnOpenCompleteChannel(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
                return;
            else
                this.CompleteOpenChannel(result);
        }

        void CompleteOpenChannel(IAsyncResult result)
        {
            IDuplexSessionChannel channel = (IDuplexSessionChannel)result.AsyncState;
            channel.EndOpen(result);

            this.Send(this.InitializeMessage);

            //Start listening for callbacks from the service
            this.ReceiveLoop(channel);
        }

        public void Send(Message message)
        {
            // Channel is now open. Send message            
            IAsyncResult resultChannel =
                this.channel.BeginSend(message, new AsyncCallback(OnSend), this.channel);

            if (resultChannel.CompletedSynchronously)
            {
                this.CompleteOnSend(resultChannel);
            }
        }

        void OnSend(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
                return;
            else
                this.CompleteOnSend(result);
        }

        void CompleteOnSend(IAsyncResult result)
        {
            try
            {
                IDuplexSessionChannel channel = (IDuplexSessionChannel)result.AsyncState;
                channel.EndSend(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        void ReceiveLoop(IDuplexSessionChannel channel)
        {
            // Start listening for callbacks.
            if (channel.State == CommunicationState.Opened)
            {
                IAsyncResult result = channel.BeginReceive(new AsyncCallback(OnReceiveComplete), channel);
                if (result.CompletedSynchronously) CompleteReceive(result);
            }
        }

        void OnReceiveComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
                return;
            else
                this.CompleteReceive(result);
        }

        void CompleteReceive(IAsyncResult result)
        {
            // A callback was received so process data
            IDuplexSessionChannel channel = (IDuplexSessionChannel)result.AsyncState;

            try
            {
                Message receivedMessage = channel.EndReceive(result);

                // Show the service response in the UI.
                if (receivedMessage != null)
                {
                    this.uiThread.Post(this.Client.ProcessData, receivedMessage);   // Run on Ui Thread.
                }

                this.ReceiveLoop(channel);
            }
            catch (CommunicationObjectFaultedException exp)
            {
                Debug.WriteLine(exp.ToString());
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                this.uiThread.Post(delegate(object msg) { System.Windows.Browser.HtmlPage.Window.Alert(msg.ToString()); }, exp.Message);
            }
            catch (TimeoutException texp)
            {
                Debug.WriteLine(texp.ToString());
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                this.uiThread.Post(delegate(object msg) { System.Windows.Browser.HtmlPage.Window.Alert(msg.ToString()); }, texp.Message);
            }
        }

        public void Close()
        {
            // this.channel.Close();
            IAsyncResult result = this.channel.BeginClose(new AsyncCallback(OnCloseChannel), this.channel);
            if (result.CompletedSynchronously)
            {
                this.CompleteCloseChannel(result);
            }
        }

        void OnCloseChannel(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
                return;
            else
                this.CompleteCloseChannel(result);
        }

        void CompleteCloseChannel(IAsyncResult result)
        {
            try
            {
                IDuplexSessionChannel channel = (IDuplexSessionChannel)result.AsyncState;
                channel.EndClose(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }
}
