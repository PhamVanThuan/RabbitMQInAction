using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PCApp
{
    class Program
    {
        private static IConnection _senderConnection;
        private static IModel _channel;

        static void Main(string[] args)
        {
            Setup();

            Console.WriteLine("Begin to send message:");

            Send();

            //WaitCommand();
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

            _channel = _senderConnection.CreateModel();
            _channel.QueueDeclare("extractQueue", false, false, false, null);
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

        public static void Send()
        {
            string msg = GetMessage();
            byte[] body = Encoding.UTF8.GetBytes(msg);
            _channel.BasicPublish("", "extractQueue", null, body);
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
