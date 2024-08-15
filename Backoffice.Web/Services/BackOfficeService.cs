using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backoffice.Web.Extensions;
using static Backoffice.Shared.Models.Response.SportsTreeResponse;

namespace Backoffice.Web.Services
{
    public class BackofficeService : IBackofficeService
    {
        #region Fields
        private HttpClient _client;
        private HttpClient _httpClient
        {
            get
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                return _client;
            }
            set
            {
                _client = value;
            }
        }

        private readonly IConfiguration _configuration;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMessageBroker _messageBroker;
        private readonly IHttpContextAccessor _httpContextAccessor;
        #endregion

        #region Ctor

        public BackofficeService(HttpClient httpClient, IConfiguration configuration, IAuthenticationService authenticationService, IMessageBroker messageBroker, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _authenticationService = authenticationService;
            _messageBroker = messageBroker;
            _httpContextAccessor = httpContextAccessor;

            _httpClient = httpClient;
        }

        #endregion

        #region Method - Maintenance

        public async Task<BaseResponse<bool>> GetMaintenance()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/config/maintenance");

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> SetMaintenance(bool request)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_configuration["API:Backoffice:Uri"]);
            httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["API:Backoffice:Security:APIKey"]);
            httpClient.DefaultRequestHeaders.Add("x-region", _configuration["API:Region"]);

            try
            {
                httpClient.Timeout = TimeSpan.FromMinutes(3);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _httpContextAccessor.HttpContext?.Session.GetString("UserToken"));

                var response = await httpClient.PostAsync($"api/config/maintenance/{request}", null);

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Methods - Activity Logs

        public async Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/config/activitylog/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = new List<ActivityLogModel>()
                    }
                };
            }
        }
        public async Task<BaseResponse<MessageResponse>> AddActivityLog(ActivityLogModel request)
        {
            try
            {
                _messageBroker.Publish(JsonSerializer.Serialize(request), "LogActivity", "r.bo.util.logs", Guid.NewGuid().ToString());

                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MessageResponse
                    {
                        IsSuccess = true
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<MessageResponse>> AddActivityLogList(OnActivityLogListMessage request)
        {
            try
            {
                _messageBroker.Publish(JsonSerializer.Serialize(request), "LogActivityList", "r.bo.util.logs", Guid.NewGuid().ToString());

                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MessageResponse
                    {
                        IsSuccess = true
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MessageResponse>
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
                var response = await _httpClient.PostAsync($"api/config/activitylog/priceboostlist", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = new List<ActivityLogModel>()
                    }
                };
            }
        }

        public async Task<BaseResponse<ActivityLogResponse>> GetManualBetSettlementActivityLogs(ManualBetSettlementActivityLogRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/config/activitylog/manualbetsettlementlist", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ActivityLogResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                            Result = new ActivityLogResponse { ActivityLogs = new List<ActivityLogModel>() }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = new List<ActivityLogModel>()
                    }
                };
            }
        }

        #endregion

        #region Methods - Client

        public async Task<BaseResponse<ClientModel>> GetClients()
        {
            try
            {
                var response = await _httpClient.GetAsync($"sportsbook/client/list");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    var test = JsonSerializer.Deserialize<BaseResponse<ClientModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    return new BaseResponse<ClientModel>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        Result = new ClientModel()
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateClient(ClientListResponse.Client request)
        {
            try
            {
                var Model = await _httpClient.PostAsync($"sportsbook/client/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<BetSettingResponse>> GetBetSetting(BetSettingRequest request)
        {
            try
            {
                var Model = await _httpClient.PostAsync($"sportsbook/client/bet/setting", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<BetSettingResponse>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<BetSettingResponse>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<BetSettingResponse>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<BetSettingResponse>();
                    else
                    {
                        return new BaseResponse<BetSettingResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<BetSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ExchangeRatesResponse>> GetExchangeRates()
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<ExchangeRatesResponse>>
                (await _httpClient.GetStreamAsync($"sportsbook/client/exchange/rates"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<ExchangeRatesResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new ExchangeRatesResponse()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ExchangeRatesResponse>
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
                var response = await _httpClient.PostAsync($"sportsbook/settings/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<ClientTopFixtureModel>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<ClientTopFixtureModel>();
                    else
                    {
                        return new BaseResponse<ClientTopFixtureModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Methods - Others
        public async Task<BaseResponse<SportsTreeResponse>> GetSportsTreeAsync(int fixtureTypeId, LanguageCode langCode)
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsTreeResponse>>
                (await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                  .GetStreamAsync($"api/trading/sports/{fixtureTypeId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<SportsTreeResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new SportsTreeResponse()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<SportsTreeResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<SportsLookupModel>> GetSportsAsync()
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsLookupModel>>
                (await _httpClient.GetStreamAsync($"api/trading/sports/lookup"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<SportsLookupModel>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new SportsLookupModel()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<SportsTreeModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Methods - Trading
        public async Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, LanguageCode langCode)
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<List<MainMarketTemplateResponse>>>
                (await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                  .GetStreamAsync($"api/trading/sports/topmarkets/{sportId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<List<MainMarketTemplateResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new List<MainMarketTemplateResponse>()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<MainMarketTemplateResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(MarketFixtureRequest request, LanguageCode langCode)
        {
            try
            {
                var stringUri = string.IsNullOrEmpty(request.MarketTemplateId) ?
                    $"api/trading/fixtures/market/{request.FixtureTypeId}/{request.SportId}/{request.SegmentId}/{request.LeagueId}" :
                    $"api/trading/fixtures/market/{request.FixtureTypeId}/{request.SportId}/{request.SegmentId}/{request.LeagueId}/{request.MarketTemplateId}";

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<MarketFixtureResponse>>
                (await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                  .GetStreamAsync(stringUri), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MarketFixtureResponse()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MarketFixtureResponse()
                };
            }
        }

        public async Task<BaseResponse<MarketFixtureDetailResponse>> GetMarketFixtureDetails(string providerFixtureId, LanguageCode langCode)
        {
            try
            {
                var stringUri = $"api/trading/fixtures2/market/details/{providerFixtureId}";

                var result = await JsonSerializer.DeserializeAsync<BaseResponse<MarketFixtureDetailResponse>>
                (await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                  .GetStreamAsync(stringUri), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<MarketFixtureDetailResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MarketFixtureDetailResponse()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<MarketTemplatePlayerPropsResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Bet Settlement

        public async Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bet/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ManualBetDetailsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ManualBetSettlementResponse>> ManualSettlement(ManualBetSettlementRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bet/manualsettlement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ManualBetSettlementResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualBetSettlementResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ManualBetSettlementResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ManualBetSettlementResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ResendSettlementBetDetailModel>> GetResendSettlementBetDetails(ResendSettlementBetDetailRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bet/resendsettlement/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ResendSettlementBetDetailModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ResendSettlementBetDetailModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ResendSettlementBetDetailModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ResendSettlementBetDetailModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ResendSettlementResponse>> ResendSettlement(ResendSettlementRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bet/resendsettlement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ResendSettlementResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<BetModifyResponse>> CancelBet(BetModifyRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bet/cancel", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<BetModifyResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<BetModifyResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<BetModifyResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Deactivate Fixture Markets
        public async Task<BaseResponse<bool>> DeactivateFixtureMarkets(DeactivateFixtureMarketRequest request)
        {
            try
            {
                var Model = await _httpClient.PostAsync($"api/trading/fixture/market/deactivate", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Config

        public async Task<BaseResponse<TranslationResponse>> GetTranslation(LanguageCode langCode)
        {
            try
            {
                var response = await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                                .GetAsync($"api/config/translations");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<TranslationResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<TranslationResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<TranslationResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<TranslationResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(long configTemplateId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/config/template/{configTemplateId}");

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<ConfigTemplateModel>();
                }
                else
                {
                    return new BaseResponse<ConfigTemplateModel>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<ConfigTemplateModel>>> GetConfigTemplate(string searchText, int sportId = -1, int scopeId = -1)
        {
            try
            {
                var path = "api/config/templates";

                var queries = new List<string>();

                if (!string.IsNullOrEmpty(searchText))
                    queries.Add($"searchText={searchText}");

                if (sportId != -1)
                    queries.Add($"sportId={sportId}");

                if (scopeId != -1)
                    queries.Add($"scopeId={scopeId}");

                if (queries != null && queries.Any() && queries.Count > 0)
                    path += $"?{string.Join("&", queries)}";

                var response = await _httpClient.GetAsync($"{path}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ConfigTemplateModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ConfigTemplateModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<ConfigTemplateModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigTemplateModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<long>> AddOrUpdateConfigTemplate(ConfigTemplateModel request)
        {
            try
            {
                var Model = await _httpClient.PostAsync($"api/config/template", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<long>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<long>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<long>>(await Model.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<long>();
                    else
                    {
                        return new BaseResponse<long>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({Model.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<long>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(long id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/config/template/delete/{id}", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<List<ConfigTemplateModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<List<ConfigTemplateModel>>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<List<ConfigTemplateModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<List<ConfigTemplateModel>>();
                    }
                    else
                    {
                        return new BaseResponse<List<ConfigTemplateModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigTemplateModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> SetLeagueState(ConfigState state, ConfigSetLeagueStateRequest payload)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/config/template/set/{state.ToString()}", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<bool>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<bool>();
                    }
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
        public async Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigClient(ConfigState state, ConfigTemplateLeagueRequest payload, int templateId = -1)
        {
            try
            {
                var query = templateId != -1 ? $"?templateId={templateId}" : "";
                var response = await _httpClient.PostAsync($"api/config/template/{state.ToString()}{query}", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateLeagueResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<ConfigTemplateLeagueResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateLeagueResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<ConfigTemplateLeagueResponse>();
                    }
                    else
                    {
                        return new BaseResponse<ConfigTemplateLeagueResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateLeagueResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<int>> GetTemplateConfigCount(string state = "", CancellationToken token = default)
        {
            try
            {
                var query = string.IsNullOrEmpty(state) ? "" : $"?state={state}";
                var response = await _httpClient.GetAsync($"api/config/template/count{query}", token);

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<int>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<int>();
                }
                else
                {
                    return new BaseResponse<int>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<int>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ConfigTemplateMarginModel>> GetConfigTemplateMargin(long configTemplateId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/config/template/margin/{configTemplateId}");

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateMarginModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? new BaseResponse<ConfigTemplateMarginModel>();
                }
                else
                {
                    return new BaseResponse<ConfigTemplateMarginModel>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateMarginModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateConfigTemplateMargin(ConfigTemplateMarginModel request)
        {
            try
            {
                var Model = await _httpClient.PostAsync($"api/config/template/margin", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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

        public async Task<BaseResponse<CopyClientConfigResponse>> CopyClientConfig(CopyClientConfigRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/config/template/copy/client", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<CopyClientConfigResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<CopyClientConfigResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<CopyClientConfigResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<CopyClientConfigResponse>();
                    else
                    {
                        return new BaseResponse<CopyClientConfigResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<CopyClientConfigResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        public void Dispose()
        {
            //_httpClient.Dispose();
            //_authenticationService.Dispose();

            Console.WriteLine("- {0} was disposed!", this.GetType().Name);
        }

    }
}
