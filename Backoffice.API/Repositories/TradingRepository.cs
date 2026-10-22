using Backoffice.Shared.Data.Sportsbook.Collections;
using Backoffice.Shared.Data.Trading;
using Backoffice.Shared.Data.Trading.SQLEntities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using OE.Shared.Data.Sportsbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using JsonSerializer = System.Text.Json.JsonSerializer;
using TierLeagueOrder = OE.Shared.Data.Sportsbook.SQLEntities.TierLeagueOrder;

namespace Backoffice.API.Repositories
{
    public class TradingRepository : ITradingRepository
    {
        private static IMongoDatabase _db;
        private readonly IConfiguration _configuration;
        private readonly IBackofficeActivityLogRepository _activityLogRepository;
        private readonly IMemCache _memCache;
        private readonly IRabbitMqRepository _rabbitMqRepository;
        private readonly ILogger<TradingRepository> _logger;
        private readonly TradingContext _tradingContext;
        private readonly SportsbookContext _sportsbookContext;

        private static IMongoCollection<Fixture> fixtureCollection;
        private static IMongoCollection<Market> marketCollection;
        private static IMongoCollection<Selection> selectionCollection;

        public TradingRepository(IConfiguration configuration, IBackofficeActivityLogRepository activityLogRepository, IMemCache memCache, ILogger<TradingRepository> logger, IRabbitMqRepository rabbitMqRepository, TradingContext tradingContext, SportsbookContext sportsbookContext)
        {
            _configuration = configuration;
            _activityLogRepository = activityLogRepository;
            _memCache = memCache;
            _logger = logger;
            _rabbitMqRepository = rabbitMqRepository;
            _tradingContext = tradingContext;
            _sportsbookContext = sportsbookContext;

            if (_db == null)
            {
                var client = new MongoClient(_configuration["Custom:MongoDB-Sportsbook"]);
                _db = client.GetDatabase("Sportsbook");

                fixtureCollection = _db.GetCollection<Fixture>("Fixture").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                marketCollection = _db.GetCollection<Market>("Market").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                selectionCollection = _db.GetCollection<Selection>("Selection").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
            }

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
                    result = JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        result = JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SportSelectorMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportSelectorMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    result = JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        result = JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<List<TierLeagueOrderResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<TierLeagueOrderResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<FixtureOddsHistoryResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FixtureOddsHistoryResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureDetailResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureDetailResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
        //public async Task<BaseResponse<List<ClientInformationModel>>> GetClientList(string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).GetAsync($"client");

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<List<ClientInformationModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<List<ClientInformationModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<List<ClientInformationModel>>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<List<ClientInformationModel>>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        public async Task<BaseResponse<ConfigModel>> GetClientConfig(ClientConfigRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/config/all", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<ClientConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                        return JsonSerializer.Deserialize<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueConfig>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueConfig>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientMarketConfig>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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

        #endregion

        #region Sportsbook
        // BO-42: read from the sportsbook Mongo here, no longer through the Trading API. The region argument stays
        // so the controller and interface do not change.
        public async Task<BaseResponse<MarketFixtureResponse>> GetFixtures(MarketFixtureRequest request, string region)
        {
            try
            {
                // all statuses up to Closed when asked, else the fixtures that have not started
                var lastStatus = request.AllFixtureStatus ? (int)FixtureStatus.Closed : (int)FixtureStatus.Prematch;

                var fixtures = await fixtureCollection
                    .Find(Builders<Fixture>.Filter.Where(SportsbookFixtureBuilder.MatchFixturePredicate(lastStatus, request.FixtureTypeId, request.SportId, request.SegmentId, request.LeagueId)))
                    .ToListAsync();

                if (fixtures.Count == 0)
                {
                    return new BaseResponse<MarketFixtureResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MarketFixtureResponse()
                    };
                }

                var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

                if (request.AllFixtureStatus)
                    marketStatusIds.Add((int)MarketStatus.Settled);

                var data = await GetFixtureMarketData(fixtures.Select(f => f.FiId).Distinct().ToList(), marketStatusIds);

                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new MarketFixtureResponse
                    {
                        Fixtures = SportsbookFixtureBuilder.BuildMatchFixtures(fixtures, data.Markets, data.Selections)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFixtures failed for sport {SportId}, segment {SegmentId}, league {LeagueId}", request?.SportId, request?.SegmentId, request?.LeagueId);

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
                var sportId = request.SportId;
                var segmentId = request.SegmentId;
                var leagueId = request.LeagueId;

                var fixtures = await fixtureCollection
                    .Find(Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch
                        && c.FiTyId == (int)FixtureType.Outright
                        && (sportId == 0 || c.SpId == sportId)
                        && (segmentId == 0 || c.SegId == segmentId)
                        && (leagueId == 0 || c.LeId == leagueId)))
                    .ToListAsync();

                if (fixtures.Count == 0)
                {
                    return new BaseResponse<MarketFixtureResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MarketFixtureResponse()
                    };
                }

                var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

                var data = await GetFixtureMarketData(fixtures.Select(f => f.FiId).Distinct().ToList(), marketStatusIds);

                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new MarketFixtureResponse
                    {
                        Fixtures = SportsbookFixtureBuilder.BuildOutrightFixtures(fixtures, data.Markets, data.Selections, GetTierLeagueOrder(), request.SportId)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOutrightFixtures failed for sport {SportId}, segment {SegmentId}, league {LeagueId}", request?.SportId, request?.SegmentId, request?.LeagueId);

                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        // The markets and selections of the fixtures in the given statuses, with only the English translation of
        // each name.
        private async Task<(List<MarketFilter> Markets, List<SelectionFilter> Selections)> GetFixtureMarketData(List<long> fixtureIds, List<int> marketStatusIds)
        {
            var english = LanguageCode.EN.ToString();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.IsBo).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == english);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.SeSp).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == english);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            return (markets.Result, selections.Result);
        }

        // The league order of the outright lists, cached 24 hours under the key the sportsbook repository uses.
        private List<TierLeagueOrder> GetTierLeagueOrder()
        {
            return _memCache.GetCache("tlo", () => _sportsbookContext.TierLeagueOrder.AsNoTracking().ToList(), 24) ?? new List<TierLeagueOrder>();
        }
        #endregion

        #region Priceboost
        // BO-42: served from the Trading DB and the sportsbook Mongo here, no longer through the Trading API.
        // The region argument stays so the controller and interface do not change.
        public async Task<BaseResponse<PriceBoostConfigResponse>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request, string region)
        {
            try
            {
                if (request == null || !request.Any())
                {
                    return new BaseResponse<PriceBoostConfigResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Result = new PriceBoostConfigResponse
                        {
                            StatusCode = StatusCode.Error,
                            Message = "No price boost config to be updated",
                            IsBoosted = false
                        }
                    };
                }

                var existingPriceBoostConfigs = new List<PriceBoostConfigModel>();

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    // A client boosts one selection per market: saving a different selection of the market replaces
                    // the saved one (the table has no key to enforce this). The Trading API only replaced when both the
                    // selection template and the selection specifier differed, which left two selections in one market
                    // when just one of them differed. The replaced boosts are returned so the Web can log them.
                    foreach (var priceBoost in request)
                    {
                        var replaced = await _tradingContext.PriceBoostConfig
                            .Where(c => c.ClientCode == priceBoost.ClientCode
                                && c.SportId == priceBoost.SportId
                                && c.SegmentId == priceBoost.SegmentId
                                && c.LeagueId == priceBoost.LeagueId
                                && c.FixtureTypeId == priceBoost.FixtureTypeId
                                && c.ProviderFixtureId == priceBoost.ProviderFixtureId
                                && c.MarketTemplateId == priceBoost.MarketTemplateId
                                && c.MarketSpecifier == priceBoost.MarketSpecifier
                                && (c.SelectionTemplateId != priceBoost.SelectionTemplateId
                                    || c.SelectionSpecifier != priceBoost.SelectionSpecifier))
                            .ToListAsync();

                        if (replaced.Any())
                        {
                            existingPriceBoostConfigs.AddRange(replaced.Select(ToPriceBoostConfigModel));
                            _tradingContext.PriceBoostConfig.RemoveRange(replaced);
                            await _tradingContext.SaveChangesAsync();
                        }
                    }

                    foreach (var newPriceBoost in request)
                    {
                        // odds are cut, not rounded, to 2 decimals
                        newPriceBoost.BoostedOdds = string.IsNullOrEmpty(newPriceBoost.BoostedOddsStr)
                            ? 0
                            : Math.Truncate(100 * Convert.ToDecimal(newPriceBoost.BoostedOddsStr, CultureInfo.InvariantCulture)) / 100;

                        var translations = JsonSerializer.Serialize(newPriceBoost.BoostedSelectionTranslations);

                        var current = await _tradingContext.PriceBoostConfig
                            .Where(c => c.ClientCode == newPriceBoost.ClientCode
                                && c.SportId == newPriceBoost.SportId
                                && c.SegmentId == newPriceBoost.SegmentId
                                && c.LeagueId == newPriceBoost.LeagueId
                                && c.FixtureTypeId == newPriceBoost.FixtureTypeId
                                && c.ProviderFixtureId == newPriceBoost.ProviderFixtureId
                                && c.MarketTemplateId == newPriceBoost.MarketTemplateId
                                && c.SelectionTemplateId == newPriceBoost.SelectionTemplateId
                                && c.MarketSpecifier == newPriceBoost.MarketSpecifier
                                && c.SelectionSpecifier == newPriceBoost.SelectionSpecifier)
                            .FirstOrDefaultAsync();

                        if (current != null)
                        {
                            if (newPriceBoost.BoostedOdds == 0)
                            {
                                _tradingContext.PriceBoostConfig.Remove(current);
                            }
                            else if (newPriceBoost.BoostedOdds != current.BoostedOdds
                                || !string.Equals(translations, current.BoostedSelectionTranslation, StringComparison.OrdinalIgnoreCase)
                                || !string.Equals(newPriceBoost.SelectionName, current.BoostedSelectionName, StringComparison.OrdinalIgnoreCase)
                                || !string.Equals(newPriceBoost.BoostedSelectionName, current.BoostedSelectionName, StringComparison.OrdinalIgnoreCase))
                            {
                                current.BoostedOdds = newPriceBoost.BoostedOdds;
                                current.UserId = newPriceBoost.UserId;
                                current.UserName = newPriceBoost.UserName;
                                current.TimeStamp = newPriceBoost.TimeStamp;
                                current.BoostedSelectionName = newPriceBoost.BoostedSelectionName;
                                current.BoostedSelectionTranslation = translations;
                                current.MarketSpecifier = newPriceBoost.MarketSpecifier;
                            }
                        }
                        else if (newPriceBoost.BoostedOdds != 0)
                        {
                            _tradingContext.PriceBoostConfig.Add(new PriceBoostConfig
                            {
                                ClientCode = newPriceBoost.ClientCode,
                                SportId = newPriceBoost.SportId,
                                SegmentId = newPriceBoost.SegmentId,
                                LeagueId = newPriceBoost.LeagueId,
                                FixtureTypeId = newPriceBoost.FixtureTypeId,
                                ProviderFixtureId = newPriceBoost.ProviderFixtureId,
                                MarketTemplateId = newPriceBoost.MarketTemplateId,
                                SelectionTemplateId = newPriceBoost.SelectionTemplateId,
                                SelectionSpecifier = newPriceBoost.SelectionSpecifier,
                                BoostedOdds = newPriceBoost.BoostedOdds,
                                UserId = newPriceBoost.UserId,
                                UserName = newPriceBoost.UserName,
                                TimeStamp = newPriceBoost.TimeStamp,
                                BoostedSelectionName = newPriceBoost.BoostedSelectionName,
                                BoostedSelectionTranslation = translations,
                                MarketSpecifier = newPriceBoost.MarketSpecifier
                            });
                        }
                    }

                    await _tradingContext.SaveChangesAsync();

                    scope.Complete();
                }

                // which clients now boost this fixture, and its market, for the Web to tell the engine about
                var providerFixtureId = request.Select(s => s.ProviderFixtureId).FirstOrDefault();
                var marketTemplateId = request.Select(s => s.MarketTemplateId).FirstOrDefault();

                var boosted = await _tradingContext.PriceBoostConfig.AsNoTracking()
                    .Where(p => p.ProviderFixtureId == providerFixtureId && p.BoostedOdds != 0)
                    .Select(p => new { p.ClientCode, p.MarketTemplateId })
                    .ToListAsync();

                return new BaseResponse<PriceBoostConfigResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new PriceBoostConfigResponse
                    {
                        IsBoosted = boosted.Any(),
                        ExistingPriceBoostConfigs = existingPriceBoostConfigs,
                        ClientBoosted = string.Join("|", boosted.Select(s => s.ClientCode).Distinct()),
                        ClientMarketBoosted = string.Join("|", boosted.Where(s => s.MarketTemplateId == marketTemplateId).Select(s => s.ClientCode).Distinct())
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddOrUpdatePriceBoost failed for {Count} price boost configs", request?.Count ?? 0);

                return new BaseResponse<PriceBoostConfigResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        // The saved boosts that match the request, with the live fixture, market and odds of each. A boost whose
        // fixture is no longer in Mongo has ended: it is not listed and it is deleted here (a write on a read, as
        // the Trading API did).
        public async Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request, string region)
        {
            try
            {
                var sportId = ParseIdFilter(request.SportId);
                var segmentId = ParseIdFilter(request.SegmentId);
                var leagueId = ParseIdFilter(request.LeagueId);

                var saved = await _tradingContext.PriceBoostConfig.AsNoTracking()
                    .Where(c => (string.IsNullOrEmpty(request.ClientCode) || c.ClientCode == request.ClientCode)
                        && (sportId == null || c.SportId == sportId)
                        && (segmentId == null || c.SegmentId == segmentId)
                        && (leagueId == null || c.LeagueId == leagueId)
                        && (string.IsNullOrEmpty(request.ProviderFixtureId) || c.ProviderFixtureId == request.ProviderFixtureId)
                        && (string.IsNullOrEmpty(request.MarketTemplateId) || c.MarketTemplateId == request.MarketTemplateId)
                        && (string.IsNullOrEmpty(request.MarketSpecifier) || c.MarketSpecifier == request.MarketSpecifier)
                        && (string.IsNullOrEmpty(request.SelectionTemplateId) || c.SelectionTemplateId == request.SelectionTemplateId)
                        && c.BoostedOdds != 0)
                    .ToListAsync();

                var priceBoost = saved.Select(ToPriceBoostConfigModel).ToList();

                // nothing saved yet: look up the requested fixture and market so an empty row can be offered
                var providerFixtureIds = priceBoost.Select(s => s.ProviderFixtureId).Distinct().ToList();
                if (!providerFixtureIds.Any())
                    providerFixtureIds.Add(request.ProviderFixtureId ?? string.Empty);

                var marketTemplateIds = priceBoost.Select(s => s.MarketTemplateId).Distinct().ToList();
                if (!marketTemplateIds.Any())
                    marketTemplateIds.Add(request.MarketTemplateId ?? string.Empty);

                var mongo = await GetPriceBoostMongoData(providerFixtureIds, marketTemplateIds);

                PriceBoostListBuilder.NormalizeOutrightSelections(mongo.Fixtures, mongo.Selections);

                var clients = await _tradingContext.Client.AsNoTracking().ToListAsync();

                var clientCodes = string.IsNullOrEmpty(request.ClientCode)
                    ? clients.Select(c => c.ClientCode).ToList()
                    : priceBoost.Select(c => c.ClientCode).Distinct().ToList();

                var markets = PriceBoostListBuilder.BuildMarkets(mongo.Markets, mongo.Selections, clientCodes, GetPlayerPropMarketTemplateIds());

                var result = PriceBoostListBuilder.Build(priceBoost, request, clients, mongo.Fixtures, mongo.Selections, markets, out var endedProviderFixtureIds);

                await DeleteEndedPriceBoost(endedProviderFixtureIds);

                return new BaseResponse<List<PriceBoostConfigModel>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPriceBoostConfig failed for client {ClientCode}", request?.ClientCode);

                return new BaseResponse<List<PriceBoostConfigModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        // Every client of the request with the cap saved for it (0 when none), optionally filtered by name.
        public async Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request, string region)
        {
            try
            {
                var clientCodes = request?.ClientCodes ?? new List<string>();

                var settings = await _tradingContext.BoostedSettings.AsNoTracking()
                    .Where(c => clientCodes.Contains(c.ClientCode))
                    .ToListAsync();

                var clients = await _tradingContext.Client.AsNoTracking()
                    .Where(c => clientCodes.Contains(c.ClientCode))
                    .ToListAsync();

                var result = clients.Select(c => new BoostedSettingsModel
                {
                    ClientCode = c.ClientCode,
                    ClientName = c.ClientName,
                    ClientId = c.ClientId,
                    Cap = settings.Where(s => s.ClientId == c.ClientId).Select(s => s.Cap).FirstOrDefault()
                }).ToList();

                if (!string.IsNullOrWhiteSpace(request?.Search))
                    result = result.Where(c => (c.ClientName ?? string.Empty).Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();

                return new BaseResponse<List<BoostedSettingsModel>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBoostedSettings failed");

                return new BaseResponse<List<BoostedSettingsModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        // Saves the % cap of one client (as a fraction, 0.05 is 5%, cut to 4 decimals), then tells the user who
        // saved it with a notification. The activity log is written by the Web.
        public async Task<BaseResponse<bool>> AddOrUpdateBoostedSettings(BoostedSettingsModel request, string userId, string region)
        {
            try
            {
                var cap = Math.Round(request.Cap, 4, MidpointRounding.ToZero);

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var setting = await _tradingContext.BoostedSettings.FirstOrDefaultAsync(c => c.ClientId == request.ClientId);

                    if (setting == null)
                    {
                        _tradingContext.BoostedSettings.Add(new BoostedSettings
                        {
                            ClientId = request.ClientId,
                            ClientCode = request.ClientCode,
                            ClientName = request.ClientName,
                            Cap = cap
                        });
                    }
                    else
                    {
                        setting.ClientCode = request.ClientCode;
                        setting.ClientName = request.ClientName;
                        setting.Cap = cap;
                    }

                    await _tradingContext.SaveChangesAsync();

                    scope.Complete();
                }

                await PublishBoostedSettingsNotification(request, cap, userId);

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddOrUpdateBoostedSettings failed for client {ClientCode}", request?.ClientCode);

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = false
                };
            }
        }

        // The toast Backoffice.WebSocket.Service pushes to the user who saved the cap. A publish failure must not
        // undo the save, so it is only logged. Without a valid user id nothing is sent: a notification without a
        // user would reach everyone.
        private async Task PublishBoostedSettingsNotification(BoostedSettingsModel request, decimal cap, string userId)
        {
            try
            {
                if (!Guid.TryParse(userId, out var notifyUserId))
                    return;

                var notification = new NotificationsDetailsModel
                {
                    Id = Guid.NewGuid(),
                    Page = "Boosted Settings",
                    ClientCode = request.ClientCode,
                    SportsId = null,
                    Details = $"<b>New Boosted Settings</b> for <b>{request.ClientName}</b> - {(cap * 100).ToString("N2", CultureInfo.InvariantCulture)}%.",
                    PageRedirect = "/hk/odds-engine/boosted-settings",
                    UserId = notifyUserId,
                    ClaimType = SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace('_', '.'),
                    Type = (int)NotificationType.BoostedSettings
                };

                await _rabbitMqRepository.Publish(new MessageRequest
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    MessageType = "OnNewNotification",
                    RoutingKey = "r.oe.bm.engine.notifications",
                    Payload = JsonSerializer.Serialize(notification)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Boosted settings notification was not published for client {ClientCode}", request.ClientCode);
            }
        }

        // The fixtures of the saved boosts that are not over yet, with the markets and selections of the boosted
        // market templates.
        private async Task<(List<Fixture> Fixtures, List<MarketFilter> Markets, List<SelectionFilter> Selections)> GetPriceBoostMongoData(List<string> providerFixtureIds, List<string> marketTemplateIds)
        {
            var fixtures = await fixtureCollection
                .Find(Builders<Fixture>.Filter.Where(c => providerFixtureIds.Contains(c.PrFiId) && c.FiStId <= (int)FixtureStatus.Live))
                .ToListAsync();

            var fixtureIds = fixtures.Select(s => s.FiId).Distinct().ToList();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.IsBo);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.MaTeId).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.SeSp).Include(c => c.SeNaTr).Include(c => c.ClBoSeNa).Include(c => c.ClBoSeNaTr).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            return (fixtures, markets.Result, selections.Result);
        }

        // Deletes the boosts of fixtures that have ended.
        private async Task DeleteEndedPriceBoost(List<string> providerFixtureIds)
        {
            if (providerFixtureIds == null || !providerFixtureIds.Any())
                return;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                var ended = await _tradingContext.PriceBoostConfig
                    .Where(c => providerFixtureIds.Contains(c.ProviderFixtureId))
                    .ToListAsync();

                if (ended.Any())
                {
                    _tradingContext.PriceBoostConfig.RemoveRange(ended);
                    await _tradingContext.SaveChangesAsync();
                }

                scope.Complete();
            }
        }

        // The market templates whose odds are player props (priced from the client margin alone).
        private HashSet<string> GetPlayerPropMarketTemplateIds()
        {
            var rows = _memCache.GetCache("mtpp", () => _sportsbookContext.MarketTemplatePlayerProps.AsNoTracking().ToList(), 24);

            if (rows == null)
                return new HashSet<string>();

            return rows.SelectMany(r => (r.MarketTemplates ?? string.Empty).Split(',')).ToHashSet();
        }

        private static int? ParseIdFilter(string value) => string.IsNullOrEmpty(value) ? null : Convert.ToInt32(value);

        private static PriceBoostConfigModel ToPriceBoostConfigModel(PriceBoostConfig s) => new PriceBoostConfigModel
        {
            ClientCode = s.ClientCode,
            SportId = s.SportId,
            SegmentId = s.SegmentId,
            LeagueId = s.LeagueId,
            FixtureTypeId = s.FixtureTypeId,
            ProviderFixtureId = s.ProviderFixtureId,
            MarketTemplateId = s.MarketTemplateId,
            MarketSpecifier = s.MarketSpecifier,
            SelectionTemplateId = s.SelectionTemplateId,
            SelectionSpecifier = s.SelectionSpecifier,
            BoostedOdds = s.BoostedOdds,
            BoostedOddsStr = s.BoostedOdds.ToString("0.00", CultureInfo.InvariantCulture),
            UserId = s.UserId,
            UserName = s.UserName,
            TimeStamp = s.TimeStamp,
            BoostedSelectionName = s.BoostedSelectionName,
            BoostedSelectionTranslations = string.IsNullOrEmpty(s.BoostedSelectionTranslation)
                ? new List<BoostedSelectionTranslation>()
                : JsonSerializer.Deserialize<List<BoostedSelectionTranslation>>(s.BoostedSelectionTranslation)
        };
        #endregion

        #region BET
        public async Task<BaseResponse<BetSelectionDetailsResponse>> GetBetSelections(BetSelectionDetailsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"bet/selections", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<BetSelectionDetailsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<BetSelectionDetailsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<ManualBetDetailsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualBetDetailsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<FastOddsSportsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FastOddsSportsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<FastOddsFixturesResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FastOddsFixturesResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                if (request == null || request.Count == 0)
                {
                    _logger.LogWarning("AddOrUpdateManualMarketSettlement: Request body is null or empty. UserId: {UserId}", userId);
                    return new BaseResponse<ManualMarketSettlementResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ManualMarketSettlementResponse()
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Request cannot be empty",
                        }
                    };
                }

                var timeStamp = DateTime.UtcNow;
                var activityLogs = new OnActivityLogListMessage();
                var statuses = new List<BetStatus> { BetStatus.Won, BetStatus.Lost, BetStatus.HalfLost, BetStatus.HalfWon, BetStatus.Void };

                // validate duplicate bets
                var duplicateBetResponse = request.GroupBy(m => new { m.FixtureId, m.MarketId, m.SelectionId }).Where(m => m.Count() > 1)
                    .Select(m => new
                    {
                        FixtureId = m.Key.FixtureId,
                        MarketId = m.Key.MarketId,
                        SelectionId = m.Key.SelectionId,
                    }).ToList();

                if (duplicateBetResponse.Count > 0)
                {
                    return new BaseResponse<ManualMarketSettlementResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ManualMarketSettlementResponse()
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Duplicate market selection found",
                        }
                    };
                }

                foreach (var item in request)
                {
                    if (string.IsNullOrEmpty(item.FixtureId) || string.IsNullOrEmpty(item.MarketId) || string.IsNullOrEmpty(item.SelectionId))
                        return new BaseResponse<ManualMarketSettlementResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ManualMarketSettlementResponse()
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! All fields cannot be empty. Please fill in accordingly or remove the empty row",
                            }
                        };

                    if (!statuses.Contains(item.SelectionStatus))
                        return new BaseResponse<ManualMarketSettlementResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ManualMarketSettlementResponse()
                            {
                                StatusCode = StatusCode.Error,
                                Message = "New Status not accepted",
                            }
                        };

                    if (string.IsNullOrEmpty(item.Reason))
                        return new BaseResponse<ManualMarketSettlementResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ManualMarketSettlementResponse()
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Reason is required",
                            }
                        };

                    var manualSettlementRequestAudit = new ManualMarketSettlementAudit()
                    {
                        SportId = item.SportId,
                        SportName = item.SportName,
                        SegmentId = item.SegmentId,
                        SegmentName = item.SegmentName,
                        LeagueId = item.LeagueId,
                        LeagueName = item.LeagueName,
                        FixtureId = Convert.ToInt64(item.FixtureId),
                        FixtureName = item.FixtureName,
                        FixtureTypeId = item.FixtureTypeId,
                        FixtureTypeName = item.FixtureTypeName,
                        MarketTemplateId = item.MarketTemplateId,
                        MarketId = Convert.ToInt64(item.MarketId),
                        MarketName = item.MarketName,
                        SelectionTemplateId = item.SelectionTemplateId,
                        SelectionId = Convert.ToInt64(item.SelectionId),
                        SelectionName = item.SelectionName,
                        FixtureStatusId = item.FixtureStatusId,
                        FixtureStatusName = item.FixtureStatusName,
                        Line = item.Line,
                        Odds = item.Odds,
                        ProviderFixtureId = item.ProviderFixtureId,
                        FixtureSpecifier = item.FixtureSpecifier,
                        MarketSpecifier = item.MarketSpecifier,
                        SelectionSpecifier = item.SelectionSpecifier,
                        FixtureStartDate = item.FixtureStartDate,
                        SettlementScore = item.SettlementScore,
                        Probability = item.Probability,
                        LostProbability = item.LostProbability,
                        WonProbability = item.WonProbability,
                        HalfWonProbability = item.HalfWonProbability,
                        HalfLostProbability = item.HalfLostProbability,
                        VoidProbability = item.VoidProbability,
                        CorrelationId = item.CorrelationId,
                        DeadHeatFactor = item.DeadHeatFactor,
                        ManualSettledBy = item.ManualSettledBy,
                        TimeStamp = timeStamp,
                        Reason = item.Reason,
                        ToStatusId = (int)item.SelectionStatus,
                        ToStatusName = item.SelectionStatus.ToString()
                    };

                    activityLogs.OnActivityLogMessages.Add(new OnActivityLogMessage()
                    {
                        Page = AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace("_", "."),
                        // Payload (nvarchar(max)) holds the full settlement detail the history grid renders.
                        // From/To carry the main change values; To is the resulting selection status.
                        Payload = JsonSerializer.Serialize(manualSettlementRequestAudit),
                        Activity = Activity.ManualSettlement.ToString(),
                        From = "-",
                        To = manualSettlementRequestAudit.ToStatusName,
                        UserId = userId
                    });
                }

                await _activityLogRepository.ProcessActivityLog(activityLogs);

                foreach (var fixtureGroup in request.GroupBy(r => r.ProviderFixtureId))
                {
                    var first = fixtureGroup.First();
                    var settlementMessage = new OnBetSettlementMessage
                    {
                        SportId = first.SportId,
                        ProviderFixtureId = first.ProviderFixtureId,
                        FixtureSpecifier = first.FixtureSpecifier,
                        HomeScore = string.Empty,
                        AwayScore = string.Empty,
                        Selections = fixtureGroup.Select(r => new SelectionSettlementMessage
                        {
                            MarketTemplateId = r.MarketTemplateId,
                            SelectionTemplateId = r.SelectionTemplateId,
                            MarketSpecifier = r.MarketSpecifier,
                            // SelectionSpecifier is required: OE.Sportsbook.Service matches the Mongo
                            // Selection on (PrFiId, MaTeId, SeTeId, SeSp). Multiple lines of the same
                            // market (e.g. Under 2.50/3.50/4.50) share MaTeId+SeTeId and differ ONLY by
                            // SeSp, so omitting it made the filter match nothing and the settlement was
                            // silently dropped (bet never settled). [GM-543]
                            SelectionSpecifier = r.SelectionSpecifier,
                            Result = r.SelectionStatus == BetStatus.Won || r.SelectionStatus == BetStatus.HalfWon,
                            VoidFactor = GetVoidFactor(r.SelectionStatus),
                            DeadHeatFactor = r.DeadHeatFactor,
                        }).ToList()
                    };

                    await _rabbitMqRepository.Publish(new MessageRequest()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "OnBetSettlement",
                        RoutingKey = "r.uof.onbetsettlement",
                        Payload = JsonSerializer.Serialize(settlementMessage)
                    });
                }

                return new BaseResponse<ManualMarketSettlementResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ManualMarketSettlementResponse()
                    {
                        StatusCode = StatusCode.Success,
                        Message = "Success! Markets has been successfully settled."
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddOrUpdateManualMarketSettlement: {Message} | UserId: {UserId} | InnerException: {InnerMessage} | StackTrace: {StackTrace} | Payload: {Payload}", ex.Message, userId, ex.InnerException?.Message, ex.StackTrace, JsonSerializer.Serialize(request));

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
                var fixtureId = long.Parse(request.FixtureId);
                var marketId = long.Parse(request.MarketId);
                var selectionId = long.Parse(request.SelectionId);

                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiId == fixtureId);
                var fixture = fixtureCollection.Find(fixtureFilter).FirstOrDefault();

                if (fixture == null)
                {
                    return new BaseResponse<ManualMarketSettlementDetailsResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "No fixture found",
                    };
                }

                var marketFilter = Builders<Market>.Filter.Where(c => c.MaId == marketId && c.FiId == fixtureId);
                var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == LanguageCode.EN.ToString());
                var market = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).FirstOrDefault();

                var selectionFilter = Builders<Selection>.Filter.Where(c => c.SeId == selectionId && c.MaId == marketId);
                var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.SeTeId).Include(c => c.SeSp).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.Pr).Include(c => c.WoPr).Include(c => c.LoPr).Include(c => c.VoPr).Include(c => c.HaWoPr).Include(c => c.HaLoPr).Include(c => c.DeHeFa).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == LanguageCode.EN.ToString());
                var selection = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).FirstOrDefault();

                if (market == null || selection == null)
                {
                    return new BaseResponse<ManualMarketSettlementDetailsResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Market selection not found",
                    };
                }

                return new BaseResponse<ManualMarketSettlementDetailsResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ManualMarketSettlementDetailsResponse()
                    {
                        SportId = fixture.SpId,
                        SportName = fixture.SpNa,
                        SegmentId = fixture.SegId,
                        SegmentName = fixture.SegNa,
                        LeagueId = fixture.LeId,
                        LeagueName = fixture.LeNa,
                        FixtureId = fixture.FiId,
                        FixtureName = fixture.FiNa,
                        FixtureTypeId = fixture.FiTyId,
                        FixtureTypeName = fixture.FiTyNa,
                        MarketTemplateId = market.MaTeId,
                        MarketId = market.MaId,
                        MarketName = market.MaNa,
                        SelectionTemplateId = selection.SeTeId,
                        SelectionId = selection.SeId,
                        SelectionName = selection.SeNa,
                        FixtureStatusId = fixture.FiStId,
                        FixtureStatusName = fixture.FiStNa,
                        Line = market.Li,
                        Odds = selection.Od,
                        ProviderFixtureId = fixture.PrFiId,
                        FixtureSpecifier = fixture.FiSp,
                        MarketSpecifier = string.Empty,
                        SelectionSpecifier = selection.SeSp,
                        FixtureStartDate = fixture.FiStDa,
                        SettlementScore = fixture.Sc,
                        Probability = selection.Pr,
                        LostProbability = selection.LoPr,
                        WonProbability = selection.WoPr,
                        HalfWonProbability = selection.HaWoPr,
                        HalfLostProbability = selection.HaLoPr,
                        VoidProbability = selection.VoPr,
                        CorrelationId = selection.CoId,
                        DeadHeatFactor = selection.DeHeFa,
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetManualMarketSettlementDetails: {ex.Message}-{ex.InnerException?.Message}");

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

                    return JsonSerializer.Deserialize<BaseResponse<string>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<string>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<ManualMessageSettlementAuditResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualMessageSettlementAuditResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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

        // BO-42: published through the broker API of this service, no longer forwarded to the Trading API.
        public async Task<BaseResponse<MessageResponse>> Publish(MessageRequest request, string region)
        {
            return await _rabbitMqRepository.Publish(request, region);
        }

        public async Task<BaseResponse<BookmakerConfigurationResponse>> GetBookmakers(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"bookmakers");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<BookmakerConfigurationResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<BookmakerConfigurationResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<Tuple<List<NotificationResponse>, MetaData>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<Tuple<List<NotificationResponse>, MetaData>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketLookupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SportMappingItemModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportMappingItemModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SegmentMappingItemModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SegmentMappingItemModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<LeagueMappingItemModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<LeagueMappingItemModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<FixtureMappingModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FixtureMappingModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketMappingModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketMappingModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SelectionMappingModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SelectionMappingModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SportMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SegmentMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SegmentMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<LeagueMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<LeagueMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<FixtureMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<FixtureMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<MarketMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<SelectionMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SelectionMappingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    result = JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        result = JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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

            if (string.IsNullOrEmpty(request.UserId) || !Guid.TryParse(request.UserId, out _))
                return $"Invalid User ID";

            if (!string.IsNullOrEmpty(request.Mapping.Id) && !int.TryParse(request.Mapping.Id, out _))
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

            if (string.IsNullOrEmpty(request.UserId) || !Guid.TryParse(request.UserId, out _))
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
                        if (!int.TryParse(mapping.Id, out _))
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

        private static VoidFactor GetVoidFactor(BetStatus status)
        {
            if (status == BetStatus.Void) return VoidFactor.One;
            if (status == BetStatus.HalfWon || status == BetStatus.HalfLost) return VoidFactor.Half;
            return VoidFactor.Zero;
        }
        #endregion
    }
}