using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Utility.Repository;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
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

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBackofficeRepository backofficeRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _backofficeRepository = backofficeRepository;
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
            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }
        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
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
                    case "ExportCancelBetHistory":
                        var cancelBetHistory = JsonSerializer.Deserialize<OnGetCancelBetHistoryMessage>(payload);
                        _backofficeRepository.ProcessOnGetCancelBetHistory(cancelBetHistory, _messageBroker);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");

            }
        }
    }
}
