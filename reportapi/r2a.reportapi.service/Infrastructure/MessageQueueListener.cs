using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace R2A.ReportApi.Service.Infrastructure
{
    public abstract class MessageQueueListener : INotificationHandler<ServiceStartNotification>
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly string _exchange;
        private readonly string _routingId;
        private readonly string _queueName;
        private readonly ILogger _logger;

        protected MessageQueueListener(ConnectionFactory connectionFactory, ILogFactory logFactory, string exchange, string routingId, string queueName)
        {
            _connectionFactory = connectionFactory;
            _exchange = exchange;
            _routingId = routingId;
            _queueName = queueName;
            _logger = logFactory.GetLogger<MessageQueueListener>();
        }

        public Task Handle(ServiceStartNotification notification, CancellationToken cancellationToken)
        {
            var connection = _connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(queue: _queueName, exchange: _exchange, routingKey:_routingId);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            cancellationToken.Register(() =>
            {
                _logger.Info($"[{_routingId}]: Closing connection.");
                channel.Dispose();
                connection.Dispose();
            });

            _logger.Info($"[{_routingId}]: Listening on queue.");

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                

                _logger.Info($"[{_routingId}]: Received a message.");
                try
                {
                    await this.OnMessageReceived(model, ea, cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.Error(e,
                        $"[{_routingId}]: The message processor encountered an error. The message will be discarded.");
                }

                try
                {
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch(Exception e)
                {
                    _logger.Error(e, $"[{_routingId}]: An error occured trying to acknowledge the message.");
                }
            };
            channel.BasicConsume(queue: _queueName,
                autoAck: false,
                consumer: consumer);
            
            return Task.CompletedTask;
        }

        protected abstract Task OnMessageReceived(object model, BasicDeliverEventArgs ea, CancellationToken cancellationToken);
    }
}
