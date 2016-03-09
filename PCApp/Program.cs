using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace PCApp
{
    class Program
    {
        private static IConnection _senderConnection;
        private static IModel _channel;
        private static int _interval = 1; //消息发送间隔

        static void Main(string[] args)
        {
            Setup();

            Console.WriteLine("Ready to send message:");

            Send();

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
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _senderConnection = factory.CreateConnection();
            _senderConnection.ConnectionShutdown += _senderConnection_ConnectionShutdown;

            _channel = _senderConnection.CreateModel();
            _channel.QueueDeclare("extractQueue", false, false, false, null);
        }

        static void _senderConnection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("Connection has already closed. " + e.ReplyText);
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
                string[] arr = line.Split(new char[] { ' ' });
                string cmd = arr[0];
                switch (cmd)
                {
                    case "exit":
                        Close();
                        isExit = true;
                        break;
                    case "go":
                        int count = 10;
                        if (arr.Length > 1)
                        {
                            int.TryParse(arr[1], out count);
                        }

                        Send(count);
                        break;
                    case "interval":
                        int.TryParse(arr[1], out _interval);
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

        public static void Send(int msgCount = 10)
        {
            //string msg = GetMessage();
            //byte[] body = Encoding.UTF8.GetBytes(msg);
            //_channel.BasicPublish("", "extractQueue", null, body);

            Console.WriteLine("---------- start to send ------------");

            for (int i = 1; i <= msgCount; i++)
            {
                string title = "Test Document" + i.ToString();
                string author = "zjh" + i.ToString();
                int docType = i % 2 + 1;
                string jsonFormat = "{{\"Title\":\"{0}\",\"Author\":\"{1}\",\"DocType\":{2}}}";
                string message = string.Format(jsonFormat, title, author, docType.ToString());
                byte[] body = Encoding.UTF8.GetBytes(message);
                try
                {
                    _channel.BasicPublish("", "extractQueue", null, body);
                }
                catch (AlreadyClosedException ex)
                {
                    Console.WriteLine("ERROR: " + ex.Message);
                    break;
                }
                
                Console.WriteLine("Time:" + DateTime.Now.ToString() + " MSG:" + title);

                if (_interval > 0)
                {
                    Thread.Sleep(_interval * 1000);
                }

            }

            Console.WriteLine("---------- finish ------------");

        }

        private static string GetMessage()
        {
            string argLine = string.Join(" ", Environment.GetCommandLineArgs());
            string args = argLine.Substring(argLine.IndexOf(" ") + 1);
            Console.WriteLine("args:" + args);
            string[] arr = args.Split(new char[] { ',' });
            string jsonFormat = "{{\"Title\":\"{0}\",\"Author\":\"{1}\",\"DocType\":{2}}}";

            return string.Format(jsonFormat, arr[0], arr[1], arr[2]);
        }

        private static void Close()
        {
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
            }

            if (_senderConnection != null && _senderConnection.IsOpen)
            {
                _senderConnection.Close();
            }
        }


    }
}
