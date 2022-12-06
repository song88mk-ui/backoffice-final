using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Pages;
using System.Net;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class BackOfficeService : IBackOfficeService, IDisposable
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        #endregion

        #region Ctor
        public BackOfficeService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            var userToken = httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
            _httpClient.BaseAddress = new Uri(_configuration["API:Backoffice:Uri"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["API:Backoffice:Security:APIKey"]);
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
        }
        #endregion

        #region Methods - Client
        public async Task<BaseResponse<ClientListResponse>> GetClients()
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<ClientListResponse>>
                (await _httpClient.GetStreamAsync($"sportsbook/Client/list"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new ClientListResponse()
                };
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

        public async Task<BaseResponse<SportsTreeResponse>> GetSportstreeByClientId(int clientId)
        {
            try
            {
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

        public async Task<BaseResponse<PlatformMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request)
        {
            try
            {
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
                var response = await _httpClient.PostAsync($"sportsbook/settings/topfixtures/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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

        #region Top Leagues
        public async Task<BaseResponse<GetTopLeaguesResponse>> GetTopLeaguesAsync(GetTopLeaguesRequest request)
        {
            try
            {
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
        public async Task<BaseResponse<SportsLookupResponse>> GetSportsAsync(string languageCode, string region)
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsLookupResponse>>
                (await _httpClient.GetStreamAsync($"sportsbook/settings/sports?language={languageCode}&region={region}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = result?.Result ?? new SportsLookupResponse()
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
        #endregion
        
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
