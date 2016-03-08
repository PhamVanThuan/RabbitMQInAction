using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;
using Newtonsoft.Json;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ReportReceivedConsumer_Test()
        {
            MessageModel msgModel = new MessageModel { Title = "This is a test document", Author = "author", DocType = DocumentType.Journal };
            String msg = GetMessage(msgModel);

            Console.WriteLine("Received message : " + msg);
        }

        /// <summary>
        /// 序列化消息实体
        /// </summary>
        /// <param name="msgModel"></param>
        /// <returns></returns>
        private string GetMessage(MessageModel msgModel)
        {
            return JsonConvert.SerializeObject(msgModel);
        }

        /// <summary>
        /// 反序列化消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private MessageModel GetMsgModel(string msg)
        {
            return JsonConvert.DeserializeObject<MessageModel>(msg);
        }

        [TestMethod]
        public void MsgModelToString_Test()
        {
            MessageModel msgModel = new MessageModel { Title = "This is a test document", Author = "author", DocType = DocumentType.Journal };
            string s = msgModel.ToString();
            string ss = "";
        }
    }
}
