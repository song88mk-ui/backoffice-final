using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.JSInterop;

namespace Backoffice.Web.Services
{
    public interface IExcelService
    {
        Task<BaseResponse<OnBulkUpdateConfigMessage>> ConvertExceltoBulkUpdateConfigMessage(MemoryStream stream, string fileName, string userId, string clientCodes, IMessageBroker messageBroker);
    }
}
