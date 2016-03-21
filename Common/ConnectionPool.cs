using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Common
{
    /// <summary>
    /// 连接池，因为RabbitMQ的连接对象可以共用，所以从池中获取对象的时候仅仅只是返回连接对象的引用
    /// </summary>
    public class ConnectionPool
    {
        private static int _index = -1;

        private static ConnectionFactory _factory;

        public static int _size = 20;  //默认创建20条TCP连接

        public static ConnectionConfig Config;

        private static IConnection[] _connPool = new IConnection[_size];

        private ConnectionPool() { }

        static ConnectionPool()
        {
            _factory = new ConnectionFactory()
            {
                HostName = Config.HostName ?? "localhost",
                VirtualHost = Config.VirtualHost ?? "/",
                UserName = Config.UserName ?? "guest",
                Password = Config.Password ?? "guest",
                RequestedHeartbeat = 60,
                Port = AmqpTcpEndpoint.UseDefaultPort,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };
        }

        /// <summary>
        /// 初始化池对象
        /// </summary>
        public static void Init()
        {
            for (int i = 0; i < _size; i++)
            {
                _connPool[i] = _factory.CreateConnection();
            }
        }

        /// <summary>
        /// 从池中获取连接
        /// </summary>
        /// <returns></returns>
        public static IConnection GetConnection()
        {
            int index, computedIndex;
            do
            {
                index = _index;
                computedIndex = (index + 1) % _size;
            } while (_index != Interlocked.CompareExchange(ref _index, computedIndex, index));
            return _connPool[computedIndex];
        }
    }
}
