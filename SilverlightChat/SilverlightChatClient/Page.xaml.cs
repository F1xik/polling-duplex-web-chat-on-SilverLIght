using System;
using System.Windows;
using System.Windows.Controls;
using System.ServiceModel.Channels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace SilverlightChatClient
{
    public partial class Page : UserControl
    {
        PushDataReceiver pusher;
        PushDataProcessor processor = new PushDataProcessor();
        ObservableCollection<ChatData> messages = new ObservableCollection<ChatData>();
        bool messagesUpdated;

        public Page()
        {
            InitializeComponent();
            this.listChat.ItemsSource = this.messages;
            this.listChat.LayoutUpdated += new EventHandler(listChat_LayoutUpdated);
            this.Loaded += new RoutedEventHandler(Page_Loaded);
            this.processor.ProcessChatData += this.AddChatMessage;

        }

        void listChat_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.messagesUpdated)
            {
                // Scroll the message box to the newest chat message upon msg arrival.
                this.listChat.ScrollIntoView(this.listChat.Items[this.listChat.Items.Count - 1]);
                this.messagesUpdated = false;
            }
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Start PushDataReceiver.

            this.pusher = new PushDataReceiver(
                this.processor,
                "http://localhost:5433/SilverlightChatDuplexService.svc",
                "Silverlight/ISilverlightChatDuplexService/InitiateDuplex",    // The Wcf function or action.
                "");

            try
            {
                this.pusher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                // Add the exception information as a chat message to help debugging.
                this.messages.Add(
                   new ChatData()
                   {
                       Data = "Could not connect to PollingDuplex Service." + Environment.NewLine + ex.ToString()
                   });
            }
        }

        /// <summary>
        /// Sends a ChatData to the ChatServer, to be broadcast to all listeners.
        /// </summary>
        /// <param name="dw">The chat message to be sent.</param>
        private void Send(ChatData chatMessage)
        {
            Message m = Message.CreateMessage(
                MessageVersion.Soap11,
                "Silverlight/ISilverlightChatDuplexService/SendMessage",
                chatMessage,
                new DataContractSerializer(typeof(ChatData)));

            this.pusher.Send(m);
        }

        private void AddChatMessage(ChatData receivedData)
        {
            this.messages.Add(receivedData);

            // Used to notify the messageBox.LayoutUpdated event listener to scroll to the newest message.
            this.messagesUpdated = true;
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string msg = this.txtName.Text.Trim()+" :"+this.txtText.Text.Trim();

                ChatData chatMessage = new ChatData() { Data = msg };
                this.Send(chatMessage);

                
        }
    }
}
