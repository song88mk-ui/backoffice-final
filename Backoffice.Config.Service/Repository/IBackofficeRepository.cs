using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Utilities;

namespace Backoffice.Config.Service.Repository
{
    public interface IBackofficeRepository
    {
        void UpdateClientMargin(long configTemplateId, IMessageBroker messageBroker);
        void DeleteConfigTemplate(ConfigTemplate configTemplateId);
    }
}
