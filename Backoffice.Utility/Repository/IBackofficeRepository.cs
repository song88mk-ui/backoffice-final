using Backoffice.Shared.OutgoingEngineMessages;
using System.Threading.Tasks;

namespace Backoffice.Utility.Repository
{
    public interface IBackofficeRepository
    {
        Task ProcessActivityLog(OnActivityLogMessage message);
        Task ProcessActivityLog(OnActivityLogListMessage message);
    }
}
