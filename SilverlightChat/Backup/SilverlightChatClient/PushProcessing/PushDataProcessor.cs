using System;
using System.ServiceModel.Channels;
using System.Runtime.Serialization;


namespace SilverlightChatClient
{
    /// <summary>
    /// Functions called by the PushDataReceiver to handle received messages.
    /// </summary>
    public interface IPushDataProcessor
    {
        void ProcessData(object receivedData);
    }

    /// <summary>
    /// Based on the message 'Type' passed in the Message headers, deserializes Message body to appropriate type
    /// and raises the corresponding event.
    /// </summary>
    public class PushDataProcessor : IPushDataProcessor
    {
        private DataContractSerializer chatDataSerializer = new DataContractSerializer(typeof(ChatData));

        /// <summary>
        /// Event raised when a ChatData Message is received by the PushDataProcessor.
        /// </summary>
        public event Action<ChatData> ProcessChatData;

        public void ProcessData(object receivedData)
        {
            Message receivedMessage = receivedData as Message;
            if (receivedMessage == null)
            {
                throw new ArgumentException("The receivedData must be of type Message.", "receivedData");
            }

            // Check message type
            string type = string.Empty;
            for (int i = 0; i < receivedMessage.Headers.Count; i++)
            {
                if (receivedMessage.Headers[i].Name == "Type")
                {
                    type = receivedMessage.Headers.GetHeader<string>(i);
                    break;
                }
            }
            
            // Dispatch message based on type.
            switch (type)
            {
                case "StayAlive":
                    break;
                case "DataWrapper":
                    ChatData chatData = receivedMessage.GetBody<ChatData>(chatDataSerializer);
                    if (this.ProcessChatData != null)
                    {
                        this.ProcessChatData(chatData);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
