using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using System.Threading.Tasks;

namespace Backoffice.Utility.Repository
{
    public interface IBackofficeRepository
    {
        Task ProcessActivityLog(OnActivityLogMessage message);
        Task ProcessActivityLog(OnActivityLogListMessage message);
        Task ProcessOnGetManualMessageSettlementHistory(OnGetManualMarketSettlementHistoryMessage request, IMessageBroker messageBroker);
        Task ProcessOnGetManualBetSettlementHistory(OnGetManualBetSettlementHistoryMessage request, IMessageBroker messageBroker);
    }
}
