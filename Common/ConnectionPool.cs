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

        private static ConnectionConfig _config = new ConnectionConfig()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/"
        };

        private static IConnection[] _connPool = new IConnection[_size];

        private ConnectionPool() { }

        /*static ConnectionPool()
        {
            _factory = new ConnectionFactory()
            {
                HostName = Config.HostName ?? "localhost",
                VirtualHost = Config.VirtualHost ?? "test",
                UserName = Config.UserName ?? "test",
                Password = Config.Password ?? "test",
                RequestedHeartbeat = 60,
                Port = AmqpTcpEndpoint.UseDefaultPort,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };
        }*/

        /// <summary>
        /// 初始化池对象
        /// </summary>
        public static void Init(ConnectionConfig config)
        {
            _config = config ?? _config;

            _factory = new ConnectionFactory()
            {
                HostName = _config.HostName,
                VirtualHost = _config.VirtualHost,
                UserName = _config.UserName,
                Password = _config.Password,
                AutomaticRecoveryEnabled = _config.AutomaticRecoveryEnabled,
                RequestedHeartbeat = 60,
                Port = AmqpTcpEndpoint.UseDefaultPort,
                TopologyRecoveryEnabled = true
            };

            try
            {
                for (int i = 0; i < _size; i++)
                {
                    _connPool[i] = _factory.CreateConnection();
                    if (_config.ShutdownHandler != null)
                    {
                        _connPool[i].ConnectionShutdown += _config.ShutdownHandler;
                    }

                }
            }
            catch (Exception ex)
            {
                //throw;
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

            //如果在初始化的时候，消息服务器没有启动，那么池中的对象都是null，所以这里需要做个判断
            if (_connPool[computedIndex] != null)
            {
                return _connPool[computedIndex];
            }
            else
            {
                Interlocked.CompareExchange(ref _connPool[computedIndex],_factory.CreateConnection(),null);

                return _connPool[computedIndex];
            }
            
        }

        /// <summary>
        /// 绑定shutdown事件处理程序
        /// </summary>
        /// <param name="handler"></param>
        /*public static void BindShutdownEvent(EventHandler<ShutdownEventArgs> handler)
        {
            for (int i = 0; i < _size; i++)
            {
                _connPool[i].ConnectionShutdown += handler;
            }
        }*/

        /// <summary>
        /// 释放连接池内的对象
        /// </summary>
        public static void Release()
        {
            for (int i = 0; i < _size; i++)
            {
                if (_connPool[i]!=null && _connPool[i].IsOpen)
                {
                    _connPool[i].Close();
                }
                _connPool[i] = null;
            }
        }
    }
}
