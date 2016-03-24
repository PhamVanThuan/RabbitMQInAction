using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Common;
using RabbitMQ.Client;

namespace WebApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        /// <summary>
        /// 消息发送连接
        /// </summary>
        private IConnection _sendConnection;

        /// <summary>
        /// 消息接收连接
        /// </summary>
        private IConnection _receiveConnection;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //在应用启动的时候建立两个独立的连接
            ConnectToBroker();
        }

        /// <summary>
        /// 应用程序回收时断开连接
        /// </summary>
        protected void Application_End()
        {
            /*if(_sendConnection!=null && _sendConnection.IsOpen)
            {
                _sendConnection.Close();
            }

            if(_receiveConnection!=null && _receiveConnection.IsOpen)
            {
                _receiveConnection.Close();
            }*/

            //释放连接池中的所有对象
            ConnectionPool.Release();
        }

        private void ConnectToBroker()
        {
            /*var factory = new ConnectionFactory() { HostName = "localhost", AutomaticRecoveryEnabled = true, TopologyRecoveryEnabled = true };
            _sendConnection = factory.CreateConnection();
            _receiveConnection = factory.CreateConnection();

            _sendConnection.ConnectionShutdown += ConnectionShutdownEventHandler;
            _receiveConnection.ConnectionShutdown += ConnectionShutdownEventHandler;*/

            ConnectionConfig config = new ConnectionConfig()
            {
                HostName = "localhost",
                UserName = "test",
                Password = "test",
                VirtualHost = "test",
                AutomaticRecoveryEnabled = true,
                ShutdownHandler = ConnectionShutdownEventHandler
            };

            ConnectionPool.Init(config);

        }

        void ConnectionShutdownEventHandler(object sender, ShutdownEventArgs e)
        {
            string shutDownInfo = e.ReplyText;

            ////这里可以记录连接断开的原因
        }

        
    }
}
