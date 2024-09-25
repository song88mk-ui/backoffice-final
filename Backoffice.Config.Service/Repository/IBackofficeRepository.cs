using Backoffice.Shared.Utilities;

namespace Backoffice.Utility.Repository
{
    public interface IBackofficeRepository
    {
        void UpdateClientMargin(long configTemplateId, IMessageBroker messageBroker);
    }
}
