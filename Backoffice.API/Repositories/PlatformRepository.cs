using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Extensions;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;
using static Backoffice.Shared.Models.Response.ClientTopFixtureResponse;
using static Backoffice.Shared.Models.Response.GetTopLeaguesResponse;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backoffice.API.Repositories
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IBackofficeActivityLogRepository _activityLogRepository;
        private readonly IRabbitMqRepository _rabbitMqRepository;
        private readonly ActivitySource _activitySource;

        public PlatformRepository(IConfiguration configuration, IBackofficeActivityLogRepository activityLogRepository, IRabbitMqRepository rabbitMqRepository, Instrumentation instrumentation)
        {
            _configuration = configuration;
            _activityLogRepository = activityLogRepository;
            _rabbitMqRepository = rabbitMqRepository;
            _activitySource = instrumentation.ActivitySource;
        }

        #region Client
        public async Task<BaseResponse<List<PlatformClientResponse>>> GetClients(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"v1/clients");

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<List<PlatformClientResponse>>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<List<PlatformClientResponse>>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<List<PlatformClientResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }

        }

        public async Task<BaseResponse<PlatformClientResponse>> AddOrUpdateClient(ClientRequest request, string region, List<Event> events)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/client", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<BaseResponse<PlatformClientResponse>>(await response.Content.ReadAsStringAsync());

                    var timeStampPublishStart = DateTime.UtcNow;

                    await _rabbitMqRepository.Publish(new MessageRequest
                    {
                        Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                        {
                            CacheType = CacheType.Client,
                            ClientId = result.Result.ClientId,
                            ClientCode = request.ClientCode,
                            CacheKey = $"cc:{result.Result.ClientId.ToString().ToUpper()}"
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnClientChange",
                        RoutingKey = $"r.oe.cachesync.onclientchange"
                    }, region);

                    events.Add(new Event
                    {
                        Action = "PUBLISH OnClientChange",
                        TimestampStarted = timeStampPublishStart,
                        TimestampFinished = DateTime.UtcNow,
                        ProcessingTime = DateTime.UtcNow.ToEpochTimestamp() - timeStampPublishStart.ToEpochTimestamp()
                    });

                    return result;
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<PlatformClientResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<PlatformClientResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<PlatformClientResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }

        }

        public async Task<BaseResponse<StatusCode>> ProcessBackofficeClientBetConfig(AddOrUpdateBetConfigRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/config/client", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<StatusCode>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<StatusCode>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<StatusCode>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<StatusCode>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }

        }

        public async Task<BaseResponse<ExchangeRatesResponse>> GetExchangeRates(string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"v1/client/exchangerates");

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<ExchangeRatesResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<ExchangeRatesResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<ExchangeRatesResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ExchangeRatesResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }

        }
        #endregion

        #region TOP LEAGUES
        public async Task<BaseResponse<GetAllLeaguesResponse>> GetAllLeagues(GetAllLeaguesRequest request, string region)
        {
            try
            {
                var queryStrings = await FormUrlEncoded(request);
                var response = await GetHttpClient(region).GetAsync($"v1/client/allleagues{queryStrings}");

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<GetAllLeaguesResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<GetAllLeaguesResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<GetAllLeaguesResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetAllLeaguesResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<GetTopLeaguesResponse>> GetTopLeagues(GetTopLeaguesRequest request, IDictionary<string, string> regionclientIds)
        {
            using (var activity = _activitySource.StartActivity("top.leagues"))
            {
                try
                {
                    var topLeagues = new List<OrderedClientTopLeagues>();
                    foreach (var region in regionclientIds)
                    {
                        var payload = JsonConvert.SerializeObject(new
                        {
                            ClientIdRefs = region.Value,
                            LanguageCode = request.LanguageCode,
                            LeagueId = request.LeagueId,
                            SegmentId = request.SegmentId,
                            SportId = request.SportId
                        });

                        var response = await GetHttpClient(region.Key).PostAsync($"v1/client/topleagues/list", new StringContent(
                            payload, Encoding.UTF8, "application/json"));

                        activity?.SetTag("client.list.status.code", response.StatusCode);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var newresult = JsonConvert.DeserializeObject<BaseResponse<GetTopLeaguesResponse>>(response.Content.ReadAsStringAsync().Result).Result.ClientTopLeagues;
                            if (newresult.Any())
                            {
                                topLeagues.AddRange(newresult);
                            }
                        }
                        else
                        {
                            if (response.StatusCode == HttpStatusCode.BadRequest)
                                return JsonConvert.DeserializeObject<BaseResponse<GetTopLeaguesResponse>>(await response.Content.ReadAsStringAsync());
                            else
                            {
                                return new BaseResponse<GetTopLeaguesResponse>
                                {
                                    StatusCode = StatusCode.Error,
                                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                                };
                            }
                        }

                    }
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return new BaseResponse<GetTopLeaguesResponse>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new GetTopLeaguesResponse
                        {
                            ClientTopLeagues = topLeagues
                        }
                    };
                }
                catch (Exception ex)
                {
                    activity?.SetTag("error.stack.trace", ex.StackTrace);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    return new BaseResponse<GetTopLeaguesResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                    };
                }
            }
        }
        public async Task<BaseResponse<PlatformMessageResponse>> PostTopLeagues(PostTopLeaguesRequest request, string region)
        {
            try
            {
                var topLeagues = new List<OrderedClientTopLeague>();
                var sortOrder = 1;

                foreach (var league in request.TopLeagues.OrderBy(l => l.SortOrder))
                {
                    topLeagues.Add(new OrderedClientTopLeague()
                    {
                        LeagueId = league.LeagueId,
                        LeagueName = league.LeagueName,
                        SegmentId = league.SegmentId,
                        SegmentName = league.SegmentName,
                        SportId = league.SportId,
                        SportName = league.SportName,
                        SortOrder = sortOrder
                    });

                    sortOrder++;
                }

                var payload = new
                {
                    ClientIdRef = request.ClientIdRef,
                    SportId = request.SportId,
                    TopLeagues = topLeagues
                };

                var response = await GetHttpClient(region).PostAsync($"v1/client/topleagues", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<PlatformMessageResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<PlatformMessageResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }

        }
        public async Task<BaseResponse<bool>> CopyTopLeagues(CopyTopLeaguesRequest request, IDictionary<string, string> regionclientIds)
        {
            try
            {
                var response = new BaseResponse<PlatformMessageResponse>();

                var hkClientIds = new List<int>();
                var saClientIds = new List<int>();

                if (regionclientIds.ContainsKey("HK"))
                    hkClientIds.AddRange(regionclientIds["HK"].Split(",").Select(int.Parse).ToList());
                else if (regionclientIds.ContainsKey("SA"))
                    saClientIds.AddRange(regionclientIds["SA"].Split(",").Select(int.Parse).ToList());

                if (hkClientIds.Any())
                {
                    response = await CopyTopLeagesPerRegion(new CopyTopLeaguesRequest
                    {
                        ClientIdRefs = hkClientIds,
                        SportId = request.SportId,
                        TopLeagues = request.TopLeagues
                    }, "HK");
                }

                if (saClientIds.Any())
                {
                    response = await CopyTopLeagesPerRegion(new CopyTopLeaguesRequest
                    {
                        ClientIdRefs = saClientIds,
                        SportId = request.SportId,
                        TopLeagues = request.TopLeagues
                    }, "SA");
                }

                return new BaseResponse<bool>()
                {
                    StatusCode = response.StatusCode,
                    Message = response.Message,
                    Result = response.StatusCode == StatusCode.Success ? true : false
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = false
                };
            }
        }
        private async Task<BaseResponse<PlatformMessageResponse>> CopyTopLeagesPerRegion(CopyTopLeaguesRequest request, string region)
        {
            var result = await GetHttpClient(region).PostAsync($"v1/client/topleagues/copy", new StringContent(JsonConvert.SerializeObject(new
            {
                ClientIdRefs = request.ClientIdRefs,
                SportId = request?.SportId,
                TopLeagues = request.TopLeagues
            }), Encoding.UTF8, "application/json"));

            var payload = JsonConvert.DeserializeObject<BaseResponse<PlatformMessageResponse>>(await result.Content.ReadAsStringAsync());

            return new BaseResponse<PlatformMessageResponse>()
            {
                StatusCode = payload.StatusCode,
                Message = payload.Message,
                Result = payload.Result
            };
        }
        public async Task<BaseResponse<SportsLookupResponse>> GetClientSports(string LanguageCode, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"v1/client/sports?LanguageCode={LanguageCode}");

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<SportsLookupResponse>>(await response.Content.ReadAsStringAsync());
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
                return new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region TOP FIXTURE
        public async Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds)
        {
            using var activity = _activitySource.StartActivity("top.fixture");

            try
            {
                var topFixture = new List<OrderedClientTopFixture>();
                foreach (var region in regionclientIds)
                {
                    var payload = JsonConvert.SerializeObject(new
                    {
                        clientRefIds = region.Value
                    });

                    var response = await GetHttpClient(region.Key).PostAsync($"v1/client/topfixtures/list", new StringContent(
                        payload, Encoding.UTF8, "application/json"));

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var responseString = response.Content.ReadAsStringAsync().Result;

                        activity?.SetTag("response", responseString);

                        var newresult = JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureResponse>>(responseString).Result.TopFixtures;
                        if (newresult.Any())
                        {
                            topFixture.AddRange(newresult);
                        }
                    }
                    else
                    {
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            var responseString = await response.Content.ReadAsStringAsync();

                            activity?.SetTag("response.bad.request", responseString);

                            return JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureResponse>>(responseString);
                        }
                        else
                        {
                            activity?.SetTag("response.error", response.ReasonPhrase);

                            return new BaseResponse<ClientTopFixtureResponse>
                            {
                                StatusCode = StatusCode.Error,
                                Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                            };
                        }
                    }
                }

                return new BaseResponse<ClientTopFixtureResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientTopFixtureResponse
                    {
                        TopFixtures = topFixture
                    }
                };
            }
            catch (Exception ex)
            {
                activity?.SetTag("response.ex.message", ex.Message);
                activity?.SetTag("response.ex.stacktrace", ex.StackTrace);

                return new BaseResponse<ClientTopFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<PlatformMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/client/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<PlatformMessageResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<PlatformMessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<PlatformMessageResponse>> CopyClientTopFixtures(CopyClientTopFixtureRequest request, string region)
        {
            var topFixtures = new List<ClientTopFixtureSetup>();
            var sortOrder = 1;

            foreach (var fixture in request.TopFixtures)
            {
                topFixtures.Add(new ClientTopFixtureSetup()
                {
                    FixtureId = fixture.FixtureId,
                    SortOrder = sortOrder
                });

                sortOrder++;
            }

            var result = GetHttpClient(region).PostAsync($"v1/client/topfixtures/copy", new StringContent(JsonConvert.SerializeObject(new
            {
                ClientIds = request.ClientIds,
                TopFixtures = topFixtures
            }), Encoding.UTF8, "application/json")).Result;
            var payload = JsonConvert.DeserializeObject<BaseResponse<PlatformMessageResponse>>(result.Content.ReadAsStringAsync().Result);

            return new BaseResponse<PlatformMessageResponse>()
            {
                StatusCode = payload.StatusCode,
                Message = payload.Message,
                Result = payload.Result
            };
        }
        #endregion

        #region PARLAY BONUS
        public async Task<BaseResponse<ClientParlayBonusResponse>> GetClientParlayBonus(GetParlayBonusRequest request, IDictionary<string, string> regionclientIds)
        {
            try
            {
                var bonus = new List<ParlayBonusConfig>();
                var minOdds = 0m;
                foreach (var region in regionclientIds)
                {
                    var queryStrings = await FormUrlEncoded(request);

                    var requestUri = $"v1/client/parlaybonus{queryStrings}";

                    var result = await GetHttpClient(region.Key).GetAsync(requestUri);

                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        var newresult = JsonConvert.DeserializeObject<BaseResponse<ClientParlayBonusResponse>>(result.Content.ReadAsStringAsync().Result).Result;
                        if (newresult.ParlayBonusConfig.Any())
                        {
                            bonus.AddRange(newresult.ParlayBonusConfig);
                            minOdds = newresult.ParlayBonusMinOdds;
                        }
                    }
                }

                //var skip = (request.Page - 1) * request.PageSize;

                return new BaseResponse<ClientParlayBonusResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientParlayBonusResponse
                    {
                        ClientIdRef = request.ClientIdRef,
                        //ParlayBonusConfig = bonus.Skip(skip).Take(request.PageSize).ToList(),
                        ParlayBonusConfig = bonus,
                        ParlayBonusMinOdds = minOdds,
                        Page = request.Page,
                        PageSize = request.PageSize,
                        Total = bonus.Count,
                        TotalPage = (request.PageSize > 0 ? (bonus.Count - 1) / request.PageSize : 0) + 1
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientParlayBonusResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/client/parlaybonus", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    await _rabbitMqRepository.Publish(new MessageRequest
                    {
                        Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                        {
                            CacheType = CacheType.ParlayBonus
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnParlayBonusChange",
                        RoutingKey = $"r.oe.cachesync.onparlaybonuschange"
                    }, region);

                    return JsonConvert.DeserializeObject<BaseResponse<StatusCode>>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<StatusCode>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<StatusCode>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<StatusCode>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Announcements
        public async Task<BaseResponse<AnnouncementResponse>> GetAnnouncements(AnnouncementRequest request)
        {
            try
            {
                var response = await GetHttpClient("HK").PostAsync($"v1/client/announcements", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<AnnouncementResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<AnnouncementResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<AnnouncementResponse>> AddOrUpdateAnnouncements(AddOrUpdateAnnouncementRequest request)
        {
            try
            {
                var response = await GetHttpClient("HK").PostAsync($"v1/client/announcements/post", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<AnnouncementResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<AnnouncementResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteAnnouncements(DeleteAnnouncementRequest request)
        {
            try
            {
                var response = await GetHttpClient("HK").PostAsync($"v1/client/announcements/delete", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<bool>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<bool>>(await response.Content.ReadAsStringAsync());
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
                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        public async Task<BaseResponse<SportsTreeResponse>> GetSportstreeDetails(int clientId, string region)
        {
            try
            {
                var url = $"v1/client/topfixtures/sportstree?clientRefId={clientId}";
                var response = await GetHttpClient(region).GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<SportsTreeResponse>>(content);
                else
                {
                    return JsonConvert.DeserializeObject<BaseResponse<SportsTreeResponse>>(content)
                        ?? new BaseResponse<SportsTreeResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"Error ({response.ReasonPhrase})"
                        };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportsTreeResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #region Manual Bet Settlement
        public async Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/bet/details", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<ManualBetDetailsResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<ManualBetDetailsResponse>>(await response.Content.ReadAsStringAsync());
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
                return new BaseResponse<ManualBetDetailsResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ManualBetSettlementResponse>> ManualSettlement(ManualBetSettlementRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/bet/manualsettlement", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var result = JsonConvert.DeserializeObject<BaseResponse<ManualBetSettlementResponse>>(await response.Content.ReadAsStringAsync());

                    if (result?.Result?.Data != null)
                    {
                        var successfulLogs = result.Result.Data
                            .Select((bet, index) => (bet, index))
                            .Where(x => x.bet.IsSuccess && x.index < request.ActivityLogs.OnActivityLogMessages.Count)
                            .Select(x => request.ActivityLogs.OnActivityLogMessages[x.index])
                            .ToList();

                        if (successfulLogs.Count > 0)
                            await _activityLogRepository.ProcessActivityLog(new OnActivityLogListMessage { OnActivityLogMessages = successfulLogs });
                    }

                    return result;
                }
                else
                {
                    return new BaseResponse<ManualBetSettlementResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ManualBetSettlementResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ResendSettlementBetDetailResponse>> GetResendSettlementBetDetails(ResendSettlementBetDetailRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/bet/resendsettlement/details", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<ResendSettlementBetDetailResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<ResendSettlementBetDetailResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<ResendSettlementBetDetailResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ResendSettlementBetDetailResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ResendSettlementResponse>> ResendSettlement(ResendSettlementRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/bet/resendsettlement", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<ResendSettlementResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ResendSettlementResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<BetModifyResponse>> CancelBet(BetModifyRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/bet/cancel", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<BetModifyResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<BetModifyResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<BetModifyResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<BetModifyResponse>()
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
            var client = new HttpClient()
            {
                BaseAddress = new Uri(GetBaseUrl(region))
            };
            client.DefaultRequestHeaders.Add("x-api-key", GetApikey(region));

            return client;
        }
        private string GetBaseUrl(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:PlatformApi:SA:Uri"];
            else
                return _configuration[$"API:PlatformApi:HK:Uri"];
        }
        private string GetApikey(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:PlatformApi:SA:APIKey"];
            else
                return _configuration[$"API:PlatformApi:HK:APIKey"];
        }
        private async Task<string> FormUrlEncoded<T>(T parameter)
        {
            var urlEncodedString = new StringBuilder();

            var parameters = new Dictionary<string, string>();

            var obj = parameter.GetType();

            foreach (var property in obj.GetProperties())
            {
                var data = property.GetValue(parameter, null);
                if (data != null)
                {
                    if (property.PropertyType == typeof(DateTimeOffset)
                        || property.PropertyType == typeof(DateTime)
                        || property.PropertyType == typeof(DateTime?))
                    {
                        var date = Convert.ToDateTime(data.ToString());
                        parameters.Add(property.Name, date.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                    else
                    {
                        parameters.Add(property.Name, data.ToString());
                    }
                }
            }

            if (!parameters.Any()) return urlEncodedString.ToString();

            urlEncodedString.Append("?");

            var content = new FormUrlEncodedContent(parameters);
            urlEncodedString.Append(await content.ReadAsStringAsync());

            content.Dispose();

            return urlEncodedString.ToString();
        }
        #endregion
    }
}
