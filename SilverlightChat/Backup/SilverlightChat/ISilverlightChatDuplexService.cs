using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

//add using
using System.ServiceModel.Channels;

namespace SilverlightChatDuplexService
{

    [ServiceContract(Namespace = "Silverlight",
                    CallbackContract = typeof(IDuplexClient), SessionMode = SessionMode.Required)]
    public interface ISilverlightChatDuplexService
    {
        [OperationContract(IsOneWay = true)]
        void InitiateDuplex(Message receivedMessage);

        [OperationContract(IsOneWay = true)]
        void SendMessage(Message message);
    }


    [ServiceContract]
    public interface IDuplexClient
    {
        [OperationContract(IsOneWay = true, AsyncPattern = true)]
        IAsyncResult BeginReceive(Message message, AsyncCallback callback, object state);

        void EndReceive(IAsyncResult result);
    }




   
}
