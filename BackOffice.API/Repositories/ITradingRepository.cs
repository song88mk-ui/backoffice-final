using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

namespace Backoffice.API.Repositories
{
    public interface ITradingRepository
    {

        #region Client
        #endregion

        #region Sportsbook
        #endregion

        #region Priceboost
        Task<Tuple<BaseResponse<PriceBoostConfigResponse>, OnActivityLogListMessage>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request, string region);
        Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request, string region);
        Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request, string region);
        Task<BaseResponse<bool>> AddOrUpdateBoostedSettings(BoostedSettingsModel request, string userId, string region);
        Task<BaseResponse<List<PriceBoostConfigAuditModel>>> GetPriceBoostAuditList(PriceBoostConfigRequest request, string region);
        #endregion

        #region Activity Log
        Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region);
        #endregion

        #region Sports
        Task<BaseResponse<SportsTreeResponse>> GetSports(int fixtureTypeId, string region);
        Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, string region);
        Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string region);
        #endregion

        #region Segment
        Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string region);
        #endregion

        #region League
        Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string region);
        #endregion

        #region Fixtures
        Task<BaseResponse<MarketFixtureResponse>> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId, string region);
        Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId, string region);
        Task<BaseResponse<FixtureOddsHistoryResponse>> GetFixtureMarketSelections(int fixtureTypeId, string providerFixtureId, string providerMarketId, string providerSelectionId, string region);
        Task<BaseResponse<MarketFixtureDetailResponse>> GetFixtureMarketDetails(string providerFixtureId, string region);
        #endregion
    }
}
