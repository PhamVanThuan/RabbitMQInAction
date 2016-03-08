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

namespace ContentChecker
{
    class Program
    {
        private static IConnection _senderConn;
        private static IModel _channel;

        static void Main(string[] args)
        {
            AsyncSetup();

            bool isExit = false;
            while (!isExit)
            {
                string line = Console.ReadLine();
                switch (line)
                {
                    case "exit":
                        isExit = true;
                        Close();
                        break;
                    default:
                        break;
                }
            }
        }

        #region 异步消息处理，客户端发送完消息后不再等待

        /// <summary>
        /// 初始化
        /// </summary>
        private static void AsyncSetup()
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = "localhost",
                TopologyRecoveryEnabled = true,
                AutomaticRecoveryEnabled = true
            };

            _senderConn = factory.CreateConnection();
            //_receiverConn = factory.CreateConnection();

            _channel = _senderConn.CreateModel();
            _channel.QueueDeclare("checkQueue", false, false, false, null);
            _channel.BasicQos(0, 10, false);
            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

            consumer.Received += consumer_Received;

            _channel.BasicConsume("checkQueue", false, consumer);
        }

        /// <summary>
        /// 消息接收处理事件，多线程处理消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            byte[] body = e.Body;
            //bool isSuccess = false;

            string message = Encoding.UTF8.GetString(body);
            MessageModel msgModel = JsonConvert.DeserializeObject<MessageModel>(message);

            if (msgModel == null)  //解析失败，消息格式不正确，拒绝处理
            {
                _channel.BasicReject(e.DeliveryTag, false);
            }

            Task.Run(() =>
            {
                HandlingMessage(msgModel, e);
            });

        }

        /// <summary>
        /// 消息处理
        /// </summary>
        /// <param name="msgModel"></param>
        /// <param name="e"></param>
        private static async void HandlingMessage(MessageModel msgModel, BasicDeliverEventArgs e)
        {
            bool isSuccess = false;

            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " ThreadID:" + Thread.CurrentThread.ManagedThreadId.ToString() + " ERROR:" + ex.Message);
            }
            finally
            {
                if (isSuccess)
                {
                    _channel.BasicAck(e.DeliveryTag, false);  //确认处理成功
                }
                else
                {
                    _channel.BasicReject(e.DeliveryTag, true); //处理异常，重新分发
                }
            }


        }

        #endregion

        static void Close()
        {
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
            }

            if (_senderConn != null && _senderConn.IsOpen)
            {
                _senderConn.Close();
            }
        }
    }
}
