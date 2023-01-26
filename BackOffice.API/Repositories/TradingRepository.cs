using System;
using System.Collections.Generic;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Text.Json;
using Newtonsoft.Json;
using Backoffice.Shared.Data.Backoffice.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Repositories
{
    public class TradingRepository : ITradingRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IRabbitMQRepository _rabbitMqRepository;

        public TradingRepository(IConfiguration configuration, IRabbitMQRepository rabbitMqRepository)
        {
            _configuration = configuration;
            _rabbitMqRepository = rabbitMqRepository;
        }

        #region SPORTS
        public async Task<BaseResponse<SportsTreeResponse>> GetSports(int fixtureTypeId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"sports2/{fixtureTypeId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<SportsTreeResponse>
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

        public async Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"sports/lookup");

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
        public async Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"segment/lookup/{sportId}");

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
        public async Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"league/lookup/{sportId}/{segmentId}");

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
        public async Task<BaseResponse<MarketFixtureResponse>> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}");

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

        public async Task<BaseResponse<MarketFixtureDetailResponse>> GetFixtureMarketDetails(string providerFixtureId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"fixtures2/market/details/{providerFixtureId}");

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
        public async Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/sportsettings", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientSportSettingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientSportSettingResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientSportSettingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientSportSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/sportorder/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientSportOrderListResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientSportOrderListResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/sportorder/audit/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderAuditResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderAuditResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientSportOrderAuditResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientSportOrderAuditResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/segmentorder/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientSegmentOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientSegmentOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientSegmentOrderListResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientSegmentOrderListResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/leagueorder/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueOrderListResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ClientLeagueOrderListResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientLeagueOrderListResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(string clientCode, int sportId, int fixtureStatusId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"client/marketgroup/{clientCode}/{sportId}/{fixtureStatusId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"client/markettemplate/{clientCode}/{sportId}/{fixtureStatusId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientMarketTemplateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/markettemplate", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientMarketTemplateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/markettemplate/copy", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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
        public async Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/markettemplategroup/post", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientPostMarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientPostMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/markettemplategroup/delete", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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
        public async Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/markettemplategroup/copy", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
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
                var activityLogList = new OnActivityLogListMessage();
                var response = await GetHttpClient(region).PostAsync($"manual/messagesettlement", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = string.Empty;
                    foreach (var item in request)
                    {
                        activityLogList.OnActivityLogMessages.Add(item.OnActivityLogMessage);
                    }

                    await _rabbitMqRepository.Publish(new MessageRequest()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "LogActivityList",
                        RoutingKey = $"r.bo.util.logs",
                        Payload = JsonSerializer.Serialize(activityLogList)
                    }, region);

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


        #region Utilities
        private HttpClient GetHttpClient(string region)
        {
            var uri = GetBaseUrl(region);
            if (uri.LastOrDefault() != '/')
                uri += "/";

            var client = new HttpClient()
            {
                BaseAddress = new Uri($"{uri}api/v1/trading/")
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