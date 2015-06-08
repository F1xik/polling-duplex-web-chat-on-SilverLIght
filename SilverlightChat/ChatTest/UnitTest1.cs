using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SilverlightChatDuplexService;

namespace ChatTest
{
    [TestClass]
    public class WCFServiceTest
    {

        [TestMethod]
        public void SendData_IsCorrect()
        {
            SilverlightChatDuplexService1 service = new SilverlightChatDuplexService1();
            ChatData chatMessage = new ChatData() { Data = "test message" };
            service.SendData(chatMessage);
            Assert.AreEqual(chatMessage.Data, "test message");

        }
        [TestMethod]
        public void GetIpAddress_IsCorrect()
        {
            SilverlightChatDuplexService1 service = new SilverlightChatDuplexService1();
            Assert.AreEqual(service.GetIpAddress(), "<failed to get ip>", false);


        }
        [TestMethod]
        public void StayAliveTimer_IsNull_after_CleanUp_Method()
        {
            SilverlightChatDuplexService1 service = new SilverlightChatDuplexService1();
            service.CleanUp();
            bool check = service.stayAliveTimer == null;
            Assert.IsFalse(!check);

        }

        [TestMethod]
        public void Client_IsNotNull_when_call_StayAlivePing()
        {
            object notUsed = new object();
            SilverlightChatDuplexService1 service = new SilverlightChatDuplexService1();
            service.StayAlivePing(notUsed);
            bool check = service.localClient == null;
            Assert.IsFalse(!check);

        }

        


    }
}
