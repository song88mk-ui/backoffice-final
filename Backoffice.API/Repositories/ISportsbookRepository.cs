using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using OE.Shared.Data.Sportsbook.SQLEntities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface ISportsbookRepository
    {
        #region Client Market Templates
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(ClientMarketTemplateRequest request);
        Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups();
        Task<BaseResponse<MarketLookupResponse>> GetMarketLookup(MarketLookupRequest request, string langCode = "");
        //Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string langCode, string region);
        //Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request, string region);
        //Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request, string region);
        Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request, string region);
        Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request, string region);
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request, string region);
        #endregion

        #region Client Market Sort Order
        Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string langCode);
        Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request);
        Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request);
        #endregion

        #region Client Selector Market Templates
        Task<BaseResponse<List<ClientSelectorMarketTemplate>>> GetClientSelectorMarketTemplates(Guid clientId);
        Task<BaseResponse<List<SelectorMarketTemplate>>> GetSelectorMarketTemplateLookup();
        Task<BaseResponse<bool>> AddOrUpdateClientSelectorMarketTemplates(ClientSelectorMarketTemplateRequest request);
        Task<BaseResponse<bool>> CopyClientSelectorMarketTemplates(CopyClientSelectorMarketTemplateRequest request);
        #endregion

        #region Lookup
        Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string langCode = "EN");
        Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string langCode = "EN");
        Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string langCode = "EN");
        #endregion

        #region Sport Settings
        Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request);
        Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request);
        Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request);
        Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request);
        Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request);
        Task<BaseResponse<List<ClientOddstableLeagueOrderListResponse>>> GetClientOddstableLeagueOrderList(ClientOddstableLeagueOrderListRequest request);
        Task<BaseResponse<ClientOddstableLeagueSettingResponse>> AddOrUpdateClientOddstableLeagueSettings(ClientOddstableLeagueSettingRequest request);
        Task<BaseResponse<ClientOddstableLeagueSettingResponse>> CopyClientOddstableLeagueSettings(CopyClientOddstableLeagueSettingRequest request);
        Task<BaseResponse<ImportClientOddstableLeagueSettingResponse>> ImportClientOddstableLeagueSettings(ImportClientOddstableLeagueSettingRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetOddstableLeagueSortingLogs(OddstableLeagueSortingLogRequest request);
        Task<BaseResponse<List<TierLeagueOrder>>> GetTierLeagueOrderList();
        #endregion

    }
}
