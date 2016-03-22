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

namespace ReportBuilder
{
    class Program
    {
        private static IConnection _recvConn;
        private static IModel _recvChannel;
        private static bool isExit = false;
        //private static IConnection _receiverConn; //同步处理（RPC）时使用

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
                TopologyRecoveryEnabled = true,
                AutomaticRecoveryEnabled = true
            };

            try
            {
                _recvConn = factory.CreateConnection();
                _recvConn.ConnectionShutdown += ConnectionShutdown;
                _recvChannel = _recvConn.CreateModel();
                _recvChannel.QueueDeclare("reportQueue", false, false, false, null);
                _recvChannel.BasicQos(0, 10, false);
                EventingBasicConsumer consumer = new EventingBasicConsumer(_recvChannel);
                consumer.Received += consumer_Received;
                _recvChannel.BasicConsume("reportQueue", false, consumer);
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

            Task.Run(() => {
                HandlingMessage(body, e);
            });

            //try
            //{
            //    string message = Encoding.UTF8.GetString(body);
            //    MessageModel msgModel = JsonConvert.DeserializeObject<MessageModel>(message);

            //    await Task.Run(() => {
            //        HandlingMessage(msgModel);
            //    });

            //    isSuccess = true;
            //}
            //catch (Exception ex)
            //{
            //    isSuccess = false;
            //    Console.WriteLine(ex.Message);
            //}
            //finally
            //{
            //    if (isSuccess)
            //    {
            //        _channel.BasicAck(e.DeliveryTag, false);  //确认
            //    }
            //    else
            //    {
            //        _channel.BasicReject(e.DeliveryTag, true); //重新分发
            //    }
            //}

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
                if(random.Next(0,11) == 8)
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

            
            
        }

        #endregion

        static void Close()
        {
            if(_recvChannel!=null && _recvChannel.IsOpen)
            {
                _recvChannel.Close();
            }

            if(_recvConn!=null && _recvConn.IsOpen)
            {
                _recvConn.Close();
            }
        }


    }
}
