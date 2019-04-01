using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using R2A.ReportApi.Client.Common;
using R2A.ReportApi.Models;
using RabbitMQ.Client;


namespace R2A.ReportApi.Client.Infrastructure
{
    public class MessageQueueService
    {
        
        private readonly Settings _settings;
        private readonly ConnectionFactory _factory;

        public MessageQueueService(Settings settings, ConnectionFactory factory)
        {
            _settings = settings;
            _factory = factory;
        }

        public void SendMessage(Dictionary<string, string> headers, string message)
        {
            
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange:_settings.MqExchange, type:"direct" , durable: true, autoDelete: false, arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Headers = MessageHeaders.ToRabbitMqHeaders(headers);

                channel.BasicPublish(exchange: _settings.MqExchange, routingKey: _settings.MqRoutingId, basicProperties: properties, body: body);

            }

        }
    }
}
