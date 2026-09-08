using Backoffice.Config.Service.Repository;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Backoffice.Config.Service
{
    public class Worker : BackgroundService
    {
        //
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
            _logger.LogInformation("Connecting to message broker");
            _messageBroker.Connect();
            _logger.LogInformation("Connected, processing incoming messages");
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
                    case "UpdateClientMargin":
                        var UpdateClientMarginPayload = JsonSerializer.Deserialize<long>(payload);
                        _backofficeRepository.UpdateClientMargin(UpdateClientMarginPayload, _messageBroker);
                        break;
                    case "DeleteConfigTemplate":
                        var DeleteConfigTemplatePayload = JsonSerializer.Deserialize<ConfigTemplate>(payload);
                        _backofficeRepository.DeleteConfigTemplate(DeleteConfigTemplatePayload);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("error processing message: {exception} {stacktrace}", ex.Message, ex.StackTrace);
            }
        }
    }
}
