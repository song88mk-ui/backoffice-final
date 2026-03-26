using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface ISportsbookRepository
    {
        #region Client Market Templates
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(Guid clientId, int sportId, int fixtureStatusId);
        //Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string langCode, string region);
        //Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request, string region);
        //Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request, string region);
        //Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request, string region);
        //Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request, string region);
        //Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request, string region);
        #endregion
        Task<BaseResponse<MessageResponse>> Publish(MessageRequest request, string region);

    }
}
