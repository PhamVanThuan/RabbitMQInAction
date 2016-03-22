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

        public RPCClient()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            connection = factory.CreateConnection();
            Init();
        }

        public RPCClient(IConnection conn)
        {
            connection = conn;
            Init();
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

                channel.QueueDeclarePassive("rpcQueue");
                channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea != null && ea.BasicProperties.CorrelationId == corrId)
                    {
                        return Encoding.UTF8.GetString(ea.Body);
                    }
                }
            }
            catch(AlreadyClosedException ex)
            {
                return "Connection has already closed.";
            }
            catch (Exception)
            {
                return "Broken has no rpcQueue.";
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
    }
}
