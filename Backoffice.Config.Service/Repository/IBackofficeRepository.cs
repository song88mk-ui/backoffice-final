using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using System.Threading.Tasks;

namespace Backoffice.Utility.Repository
{
    public interface IBackofficeRepository
    {
        void UpdateClientMargin(long configTemplateId, IMessageBroker messageBroker);
    }
}
