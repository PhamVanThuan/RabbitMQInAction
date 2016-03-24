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
        private static IConnection _recvConn;      //接收消息的连接
        private static IConnection _senderConn;  //返回结果的连接
        private static IModel _recvChannel;    //接收消息的信道
        private static IModel _sendChannel;    //返回结果的信道
        private static bool isExit = false;

        static void Main(string[] args)
        {
            Setup();

            Console.WriteLine("Begin to consume RPC message:");

            WaitCommand();
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
                _recvConn = factory.CreateConnection();
                _recvChannel = _recvConn.CreateModel();
                _recvChannel.QueueDeclare("rpcQueue", false, false, false, null);
                _recvChannel.BasicQos(0, 10, false);
                EventingBasicConsumer consumer = new EventingBasicConsumer(_recvChannel);
                consumer.Received += consumer_Received;
                _recvChannel.BasicConsume("rpcQueue", false, consumer);

                _senderConn = factory.CreateConnection();
                //_sendChannel = _senderConn.CreateModel();
            }
            catch (BrokerUnreachableException ex)
            {
                Console.WriteLine("RabbitMQ服务器尚未启动！");
                Thread.Sleep(2000);
                isExit = true;
            }
            

        }

        /// <summary>
        /// 等待接收指令
        /// </summary>
        private static void WaitCommand()
        {
            while (!isExit)
            {
                string line = Console.ReadLine().ToLower().Trim();
                switch (line)
                {
                    case "exit":
                        Close();
                        isExit = true;
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    default:
                        break;
                }
            }

            Console.WriteLine("Goodbye!");
        }

        static void Close()
        {
            if (_recvChannel != null && _recvChannel.IsOpen)
            {
                _recvChannel.Close();
            }

            if (_recvConn != null && _recvConn.IsOpen)
            {
                _recvConn.Close();
            }

            if (_senderConn != null && _senderConn.IsOpen)
            {
                _senderConn.Close();
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
            bool hasRejected = false;
            string message = Encoding.UTF8.GetString(body);
            string replyMsg = "";
            IModel _senderChannel = null;
            

            try
            {
                _senderChannel = _senderConn.CreateModel(); //多线程中每个线程使用独立的信道
                replyMsg = message + "   处理成功";

                Random random = new Random();
                int num = random.Next(0, 4);

                //模拟处理失败
                /*if (random.Next(0, 11) == 4)
                {
                    throw new Exception("处理失败", null);
                }*/

                //模拟解析失败
                if (random.Next(0, 11) == 8)
                {
                    throw new MessageException("消息解析失败");
                }

                //await Task.Delay(num * 1000);   //模拟消息处理

                //这里简单处理，仅格式化输出消息内容
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " Used: " + num.ToString() + "s MSG:" + message);

                isSuccess = true;
            }
            catch (MessageException msgEx)
            {
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " ERROR:" + msgEx.Message + " MSG:" + message);
                _recvChannel.BasicReject(e.DeliveryTag, false);  //不再重新分发
                hasRejected = true;
                replyMsg = message + "解析失败";
                isSuccess = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " ERROR:" + ex.Message + " MSG:" + message);
                replyMsg = "处理失败";
            }

            if (isSuccess)
            {
                try
                {
                    var props = e.BasicProperties;
                    var replyProps = _senderChannel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;
                    _senderChannel.BasicPublish("", e.BasicProperties.ReplyTo, replyProps, Encoding.UTF8.GetBytes(replyMsg));  //发送消息到内容检查队列
                    if(!hasRejected)
                    {
                        _recvChannel.BasicAck(e.DeliveryTag, false);  //确认处理成功  此处与不再重新分发，只能出现一次
                    }
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
