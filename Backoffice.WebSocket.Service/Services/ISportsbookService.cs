using Backoffice.Shared.IncomingSbEngineMessages;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Services
{
    public interface ISportsbookService
    {
        Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps();
        Task<BaseResponse<NoMarginClientsResponse>> GetAsianclientCodes();
    }
}
