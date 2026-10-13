using Backoffice.API.Models;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeRepository
    {
        Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request);
        Task<BaseResponse<bool>> AddActivityLogList(ActivityLogListRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetManualBetSettlementHistoryLogs(ManualBetSettlementActivityLogRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request);
        Task<BaseResponse<ClientResponse>> GetClients();
        Task<BaseResponse<string>> GetClientRegion(int clientId);
        Task<BaseResponse<Dictionary<string, string>>> GetClientsByRegion(string clientIdRefs);
        Task<BaseResponse<bool>> AddOrUpdateClient(ClientListResponse.Client request);
        Task<BaseResponse<BetSettingResponse>> GetBetSetting(BetSettingRequest request);
        Task<BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>>> GetBetSettingsBulk(List<int> clientIds);
        Task<BaseResponse<ExchangeRatesResponse>> GetExchangeRates();
        Task<BaseResponse<List<PlatformClientResponse>>> GetPlatformClients();

        #region Config
        Task<BaseResponse<ConfigTemplateResponse>> GetConfigTemplates(string searchText, List<int> sportIds = null, List<int> scopeIds = null);
        Task<BaseResponse<int>> GetConfigTemplatesCount();
        Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(long templateId);
        Task<BaseResponse<long>> AddOrUpdateConfigTemplate(ConfigTemplateModel request);
        Task<BaseResponse<bool>> SetConfigState(ConfigState configState, ConfigSetLeagueStateRequest request);
        Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(string id, RequesterInfo requesterInfo);
        Task<BaseResponse<int>> GetTemplateConfigCount(List<string> clientCodes, string state);
        Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigState(List<string> clientCodes, ConfigTemplateLeagueRequest payload, ConfigState configState, int templateId = -1, LanguageCode langCode = LanguageCode.EN);
        Task<BaseResponse<ConfigTemplateMarginModel>> GetConfigTemplateMarginById(long templateId);
        Task<BaseResponse<bool>> AddOrUpdateConfigTemplateMargin(ConfigTemplateMarginModel request);
        #endregion

        #region TOP FIXTURES
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        #endregion

        Task<BaseResponse<List<CurrenciesResponse>>> GetCurrencies();
        Task<BaseResponse<ClientListResponse>> GetClientList(GetClientListRequest request, List<string> brands = null);

        Task<BaseResponse<TranslationResponse>> GetTranslationsByLanguageCode(LanguageCode langCode);

        #region Translation
        Task<BaseResponse<CopyClientConfigResponse>> CopyClientConfig(CopyClientConfigRequest request);
        Task<BaseResponse<bool>> CachePreload(OnDeleteCacheMessage request);
        Task<BaseResponse<List<SportsbookTranslationResponse>>> GetSportsbookTranslations(string languageCode);
        Task<BaseResponse<TranslationStatsResponse>> GetSportsbookTranslationStats();
        Task<BaseResponse<bool>> AddOrUpdateSportsbookTranslation(AddOrUpdateSportsbookTranslationRequest request, RequesterInfo requesterInfo);
        Task<BaseResponse<bool>> DeleteSportsbookTranslation(DeleteSportsbookTranslationRequest request, RequesterInfo requesterInfo);
        Task<BaseResponse<bool>> DeleteSportsbookTranslationKey(DeleteSportsbookTranslationKeyRequest request, RequesterInfo requesterInfo);
        Task<BaseResponse<SportsbookGlossaryListResponse>> GetSportsbookGlossary(GetSportsbookGlossaryRequest request);
        Task<BaseResponse<TranslationStatsResponse>> GetSportsbookGlossaryStats();
        Task<BaseResponse<bool>> AddOrUpdateSportsbookGlossary(AddOrUpdateSportsbookGlossaryRequest request, RequesterInfo requesterInfo);
        Task<BaseResponse<bool>> SetSportsbookGlossaryProcessed(SetSportsbookGlossaryProcessedRequest request, RequesterInfo requesterInfo);
        Task<BaseResponse<bool>> DeleteSportsbookGlossary(DeleteSportsbookGlossaryRequest request, RequesterInfo requesterInfo);
        Task<BaseResponse<List<SportsbookGlossaryExportRow>>> ExportSportsbookGlossary(string searchText, string targetLanguage = null, bool untranslatedOnly = false, bool exactMatch = false);
        Task<BaseResponse<ImportSportsbookGlossaryResponse>> ImportSportsbookGlossary(ImportSportsbookGlossaryRequest request, RequesterInfo requesterInfo);
        Task<BaseResponse<List<SportsbookTranslationExportRow>>> ExportSportsbookTranslations();
        Task<BaseResponse<ImportSportsbookTranslationResponse>> ImportSportsbookTranslations(ImportSportsbookTranslationRequest request, RequesterInfo requesterInfo);
        #endregion
    }
}
