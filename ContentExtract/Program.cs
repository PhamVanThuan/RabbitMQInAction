using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Common;

namespace ContentExtract
{
    class Program
    {
        private static IConnection _senderConn;
        private static IConnection _recvConn;
        //private static IModel _senderChannel; 多线程情况下，每个线程需要独立的channel来发送消息
        private static IModel _recvChannel;
        private static bool isExit = false;
        

        static void Main(string[] args)
        {
            Setup();

            Console.WriteLine("Begin to consume message:");

            WaitCommand();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private static void Setup()
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "test",
                Password = "test",
                VirtualHost = "test",
                //TopologyRecoveryEnabled = true,   //默认为true，如果设置为false，则重连后不会重建相关实体，如：exchange,queue,binding
                AutomaticRecoveryEnabled = true     //自动重连
            };

            try
            {
                _recvConn = factory.CreateConnection();
                _recvConn.ConnectionShutdown += ConnectionShutdown;
                _recvChannel = _recvConn.CreateModel();
                _recvChannel.QueueDeclare("extractQueue", false, false, false, null);
                _recvChannel.BasicQos(0, 10, false);
                EventingBasicConsumer consumer = new EventingBasicConsumer(_recvChannel);
                consumer.Received += consumer_Received;
                _recvChannel.BasicConsume("extractQueue", false, consumer);

                _senderConn = factory.CreateConnection();
                var channel = _senderConn.CreateModel();
                channel.QueueDeclare("checkQueue", false, false, false, null);
                //channel.Close();   //这里如果关闭channel的话，自动重连的时候无法恢复checkQueue队列，因为checkQueue是使用channel创建的，恢复的时候还要使用channel，必须保持该信道不关闭
            }
            catch (BrokerUnreachableException ex)
            {
                Console.WriteLine("ERROR: RabbitMQ服务器未启动！");
                Thread.Sleep(2000);
                isExit = true;
            }

        }

        static void ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("Connection has already closed.");
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

        #region 异步消息处理，客户端发送完消息后不再等待

        /// <summary>
        /// 消息接收处理事件，多线程处理消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            byte[] body = e.Body;
            //bool isSuccess = false;

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
            catch(MessageException msgEx)
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


        #endregion

        #region 同步消息处理(RPC)

        #endregion

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
    }
}
