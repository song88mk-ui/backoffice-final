using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Utility.Repository;
using Backoffice.Utility.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Backoffice.Utility
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBackofficeRepository _backofficeRepository;

        private readonly ActivitySource _activitySource;
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBackofficeRepository backofficeRepository, Instrumentation instrumentation)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _backofficeRepository = backofficeRepository;
            _activitySource = instrumentation.ActivitySource;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to message broker");
            _messageBroker.Connect();
            _logger.LogInformation("Connected, consuming messages");
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }
        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            var parentContext = Propagator.Extract(default, e.BasicProperties, this.ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;

            var activityName = $"{e.RoutingKey} receive";

            using (var activity = this._activitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext))
            {
                try
                {

                    activity?.SetTag("messaging.system", "rabbitmq");
                    activity?.SetTag("messaging.destination_kind", "queue");
                    activity?.SetTag("messaging.rabbitmq.routing_key", e.RoutingKey);
                    activity?.SetTag("messaging.rabbitmq.message_type", messageType);

                    switch (messageType)
                    {
                        case "LogActivity":
                            var activityLog = JsonSerializer.Deserialize<OnActivityLogMessage>(payload);
                            _backofficeRepository.ProcessActivityLog(activityLog);
                            break;
                        case "LogActivityList":
                            var activityLogList = JsonSerializer.Deserialize<OnActivityLogListMessage>(payload);
                            _backofficeRepository.ProcessActivityLog(activityLogList);
                            break;
                        case "ExportMessageSettlementHistory":
                            var manualMessageSettlementHistory = JsonSerializer.Deserialize<OnGetMessageSettlementHistoryMessage>(payload);
                            _backofficeRepository.ProcessOnGetMessageSettlementHistory(manualMessageSettlementHistory, _messageBroker, correlationId);
                            break;
                        case "ExportManualBetSettlementHistory":
                            var manualBetSettlementHistory = JsonSerializer.Deserialize<OnGetManualBetSettlementHistoryMessage>(payload);
                            _backofficeRepository.ProcessOnGetManualBetSettlementHistory(manualBetSettlementHistory, _messageBroker);
                            break;
                        case "ExportMarketSettlementHistory":
                            var marketSettlementHistory = JsonSerializer.Deserialize<OnGetMarketSettlementHistoryMessage>(payload);
                            _backofficeRepository.ProcessOnGetMarketSettlementHistory(marketSettlementHistory, _messageBroker);
                            break;
                        case "ExportOeConfigTemplate":
                            var oeConfigTemplate = JsonSerializer.Deserialize<OnGetOeConfigTemplateMessage>(payload);
                            _backofficeRepository.ProcessOnGetOeConfigTemplate(oeConfigTemplate, _messageBroker);
                            break;
                        case "ExportCancelBetHistory":
                            var cancelBetHistory = JsonSerializer.Deserialize<OnGetCancelBetHistoryMessage>(payload);
                            _backofficeRepository.ProcessOnGetCancelBetHistory(cancelBetHistory, _messageBroker);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("consume error: {exception}. stacktrace: {stacktrace}", ex.Message, ex.StackTrace);

                }
            }
        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
        {
          try
          {
            if (props.Headers.TryGetValue(key, out var value))
            {
              var bytes = value as byte[];
              return new[] {  Encoding.UTF8.GetString(bytes) };
            }
          }
          catch (Exception ex) {}
          return Enumerable.Empty<string>();
        }
    }
}
