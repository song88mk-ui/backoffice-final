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
            // A throw makes the message broker nack and requeue the message, so a failure gets one retry. A message
            // that fails again is reported with its payload and acked, so it cannot loop forever (BO-138).
            catch (Exception ex) when (!e.Redelivered)
            {
                _logger.LogWarning(ex, "error processing message {messageType}, requeueing it for one retry: {exception}", messageType, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error processing message {messageType} after a retry, dropping it. payload: {payload}", messageType, payload);
            }
        }
    }
}
