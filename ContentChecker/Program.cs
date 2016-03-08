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

namespace ContentChecker
{
    class Program
    {
        private static IConnection _recvConn;
        private static IConnection _senderConn;
        private static IModel _recvChannel;

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
                TopologyRecoveryEnabled = true,
                AutomaticRecoveryEnabled = true
            };

            _recvConn = factory.CreateConnection();
            _recvChannel = _recvConn.CreateModel();
            _recvChannel.QueueDeclare("checkQueue", false, false, false, null);
            _recvChannel.BasicQos(0, 10, false);
            EventingBasicConsumer consumer = new EventingBasicConsumer(_recvChannel);
            consumer.Received += consumer_Received;
            _recvChannel.BasicConsume("checkQueue", false, consumer);

            _senderConn = factory.CreateConnection();
            var channel = _senderConn.CreateModel();
            channel.QueueDeclare("reportQueue", false, false, false, null);
            channel.Close();
        }

        /// <summary>
        /// 等待接收指令
        /// </summary>
        private static void WaitCommand()
        {
            bool isExit = false;

            while (!isExit)
            {
                string line = Console.ReadLine().ToLower().Trim();
                switch (line)
                {
                    case "exit":
                        Close();
                        isExit = true;
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

                //模拟异常
                if (random.Next(0, 11) == 4)
                {
                    throw new Exception("处理失败", null);
                }

                await Task.Delay(num * 1000);

                //这里简单处理，仅格式化输出消息内容
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " MSG:" + msgModel.ToString());

                isSuccess = true;
            }
            catch (MessageException msgEx)
            {
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " ERROR:" + msgEx.Message);
                _recvChannel.BasicReject(e.DeliveryTag, false);  //不再重新分发
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " ERROR:" + ex.Message);
            }

            if (isSuccess)
            {
                _senderChannel.BasicPublish("", "reportQueue", null, body);  //发送消息到内容检查队列
                _recvChannel.BasicAck(e.DeliveryTag, false);  //确认处理成功
            }
            else
            {
                _recvChannel.BasicReject(e.DeliveryTag, true); //处理失败，重新分发
            }

            _senderChannel.Close();


        }

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
