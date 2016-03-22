using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Model;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RPCServer
{
    class Program
    {
        private static IConnection _resvConn;      //接收消息的连接
        private static IConnection _senderConn;  //返回结果的连接
        private static IModel _recvChannel;    //接收消息的信道
        private static IModel _sendChannel;    //返回结果的信道
        private static bool isExit = false;

        static void Main(string[] args)
        {
            Setup();
        }

        private static void Setup()
        {
            ConnectionFactory factory = new ConnectionFactory() { 
                HostName = "localhost",
                UserName = "test",
                Password = "test",
                VirtualHost = "test",
                AutomaticRecoveryEnabled = true
            };

            try
            {
                _resvConn = factory.CreateConnection();
                _recvChannel = _resvConn.CreateModel();
                _recvChannel.QueueDeclare("rpcQueue", false, false, false, null);
                _recvChannel.BasicQos(0, 10, false);
                EventingBasicConsumer consumer = new EventingBasicConsumer(_recvChannel);
                consumer.Received += consumer_Received;
                _recvChannel.BasicConsume("rpcQueue", false, consumer);

                _senderConn = factory.CreateConnection();
                _sendChannel = _senderConn.CreateModel();
                _sendChannel.QueueDeclare("rpcReplyQueue", false, false, false, null);
            }
            catch (BrokerUnreachableException ex)
            {
                Console.WriteLine("RabbitMQ服务器尚未启动！");
                Thread.Sleep(2000);
                isExit = true;
            }
            

        }

        static void consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            byte[] body = e.Body;

            Task.Run(() =>
            {
                HandlingMessage(body, e);
            });
        }

        /// <summary>
        /// 消息处理
        /// </summary>
        /// <param name="msgModel"></param>
        /// <param name="e"></param>
        private static async void HandlingMessage(byte[] body, BasicDeliverEventArgs e)
        {
            bool isSuccess = false;
            string message = Encoding.UTF8.GetString(body);
            IModel _senderChannel = _senderConn.CreateModel(); //多线程中每个线程使用独立的信道

            try
            {

                MessageModel msgModel = JsonConvert.DeserializeObject<MessageModel>(message);
                if (msgModel == null || !msgModel.IsVlid())  //解析失败或消息格式不正确，拒绝处理
                {
                    throw new MessageException("消息解析失败");
                }

                Random random = new Random();
                int num = random.Next(0, 4);

                //模拟处理失败
                if (random.Next(0, 11) == 4)
                {
                    throw new Exception("处理失败", null);
                }

                //模拟解析失败
                if (random.Next(0, 11) == 8)
                {
                    throw new MessageException("消息解析失败");
                }

                await Task.Delay(num * 1000);

                //这里简单处理，仅格式化输出消息内容
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " Used: " + num.ToString() + "s MSG:" + msgModel.ToString());

                isSuccess = true;
            }
            catch (MessageException msgEx)
            {
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " ERROR:" + msgEx.Message + " MSG:" + message);
                _recvChannel.BasicReject(e.DeliveryTag, false);  //不再重新分发
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " ERROR:" + ex.Message + " MSG:" + message);
            }

            if (isSuccess)
            {
                try
                {
                    _senderChannel.BasicPublish("", "checkQueue", null, body);  //发送消息到内容检查队列
                    
                    _recvChannel.BasicAck(e.DeliveryTag, false);  //确认处理成功
                }
                catch (AlreadyClosedException acEx)
                {
                    Console.WriteLine("ERROR:连接已关闭");
                }

            }
            else
            {
                _recvChannel.BasicReject(e.DeliveryTag, true); //处理失败，重新分发
            }

            _senderChannel.Close();

        }
    }
}
