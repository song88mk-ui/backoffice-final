using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeActivityLogRepository : IBackofficeActivityLogRepository
    {
        private readonly IRabbitMqRepository _rabbitMqRepository;
        public BackofficeActivityLogRepository(IRabbitMqRepository rabbitMqRepository)
        {
            _rabbitMqRepository = rabbitMqRepository;
        }
        public async Task ProcessActivityLog(OnActivityLogMessage activityLog, string region = "HK")
        {
            await _rabbitMqRepository.Publish(new MessageRequest()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageType = "LogActivityList",
                RoutingKey = $"r.bo.util.logs",
                Payload = JsonSerializer.Serialize(activityLog)
            }, region);
        }

        public async Task ProcessActivityLog(OnActivityLogListMessage activityLogs, string region = "HK")
        {
            await _rabbitMqRepository.Publish(new MessageRequest()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageType = "LogActivityList",
                RoutingKey = $"r.bo.util.logs",
                Payload = JsonSerializer.Serialize(activityLogs)
            }, region);
        }
    }
}
