using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backoffice.API.Repositories
{
    public class TradingRepository : ITradingRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IBackofficeActivityLogRepository _activityLogRepository;
        private readonly IMemCache _memCache;

        public TradingRepository(IConfiguration configuration, IBackofficeActivityLogRepository activityLogRepository, IMemCache memCache)
        {
            _configuration = configuration;
            _activityLogRepository = activityLogRepository;
            _memCache = memCache;
        }

        #region SPORTS
        public async Task<BaseResponse<SportsTreeResponse>> GetSports(int fixtureTypeId, string region, string langCode)
        {
            try
            {
                //var key = $"sp-{langCode}-{fixtureTypeId}";

                //var cache = _memCache.GetCache<BaseResponse<SportsTreeResponse>>(key);

                //if (cache != null && cache.StatusCode == StatusCode.Success)
                //    return cache;

                var result = new BaseResponse<SportsTreeResponse>();

                var response = await GetHttpClientV2(region, langCode).GetAsync($"sports/tree/{fixtureTypeId}");

                if (response.IsSuccessStatusCode)
                    result = JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        result = JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                        result = new BaseResponse<SportsTreeResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                }

                //_memCache.AddOrUpdateCache(key, result, 24);
                return result;
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

        public async Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, string region, string langCode)
        {
            try
            {
                var response = await GetHttpClient(region, langCode).GetAsync($"sports/topmarkets/{sportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
        public async Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string langCode, string region = "")
        {
            try
            {
                var response = await GetHttpClient(region, langCode).GetAsync($"sports/lookup");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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

        public async Task<BaseResponse<SportSelectorMarketTemplateResponse>> GetSelectorMarketTemplates(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"sports/selectormarkettemplates");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportSelectorMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportSelectorMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SportSelectorMarketTemplateResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportSelectorMarketTemplateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Segment
        public async Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string langCode, string region = "")
        {
            try
            {
                var response = await GetHttpClient(region, langCode).GetAsync($"segment/lookup/{sportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
        public async Task<BaseResponse<LeagueLookupResponseV2>> GetAllLeagueLookup(string sportIds, string region = "")
        {
            try
            {
                var payload = new
                {
                    SportIds = sportIds
                };
                var response = await GetHttpClient(region).PostAsync($"league/lookup", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                BaseResponse<LeagueLookupResponseV2> result;
                if (response.IsSuccessStatusCode)
                    result = JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        result = JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        result = new BaseResponse<LeagueLookupResponseV2>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return new BaseResponse<LeagueLookupResponseV2>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string langCode, string region = "")
        {
            try
            {
                var response = await GetHttpClient(region, langCode).GetAsync($"league/lookup/{sportId}/{segmentId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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

        public async Task<BaseResponse<List<TierLeagueOrderResponse>>> GetTierLeagueOrderList(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"tier/leagueorder/list");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<TierLeagueOrderResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<TierLeagueOrderResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<TierLeagueOrderResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<TierLeagueOrderResponse>>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Fixtures
        public async Task<BaseResponse<MarketFixtureResponseV2>> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId, string region, string langCode = "")
        {
            try
            {
                var response = await GetHttpClient(region, langCode).GetAsync($"fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MarketFixtureResponseV2>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureResponseV2>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<MarketFixtureResponseV2>> GetMarketFixtures(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId, string region, string langCode = "")
        {
            try
            {
                var response = await GetHttpClient(region, langCode).GetAsync($"fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}/{marketTemplateId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MarketFixtureResponseV2>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureResponseV2>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<MarketFixtureResponseV2>> GetMostEvenLineMarketFixtures(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId, string region, string langCode = "")
        {
            try
            {
                var response = await GetHttpClientV2(region, langCode).GetAsync($"fixtures/{fixtureTypeId}/sports/{sportId}/segments/{segmentId}/leagues/{leagueId}/markets/{marketTemplateId}/most-even-line");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MarketFixtureResponseV2>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureResponseV2>()
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
                    return JsonSerializer.Deserialize<BaseResponse<FixtureOddsHistoryResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FixtureOddsHistoryResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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

        public async Task<BaseResponse<MarketFixtureDetailResponse>> GetFixtureMarketDetails(string providerFixtureId, string region, string langCode = "")
        {
            try
            {
                var response = await GetHttpClient(region, langCode).GetAsync($"fixtures2/market/details/{providerFixtureId}");

                // Get the response size in bytes
                //var content = await response.Content.ReadAsStringAsync();
                //var responseSize = Encoding.UTF8.GetByteCount(content) / (1024.0 * 1024.0);
                //Console.WriteLine($"Response Size: {responseSize} bytes");
                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureDetailResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureDetailResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
        public async Task<BaseResponse<List<ClientInformationModel>>> GetClientList(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"client");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientInformationModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientInformationModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientInformationModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientInformationModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ConfigModel>> GetClientConfig(ClientConfigRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/config/all", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ConfigModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ConfigModel>> GetClientConfigv2(ClientConfigRequestv2 request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/config/all", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ConfigModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientConfigResponse>> AddOrUpdateConfig(ConfigSettingsModel request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/config", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientConfigResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientConfigResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientConfigResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientConfigResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>> GetConfigLogs(ConfigLogsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/config/audit", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientLeagueConfig>>> GetClientLeagueConfig(string clientCode, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"client/leagueconfig?clientCode={clientCode}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueConfig>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueConfig>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientLeagueConfig>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientLeagueConfig>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientMarketConfig>>> GetClientMarketConfig(string clientCode, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"client/marketconfig?clientCode={clientCode}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientMarketConfig>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientMarketConfig>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientMarketConfig>>> GetClientMarketConfig(int pageSize, int page, string clientCode, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"client/marketconfig/{pageSize}/{page}?clientCode={clientCode}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientMarketConfig>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientMarketConfig>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        //public async Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/sportsettings", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //        {
        //            await Publish(new MessageRequest
        //            {
        //                Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
        //                {
        //                    ClientCode = request.ClientCode,
        //                    CacheType = CacheType.SportSortOrder
        //                }),
        //                CorrelationId = Guid.NewGuid().ToString().ToUpper(),
        //                MessageType = "OnSportSortOrderChange",
        //                RoutingKey = $"r.oe.cachesync.onsportsortorderchange"
        //            }, "HK");

        //            return JsonSerializer.Deserialize<BaseResponse<ClientSportSettingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        }
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<ClientSportSettingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<ClientSportSettingResponse>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<ClientSportSettingResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}

        //public async Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request, string langCode, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region, langCode).PostAsync($"client/sportorder/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<List<ClientSportOrderListResponse>>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<List<ClientSportOrderListResponse>>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/sportorder/audit/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderAuditResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderAuditResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<List<ClientSportOrderAuditResponse>>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<List<ClientSportOrderAuditResponse>>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/segmentorder/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<List<ClientSegmentOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<List<ClientSegmentOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<List<ClientSegmentOrderListResponse>>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<List<ClientSegmentOrderListResponse>>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/leagueorder/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<List<ClientLeagueOrderListResponse>>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<List<ClientLeagueOrderListResponse>>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(string clientCode, int sportId, int fixtureStatusId, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).GetAsync($"client/marketgroup/{clientCode}/{sportId}/{fixtureStatusId}");

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<ClientMarketTemplateGroupResponse>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<ClientMarketTemplateGroupResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string langCode, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region, langCode).GetAsync($"client/markettemplate/{clientCode}/{sportId}/{fixtureStatusId}");

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<ClientMarketTemplateResponse>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<ClientMarketTemplateResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/markettemplate", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //        {
        //            await Publish(new MessageRequest
        //            {
        //                Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
        //                {
        //                    ClientId = request.ClientId,
        //                    ClientCode = request.ClientCode,
        //                    CacheType = CacheType.MarketSortOrder
        //                }),
        //                CorrelationId = Guid.NewGuid().ToString().ToUpper(),
        //                MessageType = "OnMarketSortOrderChange",
        //                RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
        //            }, "HK");

        //            return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        }
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<ClientMarketTemplateResponse>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<ClientMarketTemplateResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/markettemplate/copy", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<bool>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<bool>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}

        #endregion

        #region Sportsbook
        public async Task<BaseResponse<MarketFixtureResponse>> GetFixtures(MarketFixtureRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"sportsbook/fixtures", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketFixtureResponse>> GetOutrightFixtures(MarketFixtureRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"sportsbook/outrightfixtures", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Priceboost
        public async Task<BaseResponse<PriceBoostConfigResponse>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"priceboost", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PriceBoostConfigResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PriceBoostConfigResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<PriceBoostConfigResponse>()
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<PriceBoostConfigResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"priceboost/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                    return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                    return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigAuditModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigAuditModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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

        #region BET
        public async Task<BaseResponse<BetSelectionDetailsResponse>> GetBetSelections(BetSelectionDetailsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"bet/selections", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<BetSelectionDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<BetSelectionDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<BetSelectionDetailsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<BetSelectionDetailsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"bet/details", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ManualBetDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualBetDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ManualBetDetailsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ManualBetDetailsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region FASTODDS
        public async Task<BaseResponse<FastOddsSportsResponse>> GetFastOddsSports(int fixtureTypeId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"fastodds/sports/{fixtureTypeId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<FastOddsSportsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FastOddsSportsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<FastOddsSportsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<FastOddsSportsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<FastOddsFixturesResponse>> GetFixtures(FastOddsFixturesRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"fastodds/fixtures", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<FastOddsFixturesResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FastOddsFixturesResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<FastOddsFixturesResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<FastOddsFixturesResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Manual
        public async Task<BaseResponse<ManualMarketSettlementResponse>> AddOrUpdateManualMarketSettlement(List<ManualMarketSettlementRequest> request, string userId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"manual/marketsettlement?userId={userId}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ManualMarketSettlementResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ManualMarketSettlementResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ManualMarketSettlementDetailsResponse>> GetManualMarketSettlementDetails(ManualMarketSettlementDetailsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"manual/marketsettlement/details", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ManualMarketSettlementDetailsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ManualMarketSettlementDetailsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<string>> AddManualMessageSettlement(List<MessageSettlementRequest> request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"manual/messagesettlement", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var activityLogList = new OnActivityLogListMessage();
                    activityLogList.OnActivityLogMessages.AddRange(request.Select(c => new OnActivityLogMessage()
                    {
                        Id = c.OnActivityLogMessage.Id,
                        Page = c.OnActivityLogMessage.Page,
                        Payload = c.OnActivityLogMessage.Payload,
                        Activity = c.OnActivityLogMessage.Activity,
                        From = c.OnActivityLogMessage.From,
                        To = c.OnActivityLogMessage.To,
                        UserId = c.OnActivityLogMessage.UserId,
                        UserName = c.OnActivityLogMessage.UserName,
                        TimeStamp = c.OnActivityLogMessage.TimeStamp
                    }).ToList());
                    await _activityLogRepository.ProcessActivityLog(activityLogList);

                    return JsonSerializer.Deserialize<BaseResponse<string>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<string>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<string>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ManualMessageSettlementAuditResponse>> GetMessageSettlementAudit(ManualMessageSettlementRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"manual/messagesettlement/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ManualMessageSettlementAuditResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualMessageSettlementAuditResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ManualMessageSettlementAuditResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ManualMessageSettlementAuditResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        public async Task<BaseResponse<MessageResponse>> Publish(MessageRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"publish", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MessageResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<BookmakerConfigurationResponse>> GetBookmakers(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"bookmakers");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<BookmakerConfigurationResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<BookmakerConfigurationResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<BookmakerConfigurationResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<BookmakerConfigurationResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<Tuple<List<NotificationResponse>, MetaData>>> GetNotifications(NotificationsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"notifications", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<Tuple<List<NotificationResponse>, MetaData>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<Tuple<List<NotificationResponse>, MetaData>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<Tuple<List<NotificationResponse>, MetaData>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<Tuple<List<NotificationResponse>, MetaData>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"markettemplateplayerprops", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<MarketTemplatePlayerPropsResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<MarketTemplatePlayerPropsResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups(int fixtureTypeId, string region)
        {
            try
            {

                var response = await GetHttpClient(region).GetAsync($"markettemplategroups/{fixtureTypeId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketLookupResponse>> GetMarketLookup(int fixtureTypeId, int sportId, string region)
        {
            try
            {

                var response = await GetHttpClient(region).GetAsync($"marketlookup/{fixtureTypeId}/{sportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketLookupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MarketLookupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketLookupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> DeactivateFixtureMarkets(DeactivateFixtureMarketRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"fixture/market/deactivate", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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

        #region Mapping

        public async Task<BaseResponse<SportMappingItemModel>> GetSportMappingById(string region, string sportId)
        {//
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"mapping/sports/{sportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportMappingItemModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportMappingItemModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SportMappingItemModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportMappingItemModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteSportMappingById(string region, string sportId, string userId)
        {
            try
            {
                var response = await GetHttpClientV2(region).DeleteAsync($"mapping/sports/{sportId}/{userId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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

        public async Task<BaseResponse<SegmentMappingItemModel>> GetSegmentMappingById(string region, string segmentId)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"mapping/segments/{segmentId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SegmentMappingItemModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SegmentMappingItemModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SegmentMappingItemModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SegmentMappingItemModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<LeagueMappingItemModel>> GetLeagueMappingById(string region, string leagueId)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"mapping/leagues/{leagueId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<LeagueMappingItemModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<LeagueMappingItemModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<LeagueMappingItemModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<LeagueMappingItemModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<FixtureMappingModel>> GetFixtureMappingById(string region, string fixtureId)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"mapping/fixtures/{fixtureId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<FixtureMappingModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FixtureMappingModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<FixtureMappingModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<FixtureMappingModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketMappingModel>> GetMarketMappingById(string region, string marketId)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"mapping/markets/{marketId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketMappingModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketMappingModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MarketMappingModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketMappingModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<SelectionMappingModel>> GetSelectionMappingById(string region, string selectionId)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"mapping/selections/{selectionId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SelectionMappingModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SelectionMappingModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SelectionMappingModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SelectionMappingModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<SportMappingResponse>> GetSportMapping(string region, MappingRequest request)
        {
            try
            {
                var response = await GetHttpClientV2(region).PostAsync($"mapping/sports", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SportMappingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportMappingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<SegmentMappingResponse>> GetSegmentMapping(string region, MappingRequest request)
        {
            try
            {
                var response = await GetHttpClientV2(region).PostAsync($"mapping/segments", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SegmentMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SegmentMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SegmentMappingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SegmentMappingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<LeagueMappingResponse>> GetLeagueMapping(string region, MappingRequest request)
        {
            try
            {
                var response = await GetHttpClientV2(region).PostAsync($"mapping/leagues", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<LeagueMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<LeagueMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<LeagueMappingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<LeagueMappingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<FixtureMappingResponse>> GetFixtureMapping(string region, MappingRequest request)
        {
            try
            {
                var response = await GetHttpClientV2(region).PostAsync($"mapping/fixtures", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<FixtureMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FixtureMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<FixtureMappingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<FixtureMappingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketMappingResponse>> GetMarketMapping(string region, MappingRequest request)
        {
            try
            {
                var response = await GetHttpClientV2(region).PostAsync($"mapping/markets", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MarketMappingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketMappingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<SelectionMappingResponse>> GetSelectionMapping(string region, MappingRequest request)
        {
            try
            {
                var response = await GetHttpClientV2(region).PostAsync($"mapping/selections", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SelectionMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SelectionMappingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SelectionMappingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SelectionMappingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<UnmappedResponse>> GetUnmappedSports(string region)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"unmapped/sports");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<UnmappedResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<UnmappedResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<UnmappedResponse>> GetUnmappedSegment(string region)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"unmapped/segments");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<UnmappedResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<UnmappedResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<UnmappedResponse>> GetUnmappedLeague(string region)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"unmapped/leagues");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<UnmappedResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<UnmappedResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<UnmappedResponse>> GetUnmappedFixture(string region)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"unmapped/fixtures");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<UnmappedResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<UnmappedResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<UnmappedResponse>> GetUnmappedMarket(string region)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"unmapped/markets");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<UnmappedResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<UnmappedResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<UnmappedResponse>> GetUnmappedSelection(string region)
        {
            try
            {
                var response = await GetHttpClientV2(region).GetAsync($"unmapped/selections");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<UnmappedResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<UnmappedResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateProviderMapping(string region, string category, ProviderMappingRequest request)
        {
            try
            {
                var validate = ValidateMappingRequest(category, request);
                if (!string.IsNullOrEmpty(validate))
                    return new BaseResponse<bool>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({validate})"
                    };

                var response = await GetHttpClientV2(region).PostAsync($"mapping/{category}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                BaseResponse<bool> result;
                if (response.IsSuccessStatusCode)
                    result = JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        result = JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        result = new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateProviderMappingBulk(string region, string category, ProviderMappingBulkRequest request)
        {
            try
            {
                var messageType = category switch
                {
                    "sport" => "OnSportBulkUpdate",
                    "segment" => "OnSegmentBulkUpdate",
                    "league" => "OnLeagueBulkUpdate",
                    "fixture" => "OnFixtureBulkUpdate",
                    _ => "default"
                };

                var validate = ValidateMappingRequestBulk(category, request);
                if (!string.IsNullOrEmpty(validate))
                    return new BaseResponse<bool>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({validate})"
                    };

                await Publish(new MessageRequest
                {
                    Payload = JsonSerializer.Serialize(request),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = messageType,
                    RoutingKey = $"r.bo.mapping.onbulkupdate"
                }, "HK");

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Success,
                    Result = true,
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Utilities
        private HttpClient GetHttpClient(string region, string langCode = "")
        {
            var uri = GetBaseUrl(region);
            if (uri.LastOrDefault() != '/')
                uri += "/";

            var client = new HttpClient()
            {
                BaseAddress = new Uri($"{uri}api/v1/trading/")
            };
            client.DefaultRequestHeaders.Add("x-api-key", GetApikey(region));

            client.DefaultRequestHeaders.Remove("x-language-code");
            if (!string.IsNullOrEmpty(langCode))
                client.DefaultRequestHeaders.Add("x-language-code", langCode);

            return client;
        }
        private HttpClient GetHttpClientV2(string region, string langCode = "")
        {
            var uri = GetBaseUrl(region);
            if (uri.LastOrDefault() != '/')
                uri += "/";

            var client = new HttpClient()
            {
                BaseAddress = new Uri($"{uri}api/v2/trading/")
            };
            client.DefaultRequestHeaders.Add("x-api-key", GetApikey(region));

            client.DefaultRequestHeaders.Remove("x-language-code");
            if (!string.IsNullOrEmpty(langCode))
                client.DefaultRequestHeaders.Add("x-language-code", langCode);

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

        private string ValidateMappingRequest(string category, ProviderMappingRequest request)
        {
            var result = string.Empty;

            if (string.IsNullOrEmpty(request.UserId) || !Guid.TryParse(request.UserId, out var userId))
                return $"Invalid User ID";

            if (!string.IsNullOrEmpty(request.Mapping.Id) && !int.TryParse(request.Mapping.Id, out int id))
                return $"Invalid {category} ID";

            switch (category)
            {
                case "sport":
                    break;
                case "segment":
                    break;
                case "league":
                    break;
                case "fixture":
                    break;
                default:
                    result = "Invalid category";
                    break;
            }

            return result;
        }

        private string ValidateMappingRequestBulk(string category, ProviderMappingBulkRequest request)
        {
            var result = string.Empty;

            if(string.IsNullOrEmpty(request.UserId) || !Guid.TryParse(request.UserId, out var userId))
                return $"Invalid User ID";


            if (!request.Mapping.Any())
                return $"No {category} mapping to be updated";

            switch (category)
            {
                case "sport":
                    foreach (var mapping in request.Mapping)
                    {
                        if (!string.IsNullOrEmpty(mapping.Id))
                        {
                            result = "ID should be empty for Bulk Upload";
                            break;
                        }
                    }
                    break;
                case "segment":
                    foreach (var mapping in request.Mapping)
                    {
                        if (!string.IsNullOrEmpty(mapping.Id))
                        {
                            result = "ID should be empty for Bulk Upload";
                            break;
                        }
                    }
                    break;
                case "league":
                    foreach (var mapping in request.Mapping)
                    {
                        if (!string.IsNullOrEmpty(mapping.Id))
                        {
                            result = "ID should be empty for Bulk Upload";
                            break;
                        }
                    }
                    break;
                case "fixture":
                    foreach (var mapping in request.Mapping)
                    {
                        if (!int.TryParse(mapping.Id, out int id))
                        {
                            result = "Invalid Fixture ID";
                            break;
                        }
                    }
                    break;
                default:
                    result = "Invalid category";
                    break;
            }

            return result;
        }
        #endregion
    }
}