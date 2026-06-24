using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeActivityLogRepository : IBackofficeActivityLogRepository
    {
        private readonly BackofficeContext _backofficeContext;
        public BackofficeActivityLogRepository(BackofficeContext backofficeContext)
        {
            _backofficeContext = backofficeContext;
        }

        public async Task ProcessActivityLog(OnActivityLogMessage activityLog, string region = "HK")
        {
            await ProcessActivityLog(new OnActivityLogListMessage
            {
                OnActivityLogMessages = new List<OnActivityLogMessage> { activityLog }
            }, region);
        }

        public async Task ProcessActivityLog(OnActivityLogListMessage activityLogs, string region = "HK")
        {
            var logs = activityLogs.OnActivityLogMessages.Select(item => new ActivityLog
            {
                Page = item.Page,
                Payload = item.Payload,
                Activity = item.Activity,
                From = item.From,
                To = item.To,
                UserId = item.UserId,
                UserName = item.UserName,
                TimeStamp = item.TimeStamp == DateTime.MinValue ? DateTime.UtcNow : item.TimeStamp
            }).ToList();

            _backofficeContext.ActivityLog.AddRange(logs);
            await _backofficeContext.SaveChangesAsync();
        }
    }
}
