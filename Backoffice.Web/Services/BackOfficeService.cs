using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Pages;
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

        public async Task<BaseResponse<ClientResponse>> GetClients()
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<ClientResponse>>
                (await _httpClient.GetStreamAsync($"sportsbook/Client/list"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<ClientResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new ClientResponse()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientResponse>
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

                var response = await _httpClient.PostAsync($"sportsbook/client/information/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<ClientListResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<ClientListResponse>();
                    else
                    {
                        return new BaseResponse<ClientListResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
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
        public async Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(GetClientTopFixtureRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);
                var response = await _httpClient.PostAsync($"sportsbook/settings/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<ClientTopFixtureResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<ClientTopFixtureResponse>();
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
            catch (Exception ex)
            {
                return new BaseResponse<ClientTopFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<PlatformMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);
                var response = await _httpClient.PostAsync($"sportsbook/settings/topfixtures/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<PlatformMessageResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<PlatformMessageResponse>();
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

        public async Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);
                var response = await _httpClient.PostAsync($"sportsbook/settings/topfixtures/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<bool>();
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
        #endregion

        #region Methods - Top Leagues

        public async Task<BaseResponse<GetAllLeaguesResponse>> GetAllLeaguesAsync(GetAllLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var response = await _httpClient.PostAsync($"sportsbook/settings/allleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<GetAllLeaguesResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<GetAllLeaguesResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<GetAllLeaguesResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<GetAllLeaguesResponse>();
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
        public async Task<BaseResponse<GetTopLeaguesResponse>> GetTopLeaguesAsync(GetTopLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var response = await _httpClient.PostAsync($"sportsbook/settings/topleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<GetTopLeaguesResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<GetTopLeaguesResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<GetTopLeaguesResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<GetTopLeaguesResponse>();
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
            catch (Exception ex)
            {
                return new BaseResponse<GetTopLeaguesResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<PlatformMessageResponse>> PostTopLeaguesAsync(PostTopLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var response = await _httpClient.PostAsync($"sportsbook/settings/topleagues/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<PlatformMessageResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PlatformMessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<PlatformMessageResponse>();
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
        public async Task<BaseResponse<bool>> CopyTopLeaguesAsync(CopyTopLeaguesRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var response = await _httpClient.PostAsync($"sportsbook/settings/topleagues/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<bool>();
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

        #endregion

        #region Methods - Others
        public async Task<BaseResponse<SportsLookupResponse>> GetSportsAsync(string languageCode)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsLookupResponse>>
                (await _httpClient.GetStreamAsync($"sportsbook/settings/sports?languageCode={languageCode}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new SportsLookupResponse()
                };
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

        public async Task<BaseResponse<ClientParlayBonusResponse>> GetClientParlayBonus(GetParlayBonusRequest request)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var requestUri = $"sportsbook/settings/parlaybonus/list?ClientIdRef={request.ClientIdRef}&Page={request.Page}&PageSize={request.PageSize}";

                var response = await _httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<ClientParlayBonusResponse>>
                        (await response.Content.ReadAsStringAsync(), 
                        new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                        new BaseResponse<ClientParlayBonusResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<BaseResponse<ClientParlayBonusResponse>>
                            (await response.Content.ReadAsStringAsync(),
                            new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                            new BaseResponse<ClientParlayBonusResponse>();

                        return result;
                    }                    
                    else
                    {
                        return new BaseResponse<ClientParlayBonusResponse>()
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
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

        public async Task<BaseResponse<SportsTreeResponse>> GetSportstreeByClientId(int clientId)
        {
            try
            {
                var userToken = await _authenticationService.AuthenticateToken();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken.AccessToken);

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsTreeResponse>>
                (await _httpClient.GetStreamAsync($"sportsbook/settings/topfixtures/sportstree?clientId={clientId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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

        #endregion
    }
}
