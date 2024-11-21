using Backoffice.Shared.IncomingSbEngineMessages;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.WebSocket.Service.Services;
using Backoffice.WebSocket.Service.Telemetry;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly ISportsbookService _sportsbookService;
        private readonly HubConnection _hubConnection;

        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
        private readonly ActivitySource _activitySource;
        private readonly Counter<long> _receivedMessagesCounter;

        private Thread _threadOnMonitoringOddsChange;
        private Thread _threadOnMonitoringDetailsOddsChange;
        private Thread _threadOnBetStopFixture;
        private Thread _threadOnBetStopGlobal;
        private Thread _threadNotification;

        private readonly BlockingCollection<IncomingOddsChangeMessage> _messagesOnMonitoringOddsChange = new BlockingCollection<IncomingOddsChangeMessage>();
        private readonly BlockingCollection<IncomingOddsChangeMessage> _messagesOnMonitoringDetailsOddsChange = new BlockingCollection<IncomingOddsChangeMessage>();
        private readonly BlockingCollection<IncomingBetStopFixtureMessage> _messagesOnBetStopFixture = new BlockingCollection<IncomingBetStopFixtureMessage>();
        private readonly BlockingCollection<IncomingBetStopGlobalMessage> _messagesOnBetStopGlobal = new BlockingCollection<IncomingBetStopGlobalMessage>();
        private readonly BlockingCollection<NotificationsDetailsModel> _messagesNotification = new BlockingCollection<NotificationsDetailsModel>();

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IMessageBroker messageBroker, ISportsbookService sportsbookService, Instrumentation instrumentation)
        {
            _logger = logger;
            _sportsbookService = sportsbookService;

            _hubConnection = new HubConnectionBuilder()
                                .WithUrl(configuration["Custom:WebSocketAPI"], options =>
                                {
                                    options.SkipNegotiation = true;
                                    options.Transports = HttpTransportType.WebSockets;
                                })
                                .AddMessagePackProtocol(options =>
                                {
                                    StaticCompositeResolver.Instance.Register(
                                        ContractlessStandardResolver.Instance
                                    );
                                    options.SerializerOptions = MessagePackSerializerOptions.Standard
                                        .WithResolver(ContractlessStandardResolver.Instance)
                                        .WithSecurity(MessagePackSecurity.UntrustedData);
                                })
                                .WithAutomaticReconnect()
                                .Build();
            _messageBroker = messageBroker;
            _activitySource = instrumentation.ActivitySource;
            _receivedMessagesCounter = instrumentation.ReceivedMessagesCounter;
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _hubConnection.DisposeAsync();

            _messageBroker.Dispose();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _threadOnMonitoringOddsChange = new Thread(ThreadMonitoringOnOddsChange);
            _threadOnMonitoringDetailsOddsChange = new Thread(ThreadMonitoringDetailsOnOddsChange);
            _threadNotification = new Thread(ThreadNotification);

            _threadOnMonitoringOddsChange.Start();
            _threadOnMonitoringDetailsOddsChange.Start();
            _threadNotification.Start();

            _hubConnection.Closed += async (error) =>
            {
                _logger.LogError($"Connection closed. Trying to reconnect... - {error.Message}- {error.InnerException}");
            };
            _hubConnection.Reconnecting += (error) =>
            {
                _logger.LogWarning($"Reconnecting... {error.Message}- {error.StackTrace}");
                return Task.CompletedTask;
            };
            _hubConnection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation($"Reconnected with connectionId: {connectionId}");
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();

            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;
        }

        private async void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var parentContext = Propagator.Extract(default, e.BasicProperties, this.ExtractTraceFromBasicProperties);
            Baggage.Current = parentContext.Baggage;

            using (var activity = _activitySource.StartActivity($"{e.RoutingKey} receive", ActivityKind.Consumer, parentContext.ActivityContext))
            {
                var payload = Encoding.UTF8.GetString(e.Body);
                var messageType = e.BasicProperties.Type;
                var correlationId = e.BasicProperties.CorrelationId;

                activity?.SetTag("message.type", messageType);
                if (_receivedMessagesCounter.Enabled)
                    _receivedMessagesCounter.Add(1);

                try
                {
                    switch (messageType)
                    {
                        case "OnOddsChange":
                            var onOddsChange = JsonSerializer.Deserialize<IncomingOddsChangeMessage>(payload);

                            if (onOddsChange != null)
                            {
                                _messagesOnMonitoringOddsChange.Add(onOddsChange);
                                _messagesOnMonitoringDetailsOddsChange.Add(onOddsChange);
                            }
                            break;
                        case "OnBetStopFixture":
                            var onBetStop = JsonSerializer.Deserialize<IncomingBetStopFixtureMessage>(payload);
                            _messagesOnBetStopFixture.Add(onBetStop);
                            break;
                        case "OnBetStopGlobal":
                            var onBetStopGlobal = JsonSerializer.Deserialize<IncomingBetStopGlobalMessage>(payload);
                            _messagesOnBetStopGlobal.Add(onBetStopGlobal);
                            break;
                        case "OnNewNotification":
                            var notificationsModel = JsonSerializer.Deserialize<NotificationsDetailsModel>(payload);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message} - {ex.StackTrace}");
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                }
            }
        }

        private async void ThreadMonitoringOnOddsChange()
        {
            while (true)
            {
                var message = _messagesOnMonitoringOddsChange.Take();
                await _sportsbookService.ProcessOnMonitoringOddsChange(message, _hubConnection);
            }
        }
        private async void ThreadMonitoringDetailsOnOddsChange()
        {
            while (true)
            {
                var message = _messagesOnMonitoringDetailsOddsChange.Take();
                await _sportsbookService.ProcessOnMonitoringDetailsOddsChange(message, _hubConnection);
            }
        }
        private async void ThreadBetStopFixture()
        {
            while (true)
            {
                var message = _messagesOnBetStopFixture.Take();
                await _sportsbookService.ProcessOnBetStopFixture(message, _hubConnection);
            }
        }
        private async void ThreadBetStopGlobal()
        {
            while (true)
            {
                var message = _messagesOnBetStopGlobal.Take();
                await _sportsbookService.ProcessOnBetStopGlobal(message, _hubConnection);
            }
        }
        private async void ThreadNotification()
        {
            while (true)
            {
                var message = _messagesNotification.Take();
                await _sportsbookService.ProcessNotification(message, _hubConnection);
            }
        }

        private IEnumerable<string> ExtractTraceFromBasicProperties(IBasicProperties props, string key)
        {
            try
            {
                if (props.Headers.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    return new[] { Encoding.UTF8.GetString(bytes) };
                }
            }
            catch (Exception) {}
            return Enumerable.Empty<string>();
        }
    }
}
