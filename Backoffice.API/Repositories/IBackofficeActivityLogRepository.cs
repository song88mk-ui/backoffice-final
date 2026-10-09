using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeActivityLogRepository
    {
        Task ProcessActivityLog(OnActivityLogMessage activityLog, string region = "HK");
        Task ProcessActivityLog(OnActivityLogListMessage activityLogs, string region = "HK");
        // Reads ActivityLog2 with a plain query, newest first, so it does not depend on the GetActivityLogs stored procedure.
        Task<BaseResponse<ActivityLogResponse>> GetPagedActivityLogs(string page, string payload, DateTime dateFrom, MetaData metaData);
    }
}
