using Backoffice.Shared.Models;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
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
        private int _hostPort = 5672;
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
        //private IModel _modelSa;

        private readonly string _messageBrokerName;

        private string _externalHostName = string.Empty;
        private int _externalHostPort = 5672;
        private string _externalUserName = string.Empty;
        private string _externalPassword = string.Empty;
        private string _externalVirtualHost = string.Empty;
        private string _externalExchange = string.Empty;
        private string _externalQueue = string.Empty;
        private string _externalConsumerName = string.Empty;
        private string _externalRoutingKey = string.Empty;

        MessageBrokerCredentialModel _credential { get;set; }

        private static readonly ActivitySource _activitySource = new ActivitySource(nameof(MessageBroker));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public MessageBroker(IConfiguration configuration)
        {
            _messageBrokerName = $"MessageBroker";
            _configuration = configuration;
            _credential = new MessageBrokerCredentialModel();
            _credential = configuration.GetSection("MessageBroker:HK").Get<MessageBrokerCredentialModel>();
            //_credentials.Add("SA", configuration.GetSection("MessageBroker:SA").Get<MessageBrokerCredentialModel>());
            //ConnectRegions();
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
            _externalHostPort = string.IsNullOrEmpty(_configuration["ExternalMessageBroker:Port"]) ? 5672 : Convert.ToInt32(_configuration["ExternalMessageBroker:Port"]);
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
                        Port = _externalHostPort,
                        UserName = _externalUserName,
                        Password = _externalPassword,
                        VirtualHost = _externalVirtualHost,
                        RequestedHeartbeat = 60,
                        Ssl = new SslOption { Enabled = _externalHostPort == 5671 ? true : false, ServerName = _externalHostName },
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
            var messageBroker = _credential;
            try
            {
                var connectionFactory = new ConnectionFactory
                {
                    HostName = messageBroker.Host,
                    Port = messageBroker.Port,
                    UserName = messageBroker.Username,
                    Password = messageBroker.Password,
                    VirtualHost = messageBroker.VirtualHost,
                    RequestedHeartbeat = 60,
                    Ssl = messageBroker.Ssl,
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
            catch (Exception) {}
            #endregion
        }
        public void Connect(MessageBrokerCredentialModel messageBroker = null)
        {
            var suffix = "";
            using (MD5 hasher = MD5.Create())
            {
                byte[] bytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
                StringBuilder sb = new StringBuilder();
                foreach(byte b in bytes)
                    sb.Append(b.ToString("x2"));
                suffix = sb.ToString().Substring(0,5);
            }

            try
            {
                if (messageBroker != null)
                {
                    _isExclusive = messageBroker.IsExclusive;
                    _isPriority = messageBroker.IsPriority;
                    _hostName = messageBroker.Host;
                    _hostPort = messageBroker.Port;
                    _userName = messageBroker.Username;
                    _password = messageBroker.Password;
                    _virtualHost = messageBroker.VirtualHost;
                    _exchange = messageBroker.Exchange;
                    _queue = _isExclusive == true ? $"{messageBroker.Queue}.{suffix}" : messageBroker.Queue;
                    _consumerName = messageBroker.Consumer;
                    _routingKey = messageBroker.RoutingKey;
                    _prefetchCount = string.IsNullOrEmpty(messageBroker.PrefetchCount) ? "1000" : messageBroker.PrefetchCount;

                }
                else
                {
                    _isExclusive = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:IsExclusive"]) ? false : Convert.ToBoolean(_configuration[$"{_messageBrokerName}:IsExclusive"]);
                    _isPriority = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:IsPriority"]) ? false : Convert.ToBoolean(_configuration[$"{_messageBrokerName}:IsPriority"]);
                    _hostName = _configuration[$"{_messageBrokerName}:Host"];
                    _hostPort = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:Port"]) ? 5672 : Convert.ToInt32(_configuration[$"{_messageBrokerName}:Port"]);
                    _userName = _configuration[$"{_messageBrokerName}:Username"];
                    _password = _configuration[$"{_messageBrokerName}:Password"];
                    _virtualHost = _configuration[$"{_messageBrokerName}:VirtualHost"];
                    _exchange = _configuration[$"{_messageBrokerName}:Exchange"];
                    _queue = _isExclusive == true ? $"{_configuration[$"{_messageBrokerName}:Queue"]}.{suffix}" : _configuration[$"{_messageBrokerName}:Queue"];
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
                            Port = _hostPort,
                            UserName = _userName,
                            Password = _password,
                            VirtualHost = _virtualHost,
                            RequestedHeartbeat = 60,
                            Ssl = new SslOption { Enabled = _hostPort == 5671 ? true : false, ServerName = _hostName },
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
                throw ex;
            }
        }

        public void ConnectWithManualAck()
        {
            _hostName = _configuration[$"{_messageBrokerName}:Host"];
            _hostPort = string.IsNullOrEmpty(_configuration[$"{_messageBrokerName}:Port"]) ? 5672 : Convert.ToInt32(_configuration[$"{_messageBrokerName}:Port"]);
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
                        Port = _hostPort,
                        UserName = _userName,
                        Password = _password,
                        VirtualHost = _virtualHost,
                        RequestedHeartbeat = 60,
                        Ssl = new SslOption { Enabled = _hostPort == 5671 ? true : false, ServerName = _hostName },
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

        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId)
        {
            try
            {
              var activityName = $"{routingKey} send";
              using var activity = _activitySource.StartActivity(activityName, ActivityKind.Producer);

              var properties = _model.CreateBasicProperties();
              properties.CorrelationId = correlationId;
              properties.DeliveryMode = 2;
              properties.Type = messageType;

              ActivityContext contextToInject = default;
              if (activity != null)
              {
                contextToInject = activity.Context;
              }
              else if (Activity.Current != null)
              {
                contextToInject = Activity.Current.Context;
              }

              Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, this.InjectTraceContextIntoBasicProperties);

              var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

              BasicPublish(routingKey, properties, payloadBytes);
            }
            catch (Exception)
            {
              // throw?
            }
        }

        public void Publish(string payload, string messageType, string routingKey, string correlationId)
        {
            try
            {
              var activityName = $"{routingKey} send";
              using var activity = _activitySource.StartActivity(activityName, ActivityKind.Producer);

              var properties = _model.CreateBasicProperties();
              properties.CorrelationId = correlationId;
              properties.DeliveryMode = 2;
              properties.Type = messageType;

              ActivityContext contextToInject = default;
              if (activity != null)
              {
                contextToInject = activity.Context;
              }
              else if (Activity.Current != null)
              {
                contextToInject = Activity.Current.Context;
              }

              Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, this.InjectTraceContextIntoBasicProperties);

              var payloadBytes = Encoding.UTF8.GetBytes(payload);

              BasicPublish(routingKey, properties, payloadBytes);
            }
            catch (Exception)
            {
              // throw?
            }
        }

        private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
        {
          try
          {
            if (props.Headers == null)
            {
              props.Headers = new Dictionary<string, object>();
            }
            props.Headers[key] = value;
          }
          catch (Exception)
          {
            //
          }
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
        private void BasicPublish(string routingKey, IBasicProperties properties, byte[] payloadBytes)
        {
            _model.BasicPublish(_exchange, routingKey, properties, payloadBytes);
        }
    }
}
