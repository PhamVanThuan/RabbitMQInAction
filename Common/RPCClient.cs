using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Common
{
    public class RPCClient
    {
        private IConnection connection;
        private IModel recvChannel;
        private string replyQueueName;
        private QueueingBasicConsumer consumer;
        private static readonly object _lockObj = new object();

        private static RPCClient Current;

        /*public RPCClient()
        {
            var factory = new ConnectionFactory() {
                HostName = "localhost" 
            };

            connection = factory.CreateConnection();
            Init();
        }*/

        private RPCClient(IConnection conn)
        {
            connection = conn;
            Init();
        }

        public static RPCClient GetInstance()
        {
            //对象实例化以后，避免每次都加锁
            if (Current == null)
            {
                lock (_lockObj)
                {
                    //防止多次实例化
                    if (Current == null)
                    {
                        Current = new RPCClient(ConnectionPool.GetConnection());
                    }
                }
            }

            return Current;
        }

        private void Init()
        {
            recvChannel = connection.CreateModel();
            replyQueueName = recvChannel.QueueDeclare().QueueName;
            consumer = new QueueingBasicConsumer(recvChannel);
            recvChannel.BasicConsume(queue: replyQueueName, noAck: true, consumer: consumer);
        }

        public string Call(string msg)
        {
            IModel channel = null;
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(msg);
                channel = connection.CreateModel();
                var corrId = Guid.NewGuid().ToString();
                var props = channel.CreateBasicProperties();
                props.ReplyTo = replyQueueName;
                props.CorrelationId = corrId;

                channel.QueueDeclarePassive("rpcQueue"); //判断broken中是否已创建该队列
                channel.BasicPublish(exchange: "", routingKey: "rpcQueue", basicProperties: props, body: messageBytes);

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea != null && ea.BasicProperties.CorrelationId == corrId)  //共享接收队列，判断关联Id
                    {
                        return Encoding.UTF8.GetString(ea.Body);
                    }
                }
            }
            catch (OperationInterruptedException)
            {
                throw new NoRPCConsumeException("Broken has no rpcQueue.");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if(channel!=null)
                {
                    channel.Close();
                }
            }
        }

        public async Task<string> CallAsync(string msg)
        {
            return await Task.Run<string>(() => {
                return Call(msg);
            });
        }

        public void Close()
        {
            if(recvChannel!=null && recvChannel.IsOpen)
            {
                recvChannel.Close();
            }

            //connection是连接池对象，这里不需要关闭
            /*if (connection!=null && connection.IsOpen)
            {
                connection.Close();
            }*/
        }
    }
}
