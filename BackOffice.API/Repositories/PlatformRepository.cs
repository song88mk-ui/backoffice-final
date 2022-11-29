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
using System.Text.Json;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Requests.TopLeaguesRequest;
using static Backoffice.Shared.Models.Response.ClientParlayBonusResponse;
using static Backoffice.Shared.Models.Response.ClientTopFixtureResponse;
using static Backoffice.Shared.Models.Response.ClientTopLeaguesResponse;
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

        #region Methods
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
                return _configuration[$"API:PlatformApi:SA:ApiKey"];
            else
                return _configuration[$"API:PlatformApi:HK:ApiKey"];
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

        public async Task<BaseResponse<ClientTopFixtureMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/client/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureMessageResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureMessageResponse>>(await response.Content.ReadAsStringAsync());
                    else
                    {
                        return new BaseResponse<ClientTopFixtureMessageResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientTopFixtureMessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds)
        {
            try
            {
                var topFixture = new List<OrderedClientTopFixture>();
                foreach (var region in regionclientIds)
                {
                    var response = await GetHttpClient(region.Key).GetAsync($"v1/client/topfixtures?clientRefIds={region.Value}");
                    var payload = JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureResponse>>(response.Content.ReadAsStringAsync().Result);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
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

        public async Task<BaseResponse<ClientTopLeaguesResponse>> GetClientTopLeagues(TopLeaguesRequest request, IDictionary<string, string> regionclientIds)
        {
            try
            {
                var topLeagues = new List<OrderedClientTopLeagues>();
                foreach (var region in regionclientIds)
                {
                    var queryStrings = await FormUrlEncoded(new TopLeaguesRequest
                    {
                        ClientIdRefs = region.Value,
                        LanguageCode = request.LanguageCode,
                        LeagueId = request.LeagueId,
                        SegmentId = request.SegmentId,
                        SportId = request.SportId
                    });
                    var requestUri = $"v1/client/topleagues{queryStrings}";

                    var result = GetHttpClient(region.Key).GetAsync(requestUri).Result;

                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var newresult = JsonConvert.DeserializeObject<BaseResponse<ClientTopLeaguesResponse>>(result.Content.ReadAsStringAsync().Result).Result.ClientTopLeagues;
                        if (newresult.Any())
                        {
                            topLeagues.AddRange(newresult);
                        }
                    }
                }

                if (topLeagues.Any())
                {
                    return new BaseResponse<ClientTopLeaguesResponse>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ClientTopLeaguesResponse()
                        {
                            ClientTopLeagues = topLeagues
                        }
                    };
                }
                else
                {
                    return new BaseResponse<ClientTopLeaguesResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}"
                    };
                }

            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientTopLeaguesResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientTopLeagueMessageResponse>> AddOrUpdateTopLeagues(ClientTopLeaguesRequest request, string region)
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
                return JsonConvert.DeserializeObject<BaseResponse<ClientTopLeagueMessageResponse>>(await response.Content.ReadAsStringAsync());
            else
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                    return JsonConvert.DeserializeObject<BaseResponse<ClientTopLeagueMessageResponse>>(await response.Content.ReadAsStringAsync());
                else
                {
                    return new BaseResponse<ClientTopLeagueMessageResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                    };
                }
            }
        }

        public async Task<BaseResponse<CopyClientTopLeagueMessageResponse>> CopyClientTopLeagues(CopyClientTopLeaguesRequest request)
        {
            throw new NotImplementedException();
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

        #region Top Fixtures
        public async Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request, string region)
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

            var requestUri = $"v1/client/topfixtures/copy";

            var result = GetHttpClient(region).PostAsync(requestUri, new StringContent(JsonConvert.SerializeObject(new
            {
                ClientIds = request.ClientIds,
                TopFixtures = topFixtures
            }), Encoding.UTF8, "application/json")).Result;
            var payload = JsonConvert.DeserializeObject<BaseResponse<bool>>(result.Content.ReadAsStringAsync().Result);

            return new BaseResponse<bool>()
            { 
                StatusCode = payload.StatusCode,
                Message = payload.Message,
                Result = payload.Result
            };
        }
        #endregion
    }
}
