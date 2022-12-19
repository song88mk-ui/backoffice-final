using System;
using System.Collections.Generic;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Configuration;
using static Backoffice.Shared.Models.Response.GetAllLeaguesResponse;

namespace Backoffice.API.Repositories
{
    public class TradingRepository : ITradingRepository
    {
        private readonly IConfiguration _configuration; 
        private readonly HttpClient _httpClient;

        public TradingRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        #region SPORTS
        public async Task<BaseResponse<SportsTreeResponse>> GetSports(int fixtureTypeId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"sports2/{fixtureTypeId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse <SportsTreeResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportsTreeResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"sports/topmarkets/{sportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<List<MainMarketTemplateResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<MainMarketTemplateResponse>>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"sports/lookup");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<SportsLookupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportsLookupResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Segment
        public async Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"segment/lookup/{sportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<SegmentLookupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SegmentLookupResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region League
        public async Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"league/lookup/{sportId}/{segmentId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<LeagueLookupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<LeagueLookupResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Fixtures
        public async Task<BaseResponse<MarketFixtureResponse>> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<MarketFixtureResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}/{marketTemplateId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<MarketFixtureResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        
        public async Task<BaseResponse<FixtureOddsHistoryResponse>> GetFixtureMarketSelections(int fixtureTypeId, string providerFixtureId, string providerMarketId, string providerSelectionId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"fixtures/market/selections/{fixtureTypeId}/{providerFixtureId}/{providerMarketId}/{providerSelectionId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<FixtureOddsHistoryResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FixtureOddsHistoryResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<FixtureOddsHistoryResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<FixtureOddsHistoryResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketFixtureDetailResponse>> GetFixtureMarketDetails(string providerFixtureId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"fixtures2/market/details/{providerFixtureId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureDetailResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureDetailResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<MarketFixtureDetailResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureDetailResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Client
        #endregion

        #region Sportsbook
        #endregion

        #region Priceboost
        public async Task<Tuple<BaseResponse<PriceBoostConfigResponse>, OnActivityLogListMessage>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"priceboost", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<Tuple<BaseResponse<PriceBoostConfigResponse>, OnActivityLogListMessage>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<Tuple<BaseResponse<PriceBoostConfigResponse>, OnActivityLogListMessage>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new Tuple<BaseResponse<PriceBoostConfigResponse>, OnActivityLogListMessage>(new BaseResponse<PriceBoostConfigResponse>()
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        }, null);
                    }
                }
            }
            catch (Exception ex)
            {
                return new Tuple<BaseResponse<PriceBoostConfigResponse>, OnActivityLogListMessage>(new BaseResponse<PriceBoostConfigResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                }, null);
            }
        }

        public async Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"priceboost/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigModel>>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigModel>>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<List<PriceBoostConfigModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<PriceBoostConfigModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"priceboost/settings/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<List<BoostedSettingsModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<BoostedSettingsModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateBoostedSettings(BoostedSettingsModel request, string userId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"priceboost/settings?userId={userId}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<PriceBoostConfigAuditModel>>> GetPriceBoostAuditList(PriceBoostConfigRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"priceboost/audit/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigAuditModel>>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigAuditModel>>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<List<PriceBoostConfigAuditModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<PriceBoostConfigAuditModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Activity Log
        public async Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"activitylog", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"activitylog/priceboostlist", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<ActivityLogResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"activitylog/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<ActivityLogResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion


        #region Utilities
        private HttpClient GetHttpClient(string region)
        {
            var uri = GetBaseUrl(region);
            if (uri.LastOrDefault() != '/')
                uri += "/";

            var client = new HttpClient()
            {
                BaseAddress = new Uri(Path.Combine(GetBaseUrl(region), "api/v1/trading/"))
            };
            client.DefaultRequestHeaders.Add("x-api-key", GetApikey(region));

            return client;
        }
        private string GetBaseUrl(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:TradingAPI:SA:Uri"];
            else
                return _configuration[$"API:TradingAPI:HK:Uri"];
        }
        private string GetApikey(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:TradingAPI:SA:APIKey"];
            else
                return _configuration[$"API:TradingAPI:HK:APIKey"];
        }
        #endregion
    }
}