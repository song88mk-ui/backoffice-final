using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Utility.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
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
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Received {messageType}");
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");

            }
        }
    }
}
