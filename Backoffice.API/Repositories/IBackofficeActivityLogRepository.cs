using Backoffice.Shared.OutgoingEngineMessages;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeActivityLogRepository
    {
        Task ProcessActivityLog(OnActivityLogMessage activityLog, string region = "HK");
        Task ProcessActivityLog(OnActivityLogListMessage activityLogs, string region = "HK");
    }
}
