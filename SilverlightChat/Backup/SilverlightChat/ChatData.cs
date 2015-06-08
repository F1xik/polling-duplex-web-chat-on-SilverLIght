using System.Runtime.Serialization;

namespace SilverlightChatDuplexService
{
    /// <summary>
    /// DataContract that matches the type sent from the server. 
    /// Must be kept in sync with server project.
    /// </summary>
    [DataContract(Name = "ChatData", Namespace = "SilverlightData")]
    public class ChatData
    {
        [DataMember]
        public string IpAddress
        {
            get;
            set;
        }

        [DataMember]
        public string Data
        {
            get;
            set;
        }
    }
}
