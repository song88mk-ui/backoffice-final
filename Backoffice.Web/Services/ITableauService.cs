using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.JSInterop;

namespace Backoffice.Web.Services
{
    public interface ITableauService
    {
        Task<string> GetTicket();
    }
}
