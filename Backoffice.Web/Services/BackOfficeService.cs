using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class BackOfficeService : IBackOfficeService
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IAuthenticationService _authenticationService;
        #endregion

        #region Ctor
        public BackOfficeService(HttpClient httpClient, IConfiguration configuration, IAuthenticationService authenticationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _authenticationService = authenticationService;

            //var userToken = httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;

            _httpClient.BaseAddress = new Uri(_configuration["API:Backoffice:Uri"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["API:Backoffice:Security:APIKey"]);
        }
        #endregion

        #region Methods - Client

        public async Task<BaseResponse<ClientModel>> GetClients()
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<ClientModel>>
                (await _httpClient.GetStreamAsync($"sportsbook/client/list"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<ClientModel>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new ClientModel()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientListResponse>> GetClientList(GetClientListRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var Model = await _httpClient.PostAsync($"sportsbook/client/information/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientListResponse>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<ClientListResponse>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientListResponse>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<ClientListResponse>();
                    else
                    {
                        return new BaseResponse<ClientListResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Methods - Top Fixtures
        public async Task<BaseResponse<ClientTopFixtureModel>> GetClientTopFixtures(GetClientTopFixtureRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);
                var Model = await _httpClient.PostAsync($"sportsbook/settings/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<ClientTopFixtureModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<ClientTopFixtureModel>();
                    else
                    {
                        return new BaseResponse<ClientTopFixtureModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientTopFixtureModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<PlatformMessageModel>> PostClientTopFixtures(PostClientTopFixtureRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);
                var Model = await _httpClient.PostAsync($"sportsbook/settings/topfixtures/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PlatformMessageModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<PlatformMessageModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PlatformMessageModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<PlatformMessageModel>();
                    else
                    {
                        return new BaseResponse<PlatformMessageModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<PlatformMessageModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);
                var Model = await _httpClient.PostAsync($"sportsbook/settings/topfixtures/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<bool>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<bool>();
                    else
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
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
        #endregion

        #region Methods - Top Leagues

        public async Task<BaseResponse<GetAllLeaguesModel>> GetAllLeaguesAsync(GetAllLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var Model = await _httpClient.PostAsync($"sportsbook/settings/allleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<GetAllLeaguesModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<GetAllLeaguesModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<GetAllLeaguesModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<GetAllLeaguesModel>();
                    else
                    {
                        return new BaseResponse<GetAllLeaguesModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetAllLeaguesModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<TopLeaguesModel>> GetTopLeaguesAsync(GetTopLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var Model = await _httpClient.PostAsync($"sportsbook/settings/topleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<TopLeaguesModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<TopLeaguesModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<TopLeaguesModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<TopLeaguesModel>();
                    else
                    {
                        return new BaseResponse<TopLeaguesModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<TopLeaguesModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<PlatformMessageModel>> PostTopLeaguesAsync(PostTopLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var Model = await _httpClient.PostAsync($"sportsbook/settings/topleagues/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PlatformMessageModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<PlatformMessageModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PlatformMessageModel>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<PlatformMessageModel>();
                    else
                    {
                        return new BaseResponse<PlatformMessageModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<PlatformMessageModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> CopyTopLeaguesAsync(CopyTopLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var Model = await _httpClient.PostAsync($"sportsbook/settings/topleagues/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<bool>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<bool>();
                    else
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
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

        #endregion

        #region Methods - Others
        public async Task<BaseResponse<SportsTreeResponse>> GetSportsTreeAsync(int fixtureTypeId)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsTreeResponse>>
                (await _httpClient.GetStreamAsync($"api/trading/sports/{fixtureTypeId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<SportsTreeResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new SportsTreeResponse()
                };
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

        public async Task<BaseResponse<SportsLookupModel>> GetSportsAsync(string languageCode)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsLookupModel>>
                (await _httpClient.GetStreamAsync($"sportsbook/settings/sports?languageCode={languageCode}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<SportsLookupModel>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new SportsLookupModel()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportsLookupModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientParlayBonusModel>> GetClientParlayBonus(GetParlayBonusRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var requestUri = $"sportsbook/settings/parlaybonus/list?ClientIdRef={request.ClientIdRef}&Page={request.Page}&PageSize={request.PageSize}";

                var Model = await _httpClient.GetAsync(requestUri);

                if (Model.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<ClientParlayBonusModel>>
                        (await Model.Content.ReadAsStringAsync(),
                        new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                        new BaseResponse<ClientParlayBonusModel>();
                }
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<BaseResponse<ClientParlayBonusModel>>
                            (await Model.Content.ReadAsStringAsync(),
                            new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                            new BaseResponse<ClientParlayBonusModel>();

                        return result;
                    }
                    else
                    {
                        return new BaseResponse<ClientParlayBonusModel>()
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientParlayBonusModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var Model = await _httpClient.PostAsync($"sportsbook/settings/parlaybonus", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<StatusCode>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<StatusCode>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<StatusCode>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<StatusCode>();
                    else
                    {
                        return new BaseResponse<StatusCode>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
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

        public async Task<BaseResponse<SportsTreeModel>> GetSportstreeByClientId(int clientId)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsTreeModel>>
                (await _httpClient.GetStreamAsync($"sportsbook/settings/topfixtures/sportstree?clientId={clientId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<SportsTreeModel>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new SportsTreeModel()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportsTreeModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Methods - Trading
        public async Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<List<MainMarketTemplateResponse>>>
                (await _httpClient.GetStreamAsync($"api/trading/sports/topmarkets/{sportId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<List<MainMarketTemplateResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new List<MainMarketTemplateResponse>()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<MainMarketTemplateResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(MarketFixtureRequest request)
        {
            try
            {
                var stringUri = string.IsNullOrEmpty(request.MarketTemplateId) ?
                    $"api/trading/fixtures/market/{request.FixtureTypeId}/{request.SportId}/{request.SegmentId}/{request.LeagueId}" :
                    $"api/trading/fixtures/market/{request.FixtureTypeId}/{request.SportId}/{request.SegmentId}/{request.LeagueId}/{request.MarketTemplateId}";

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<MarketFixtureResponse>>
                (await _httpClient.GetStreamAsync(stringUri), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MarketFixtureResponse()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MarketFixtureResponse()
                };
            }
        }

        public async Task<BaseResponse<MarketFixtureDetailResponse>> GetMarketFixtureDetails(string providerFixtureId)
        {
            try
            {
                var stringUri = $"api/trading/fixtures2/market/details/{providerFixtureId}";

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<MarketFixtureDetailResponse>>
                (await _httpClient.GetStreamAsync(stringUri), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<MarketFixtureDetailResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MarketFixtureDetailResponse()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketFixtureDetailResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MarketFixtureDetailResponse()
                };
            }
        }

        public async Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups(int fixtureTypeId)
        {
            try
            {
                var stringUri = $"api/trading/markettemplategroups/{fixtureTypeId}";

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<MarketTemplateGroupResponse>>
                (await _httpClient.GetStreamAsync(stringUri), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<MarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MarketTemplateGroupResponse()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MarketTemplateGroupResponse()
                };
            }
        }

        public async Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);
                var Model = await _httpClient.PostAsync($"api/trading/markettemplateplayerprops", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<List<MarketTemplatePlayerPropsResponse>>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<List<MarketTemplatePlayerPropsResponse>>();
                    else
                    {
                        return new BaseResponse<List<MarketTemplatePlayerPropsResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
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
        #endregion

    }
}
