using Backoffice.Shared.Models;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace Backoffice.Shared.Utilities
{
    public class MessageBroker : IDisposable, IMessageBroker
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private EventingBasicConsumer _consumer;
        public event EventHandler<BasicDeliverEventArgs> MessageReceived;

        private readonly IConfiguration _configuration;

        private string _hostName = string.Empty;
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _virtualHost = string.Empty;
        private string _exchange = string.Empty;
        private string _queue = string.Empty;
        private string _consumerName = string.Empty;
        private string _routingKey = string.Empty;
        private string _prefetchCount = string.Empty;
        private bool _isExclusive = false;
        private bool _isPriority = false;

        private ConnectionFactory _externalConnectionFactory;
        private IConnection _externalConnection;
        private IModel _externalModel;
        private IModel _modelHk;
        private IModel _modelSa;

        private readonly string _messageBrokerName;

        private string _externalHostName = string.Empty;
        private string _externalUserName = string.Empty;
        private string _externalPassword = string.Empty;
        private string _externalVirtualHost = string.Empty;
        private string _externalExchange = string.Empty;
        private string _externalQueue = string.Empty;
        private string _externalConsumerName = string.Empty;
        private string _externalRoutingKey = string.Empty;

        private Dictionary<string, MessageBrokerCredentialModel> _credentials { get; set; }

        public MessageBroker(IConfiguration configuration)
        {
            _messageBrokerName = $"MessageBroker";
            _configuration = configuration;
            _credentials = new Dictionary<string, MessageBrokerCredentialModel>();
            _credentials.Add("HK", configuration.GetSection("MessageBroker:HK").Get<MessageBrokerCredentialModel>());
            _credentials.Add("SA", configuration.GetSection("MessageBroker:SA").Get<MessageBrokerCredentialModel>());
            ConnectRegions();
        }

        public void Acknowledge(BasicDeliverEventArgs e)
        {
            _consumer.Model.BasicAck(e.DeliveryTag, false);
        }

        public void NAcknowledge(BasicDeliverEventArgs e)
        {
            _consumer.Model.BasicNack(e.DeliveryTag, false, true);
        }
        public void ExternalConnect()
        {
            _externalHostName = _configuration["ExternalMessageBroker:Host"];
            _externalUserName = _configuration["ExternalMessageBroker:Username"];
            _externalPassword = _configuration["ExternalMessageBroker:Password"];
            _externalVirtualHost = _configuration["ExternalMessageBroker:VirtualHost"];
            _externalExchange = _configuration["ExternalMessageBroker:Exchange"];
            _externalQueue = _configuration["ExternalMessageBroker:Queue"];
            _externalConsumerName = _configuration["ExternalMessageBroker:Consumer"];
            _externalRoutingKey = _configuration["ExternalMessageBroker:RoutingKey"];

            if (_externalConnection == null)
            {
                if (_externalConnectionFactory == null)
                    _externalConnectionFactory = new ConnectionFactory
                    {
                        HostName = _externalHostName,
                        Port = 5672,
                        UserName = _externalUserName,
                        Password = _externalPassword,
                        VirtualHost = _externalVirtualHost,
                        RequestedHeartbeat = 60,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                    };

                _externalConnection = _externalConnectionFactory.CreateConnection();

                if (_externalModel == null)
                    _externalModel = _externalConnection.CreateModel();
            }
        }

        public void ConnectRegions()
        {
            #region HK
            var messageBroker = _credentials["HK"];
            try
            {
                var connectionFactory = new ConnectionFactory
                {
                    HostName = messageBroker.Host,
                    Port = 5672,
                    UserName = messageBroker.Username,
                    Password = messageBroker.Password,
                    VirtualHost = messageBroker.VirtualHost,
                    RequestedHeartbeat = 60,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                _modelHk = connectionFactory.CreateConnection().CreateModel();

                if (!string.IsNullOrEmpty(messageBroker.Queue))
                {
                    _modelHk.ExchangeDeclare(messageBroker.Exchange, "topic", true, false, null);

                    if (messageBroker.IsExclusive)
                    {
                        if (messageBroker.IsPriority)
                        {
                            var dictionary = new Dictionary<string, object>();
                            dictionary.Add("x-max-priority", 10);

                            _modelHk.QueueDeclare(messageBroker.Queue, true, true, true, dictionary);
                        }
                        else
                            _modelHk.QueueDeclare(messageBroker.Queue, true, true, true, null);
                    }
                    else
                    {
                        if (messageBroker.IsPriority)
                        {
                            var dictionary = new Dictionary<string, object>();
                            dictionary.Add("x-max-priority", 10);

                            _modelHk.QueueDeclare(messageBroker.Queue, true, false, false, dictionary);
                        }
                        else
                            _modelHk.QueueDeclare(messageBroker.Queue, true, false, false, null);
                    }

                    foreach (var routingKey in messageBroker.RoutingKey.Split('|'))
                        _modelHk.QueueBind(messageBroker.Queue, messageBroker.Exchange, routingKey.ToLower(), null);

                    _modelHk.BasicQos(0, Convert.ToUInt16(messageBroker.PrefetchCount), false);
                    var consumer = new EventingBasicConsumer(_modelHk);
                    consumer.Received += (sender, e) =>
                    {
                        try
                        {
                            MessageReceived(sender, e);

                            consumer.Model.BasicAck(e.DeliveryTag, false);
                        }
                        catch (Exception)
                        {
                            consumer.Model.BasicNack(e.DeliveryTag, false, true);
                        };
                    };

                    _modelHk.BasicConsume(messageBroker.Queue, false, $"{messageBroker.Consumer}.{Guid.NewGuid()}", consumer);
                }
            }
            catch (Exception ex)
            {
            }
            #endregion
            #region Sa
            messageBroker = _credentials["SA"];
            try
            {
                var connectionFactory = new ConnectionFactory
                {
                    HostName = messageBroker.Host,
                    Port = 5672,
                    UserName = messageBroker.Username,
                    Password = messageBroker.Password,
                    VirtualHost = messageBroker.VirtualHost,
                    RequestedHeartbeat = 60,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                _modelSa = connectionFactory.CreateConnection().CreateModel();

                if (!string.IsNullOrEmpty(messageBroker.Queue))
                {
                    _modelSa.ExchangeDeclare(messageBroker.Exchange, "topic", true, false, null);

                    if (messageBroker.IsExclusive)
                    {
                        if (messageBroker.IsPriority)
                        {
                            var dictionary = new Dictionary<string, object>();
                            dictionary.Add("x-max-priority", 10);

                            _modelSa.QueueDeclare(messageBroker.Queue, true, true, true, dictionary);
                        }
                        else
                            _modelSa.QueueDeclare(messageBroker.Queue, true, true, true, null);
                    }
                    else
                    {
                        if (messageBroker.IsPriority)
                        {
                            var dictionary = new Dictionary<string, object>();
                            dictionary.Add("x-max-priority", 10);

                            _modelSa.QueueDeclare(messageBroker.Queue, true, false, false, dictionary);
                        }
                        else
                            _modelSa.QueueDeclare(messageBroker.Queue, true, false, false, null);
                    }

                    foreach (var routingKey in messageBroker.RoutingKey.Split('|'))
                        _modelSa.QueueBind(messageBroker.Queue, messageBroker.Exchange, routingKey.ToLower(), null);

                    _modelSa.BasicQos(0, Convert.ToUInt16(messageBroker.PrefetchCount), false);
                    var consumer = new EventingBasicConsumer(_modelSa);
                    consumer.Received += (sender, e) =>
                    {
                        try
                        {
                            MessageReceived(sender, e);

                            consumer.Model.BasicAck(e.DeliveryTag, false);
                        }
                        catch (Exception)
                        {
                            consumer.Model.BasicNack(e.DeliveryTag, false, true);
                        };
                    };

                    _modelSa.BasicConsume(messageBroker.Queue, false, $"{messageBroker.Consumer}.{Guid.NewGuid()}", consumer);
                }
            }
            catch (Exception ex)
            {
            }
            #endregion
        }
        public void Connect(MessageBrokerCredentialModel messageBroker = null)
        {
            try
            {
                if (messageBroker != null)
                {
                    _isExclusive = messageBroker.IsExclusive;
                    _isPriority = messageBroker.IsExclusive;
                    _hostName = messageBroker.Host;
                    _userName = messageBroker.Username;
                    _password = messageBroker.Password;
                    _virtualHost = messageBroker.VirtualHost;
                    _exchange = messageBroker.Exchange;
                    _queue = _isExclusive ? $"{messageBroker.Queue}.{Environment.MachineName.ToLower()}" : messageBroker.Queue;
                    _consumerName = messageBroker.Consumer;
                    _routingKey = messageBroker.RoutingKey;
                    _prefetchCount = string.IsNullOrEmpty(messageBroker.PrefetchCount) ? "1000" : messageBroker.PrefetchCount;

                }
                else
                {
                    _isExclusive = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:IsExclusive"]) ? false : Convert.ToBoolean(_configuration[$"{_messageBrokerName}:IsExclusive"]);
                    _isPriority = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:IsPriority"]) ? false : Convert.ToBoolean(_configuration[$"{_messageBrokerName}:IsPriority"]);
                    _hostName = _configuration[$"{_messageBrokerName}:Host"];
                    _userName = _configuration[$"{_messageBrokerName}:Username"];
                    _password = _configuration[$"{_messageBrokerName}:Password"];
                    _virtualHost = _configuration[$"{_messageBrokerName}:VirtualHost"];
                    _exchange = _configuration[$"{_messageBrokerName}:Exchange"];
                    _queue = _isExclusive == true ? $"{_configuration[$"{_messageBrokerName}:Queue"]}.{Environment.MachineName.ToLower()}" : _configuration[$"{_messageBrokerName}:Queue"];
                    _consumerName = _configuration[$"{_messageBrokerName}:Consumer"];
                    _routingKey = _configuration[$"{_messageBrokerName}:RoutingKey"];
                    _prefetchCount = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:PrefetchCount"]) ? "1000" : _configuration[$"{_messageBrokerName}:PrefetchCount"];
                }
                if (_connection == null)
                {
                    if (_connectionFactory == null)
                        _connectionFactory = new ConnectionFactory
                        {
                            HostName = _hostName,
                            Port = 5672,
                            UserName = _userName,
                            Password = _password,
                            VirtualHost = _virtualHost,
                            RequestedHeartbeat = 60,
                            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                        };

                    _connection = _connectionFactory.CreateConnection();

                    if (_model == null)
                        _model = _connection.CreateModel();

                    if (!string.IsNullOrEmpty(_queue))
                    {
                        _model.ExchangeDeclare(_exchange, "topic", true, false, null);

                        if (_isExclusive)
                        {
                            if (_isPriority)
                            {
                                var dictionary = new Dictionary<string, object>();
                                dictionary.Add("x-max-priority", 10);

                                _model.QueueDeclare(_queue, true, true, true, dictionary);
                            }
                            else
                                _model.QueueDeclare(_queue, true, true, true, null);
                        }
                        else
                        {
                            if (_isPriority)
                            {
                                var dictionary = new Dictionary<string, object>();
                                dictionary.Add("x-max-priority", 10);

                                _model.QueueDeclare(_queue, true, false, false, dictionary);
                            }
                            else
                                _model.QueueDeclare(_queue, true, false, false, null);
                        }

                        foreach (var routingKey in _routingKey.Split('|'))
                            _model.QueueBind(_queue, _exchange, routingKey.ToLower(), null);

                        _model.BasicQos(0, Convert.ToUInt16(_prefetchCount), false);
                        _consumer = new EventingBasicConsumer(_model);
                        _consumer.Received += HandleMessage;

                        _model.BasicConsume(_queue, false, $"{_consumerName}.{Guid.NewGuid()}", _consumer);
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public void ConnectWithManualAck()
        {
            _hostName = _configuration[$"{_messageBrokerName}:Host"];
            _userName = _configuration[$"{_messageBrokerName}:Username"];
            _password = _configuration[$"{_messageBrokerName}:Password"];
            _virtualHost = _configuration[$"{_messageBrokerName}:VirtualHost"];
            _exchange = _configuration[$"{_messageBrokerName}:Exchange"];
            _queue = _configuration[$"{_messageBrokerName}:Queue"];
            _consumerName = _configuration[$"{_messageBrokerName}:Consumer"];
            _routingKey = _configuration[$"{_messageBrokerName}:RoutingKey"];
            _prefetchCount = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:PrefetchCount"]) ? "1000" : _configuration[$"{_messageBrokerName}:PrefetchCount"];

            if (_connection == null)
            {
                if (_connectionFactory == null)
                    _connectionFactory = new ConnectionFactory
                    {
                        HostName = _hostName,
                        Port = 5672,
                        UserName = _userName,
                        Password = _password,
                        VirtualHost = _virtualHost,
                        RequestedHeartbeat = 60,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                    };

                _connection = _connectionFactory.CreateConnection();

                if (_model == null)
                    _model = _connection.CreateModel();

                if (!string.IsNullOrEmpty(_queue))
                {
                    _model.ExchangeDeclare(_exchange, "topic", true, false, null);
                    _model.QueueDeclare(_queue, true, false, false, null);

                    foreach (var routingKey in _routingKey.Split('|'))
                        _model.QueueBind(_queue, _exchange, routingKey, null);

                    _model.BasicQos(0, Convert.ToUInt16(_prefetchCount), false);
                    _consumer = new EventingBasicConsumer(_model);
                    _consumer.Received += HandleMessageWithManualAck;

                    _model.BasicConsume(_queue, false, $"{_consumerName}.{Guid.NewGuid()}", _consumer);
                }
            }
        }

        private void HandleMessageWithManualAck(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                MessageReceived(sender, e);
            }
            catch (Exception)
            {
                _consumer.Model.BasicNack(e.DeliveryTag, false, true);
            }
        }

        private void HandleMessage(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                MessageReceived(sender, e);

                _consumer.Model.BasicAck(e.DeliveryTag, false);
            }
            catch (Exception)
            {
                _consumer.Model.BasicNack(e.DeliveryTag, false, true);
            }
        }

        public void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId)
        {
            var properties = _externalModel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.DeliveryMode = 2;
            properties.Type = messageType;

            var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            _externalModel.BasicPublish(_externalExchange, routingKey, properties, payloadBytes);
        }

        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId, string region = "")
        {
            var properties = _model.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.DeliveryMode = 2;
            properties.Type = messageType;

            var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            BasicPublish(region, routingKey, properties, payloadBytes);
        }

        public void Publish(string payload, string messageType, string routingKey, string correlationId, string region = "")
        {
            var properties = _model.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.DeliveryMode = 2;
            properties.Type = messageType;

            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            BasicPublish(region, routingKey, properties, payloadBytes);
        }

        public void Dispose()
        {
            if (_model != null)
                _model.Dispose();

            if (_externalModel != null)
                _externalModel.Dispose();

            if (_connection != null)
                _connection.Dispose();
        }
        private void BasicPublish(string region, string routingKey, IBasicProperties properties, byte[] payloadBytes)
        {
            switch (region.ToUpper())
            {
                case "HK":
                    _modelHk.BasicPublish(_credentials["HK"].Exchange, routingKey, properties, payloadBytes);
                    break;
                case "SA":
                    _modelSa.BasicPublish(_credentials["SA"].Exchange, routingKey, properties, payloadBytes);
                    break;
                default:
                    _model.BasicPublish(_exchange, routingKey, properties, payloadBytes);
                    break;
            }
        }
    }
}
