using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Repositories
{
    public interface ISportsbookRepository
    {
        Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps();
    }
}
