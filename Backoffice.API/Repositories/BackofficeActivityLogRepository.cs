using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.EntityFrameworkCore;
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

        public async Task<BaseResponse<ActivityLogResponse>> GetPagedActivityLogs(string page, string payload, DateTime dateFrom, MetaData metaData)
        {
            try
            {
                var pageSize = metaData?.PageSize > 0 ? metaData.PageSize : 10;
                var currentPage = metaData?.CurrentPage > 0 ? metaData.CurrentPage : 1;

                var query = _backofficeContext.ActivityLog.AsNoTracking()
                    .Where(l => l.Page == page && l.TimeStamp >= dateFrom);

                if (!string.IsNullOrEmpty(payload))
                    query = query.Where(l => l.Payload == payload);

                var totalCount = await query.CountAsync();

                var logs = await query
                    .OrderByDescending(l => l.TimeStamp)
                    .ThenByDescending(l => l.Id)
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new ActivityLogModel
                    {
                        Id = l.Id,
                        Page = l.Page,
                        Payload = l.Payload,
                        Activity = l.Activity,
                        From = l.From,
                        To = l.To,
                        UserId = l.UserId,
                        UserName = l.UserName,
                        TimeStamp = l.TimeStamp
                    })
                    .ToListAsync();

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = logs,
                        MetaData = new MetaData
                        {
                            CurrentPage = currentPage,
                            PageSize = pageSize,
                            TotalCount = totalCount,
                            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}
