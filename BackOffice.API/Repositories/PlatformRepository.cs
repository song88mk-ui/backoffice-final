using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;
using static Backoffice.Shared.Models.Response.ClientParlayBonusResponse;
using static Backoffice.Shared.Models.Response.ClientTopFixtureResponse;
using static Backoffice.Shared.Models.Response.GetTopLeaguesResponse;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backoffice.API.Repositories
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;
        public PlatformRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #region TOP LEAGUES
        public async Task<BaseResponse<TopLeaguesResponse>> GetClientAllLeagues(ClientAllLeaguesRequest request, string region)
        {
            try
            {
                var queryStrings = await FormUrlEncoded(request);
                var response = await GetHttpClient(region).GetAsync($"v1/client/allleagues{queryStrings}");

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<TopLeaguesResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<TopLeaguesResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<TopLeaguesResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<TopLeaguesResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<GetTopLeaguesResponse>> GetTopLeagues(GetTopLeaguesRequest request, IDictionary<string, string> regionclientIds)
        {
            try
            {
                var topLeagues = new List<OrderedClientTopLeagues>();
                foreach (var region in regionclientIds)
                {
                    var queryStrings = await FormUrlEncoded(new GetTopLeaguesRequest
                    {
                        ClientIdRefs = region.Value,
                        LanguageCode = request.LanguageCode,
                        LeagueId = request.LeagueId,
                        SegmentId = request.SegmentId,
                        SportId = request.SportId
                    });

                    var result = await GetHttpClient(region.Key).GetAsync($"v1/client/topleagues{queryStrings}");

                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        var newresult = JsonConvert.DeserializeObject<BaseResponse<GetTopLeaguesResponse>>(await result.Content.ReadAsStringAsync()).Result.ClientTopLeagues;
                        if (newresult.Any())
                        {
                            topLeagues.AddRange(newresult);
                        }
                    }
                }

                if (topLeagues.Any())
                {
                    return new BaseResponse<GetTopLeaguesResponse>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new GetTopLeaguesResponse()
                        {
                            ClientTopLeagues = topLeagues
                        }
                    };
                }
                else
                {
                    return new BaseResponse<GetTopLeaguesResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetTopLeaguesResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<PlatformMessageResponse>> PostTopLeagues(PostTopLeaguesRequest request, string region)
        {
            try
            {
                var topLeagues = new List<OrderedClientTopLeague>();
                var sortOrder = 1;

                foreach (var league in request.TopLeagues)
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
                    saClientIds.AddRange(regionclientIds["HK"].Split(",").Select(int.Parse).ToList());

                if (hkClientIds.Any())
                {
                    var topLeagues = new List<ClientTopSetup>();
                    var sortOrder = 1;

                    foreach (var league in request.TopLeagues)
                    {
                        topLeagues.Add(new ClientTopSetup()
                        {
                            Id = league.Id,
                            SortOrder = sortOrder
                        });

                        sortOrder++;
                    }

                    response = await CopyTopLeagesPerRegion(new CopyTopLeaguesRequest
                    {
                        ClientIds = hkClientIds,
                        SportId = request.SportId,
                        TopLeagues = topLeagues
                    }, "HK");
                }

                // only proceed to SA if result from HK is null or HK has a result and the status code is success
                if (saClientIds.Any() && (response.Result == null || (response.Result != null && response.Result.StatusCode == StatusCode.Success)))
                {
                    var topLeagues = new List<ClientTopSetup>();
                    var sortOrder = 1;

                    foreach (var league in request.TopLeagues)
                    {
                        topLeagues.Add(new ClientTopSetup()
                        {
                            Id = league.Id,
                            SortOrder = sortOrder
                        });

                        sortOrder++;
                    }

                    response = await CopyTopLeagesPerRegion(new CopyTopLeaguesRequest
                    {
                        ClientIds = saClientIds,
                        SportId = request.SportId,
                        TopLeagues = topLeagues
                    }, "SA");
                }

                return new BaseResponse<bool>()
                {
                    StatusCode = response.StatusCode,
                    Message = response.Message,
                    Result = true
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
            var result = await GetHttpClient(region).PostAsync($"v1/client/topleauges/copy", new StringContent(JsonConvert.SerializeObject(new
            {
                ClientIds = request.ClientIds,
                TopLeague = request.TopLeagues
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
            try
            {
                var topFixture = new List<OrderedClientTopFixture>();
                foreach (var region in regionclientIds)
                {
                    var response = await GetHttpClient(region.Key).GetAsync($"v1/client/topfixtures?clientRefIds={region.Value}");
                    var payload = JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureResponse>>(response.Content.ReadAsStringAsync().Result);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var newresult = JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureResponse>>(response.Content.ReadAsStringAsync().Result).Result.TopFixtures;
                        if (newresult.Any())
                        {
                            topFixture.AddRange(newresult);
                        }
                    }
                    else
                    {
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                            return JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureResponse>>(await response.Content.ReadAsStringAsync());
                        else
                        {
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
            var topFixtures = new List<ClientTopSetup>();
            var sortOrder = 1;

            foreach (var fixture in request.TopFixtures)
            {
                topFixtures.Add(new ClientTopSetup()
                {
                    Id = fixture.Id,
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
                        if (newresult.ParlayBonusConfigs.Any())
                        {
                            bonus.AddRange(newresult.ParlayBonusConfigs);
                            minOdds = newresult.ParlayBonusMinOdds;
                        }
                    }
                }

                if (bonus.Any())
                {
                    return new BaseResponse<ClientParlayBonusResponse>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ClientParlayBonusResponse
                        {
                            ClientIdRef = request.ClientIdRef,
                            ParlayBonusConfigs = bonus,
                            ParlayBonusMinOdds = minOdds
                        }
                    };
                }
                else
                {
                    return new BaseResponse<ClientParlayBonusResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}"
                    };
                }
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
                return new BaseResponse<StatusCode>
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
                var response = await GetHttpClient(region).GetAsync($"v1/client/topfixtures/sportstree?clientRefId={clientId}");

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync());
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
                return new BaseResponse<SportsTreeResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

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
