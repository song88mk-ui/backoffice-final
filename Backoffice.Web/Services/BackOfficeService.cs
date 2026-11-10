using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Extensions;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.States;
using Microsoft.JSInterop;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class BackofficeService : BaseService, IBackofficeService
    {
        #region Fields
        private HttpClient _client;
        private HttpClient _httpClient
        {
            get
            {
                if (_client == null)
                {
                    _client = new HttpClient();
                }

                ConfigureHttpClientHeaders();

                return _client;
            }
            set => _client = value;
        }

        private readonly IConfiguration _configuration;
        private readonly IAuthenticationService _authenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJSRuntime _jsRuntime;
        #endregion

        #region Ctor

        public BackofficeService(IHttpClientFactory clientFactory, IConfiguration configuration, IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor, IJSRuntime jsRuntime, UserState userState) : base(clientFactory.CreateClient("backoffice-api"), userState)
        {
            _configuration = configuration;
            _authenticationService = authenticationService;
            _httpContextAccessor = httpContextAccessor;
            _jsRuntime = jsRuntime;

            _client = clientFactory.CreateClient("backoffice-api");
        }

        #endregion


        private void ConfigureHttpClientHeaders()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userState.Model.AccessToken);

            RemoveHeader("x-page");
            RemoveHeader("x-language-code");

            if (!string.IsNullOrEmpty(Page))
            {
                _client.DefaultRequestHeaders.Add("x-page", Page);
            }

            _client.DefaultRequestHeaders.Add("x-language-code", LanguageCode.ToString());
        }

        private void RemoveHeader(string headerName)
        {
            if (_client.DefaultRequestHeaders.Contains(headerName))
            {
                _client.DefaultRequestHeaders.Remove(headerName);
            }
        }

        #region Method - Maintenance

        public async Task<BaseResponse<MaintenanceResponse>> GetMaintenanceV2()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/config/maintenance");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MaintenanceResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MaintenanceResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<MaintenanceResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<MaintenanceResponse>> SetMaintenanceV2(SetMaintenanceRequest request)
        {
            try
            {
                var endpoint = "api/config/maintenance";

                var response = await _httpClient.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MaintenanceResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MaintenanceResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        Log.Error($"{endpoint} - {response.StatusCode.ToString()} - {response.ReasonPhrase}");

                        return new BaseResponse<MaintenanceResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Method - Feature Flags

        public async Task<BaseResponse<ClientFeatureFlagsResponse>> GetClientFeatureFlags(string clientId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/config/feature-flags/{clientId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientFeatureFlagsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
                        return JsonSerializer.Deserialize<BaseResponse<ClientFeatureFlagsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<ClientFeatureFlagsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientFeatureFlagsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientFeatureFlagsResponse>> UpdateClientFeatureFlags(UpdateClientFeatureFlagsRequest request)
        {
            try
            {
                var endpoint = $"api/config/feature-flags/{request.ClientId}";

                var response = await _httpClient.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientFeatureFlagsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientFeatureFlagsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        Log.Error($"{endpoint} - {response.StatusCode.ToString()} - {response.ReasonPhrase}");

                        return new BaseResponse<ClientFeatureFlagsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientFeatureFlagsResponse>
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
                if (request != null)
                {
                    if (request.DateFrom == DateTime.MinValue)
                        request.DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-1);
                    if (request.DateTo == DateTime.MinValue)
                        request.DateTo = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1);

                    // DateFrom/DateTo are the user's local wall-clock boundaries (per _userState.TimeZoneOffset,
                    // the browser's real offset). ToUniversalTime() assumed the *server's* local offset instead,
                    // which is UTC in every hosting environment, making it a no-op and sending local time
                    // mislabeled as UTC. Subtract the real client offset to get the correct UTC instant.
                    request.DateFrom = DateTime.SpecifyKind(request.DateFrom - _userState.TimeZoneOffset, DateTimeKind.Utc);
                    request.DateTo = DateTime.SpecifyKind(request.DateTo - _userState.TimeZoneOffset, DateTimeKind.Utc);
                }

                var response = await _httpClient.PostAsync($"api/config/activitylog/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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
        // Market Settlement history: same shape as GetActivityLogs, but posts to the trading route
        // (gated on Admin.Settlements.MarketSettlement) rather than api/config/activitylog/list,
        // which requires the unrelated SBC.OE.Config permission.
        public async Task<BaseResponse<ActivityLogResponse>> GetMarketSettlementActivityLogs(ActivityLogRequest request)
        {
            try
            {
                if (request != null)
                {
                    if (request.DateFrom == DateTime.MinValue)
                        request.DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-1);
                    if (request.DateTo == DateTime.MinValue)
                        request.DateTo = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1);

                    request.DateFrom = request.DateFrom.ToUniversalTime();
                    request.DateTo = request.DateTo.ToUniversalTime();
                }

                var response = await _httpClient.PostAsync($"api/trading/manual/marketsettlement/history", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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
                var result = new BaseResponse<bool>();
                var response = await _httpClient.PostAsync($"api/config/activitylog", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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

                return new BaseResponse<MessageResponse>()
                {
                    StatusCode = result.StatusCode,
                    Message = result.Message,
                    Result = new MessageResponse()
                    {
                        IsSuccess = result.Result
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MessageResponse()
                    {
                        IsSuccess = false
                    }
                };
            }
        }
        public async Task<BaseResponse<MessageResponse>> AddActivityLogList(ActivityLogListRequest request)
        {
            try
            {
                var result = new BaseResponse<bool>();
                var response = await _httpClient.PostAsync($"api/config/set/activitylog/list", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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

                return new BaseResponse<MessageResponse>()
                {
                    StatusCode = result.StatusCode,
                    Message = result.Message,
                    Result = new MessageResponse()
                    {
                        IsSuccess = result.Result
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MessageResponse()
                    {
                        IsSuccess = false
                    }
                };
            }
        }
        public async Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/config/activitylog/priceboostlist", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ActivityLogResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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

        public async Task<BaseResponse<PlatformClientModel>> GetPlatformClients()
        {
            try
            {
                var response = await _httpClient.GetAsync($"sportsbook/client/platform/list");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BaseResponse<List<PlatformClientResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);

                    return new BaseResponse<PlatformClientModel>()
                    {
                        StatusCode = result.StatusCode,
                        Message = result.Message,
                        Result = new PlatformClientModel
                        {
                            Clients = result.Result
                        }
                    };
                }
                else
                {
                    return new BaseResponse<PlatformClientModel>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        Result = new PlatformClientModel()
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<PlatformClientModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientModel>> GetClients()
        {
            try
            {
                var response = await _httpClient.GetAsync($"sportsbook/client/list");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientModel>> GetClientsV2(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetAsync<ClientModel>($"sportsbook/client/list", cancellationToken) ?? new BaseResponse<ClientModel>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                var response = await _httpClient.PostAsync($"sportsbook/client/information/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientListResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
                        ?? new BaseResponse<ClientListResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientListResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
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
                Log.Error(ex, ex.Message);
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
                return await PostAsJsonAsync<bool, ClientListResponse.Client>($"sportsbook/client/post", request) ?? new BaseResponse<bool>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<string>> GetClientImportEnvironment()
        {
            try
            {
                return await GetAsync<string>($"sportsbook/clientimport/environment") ?? new BaseResponse<string>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientImportProgressResponse>> GetClientImportProgress(Guid batchId)
        {
            try
            {
                return await GetAsync<ClientImportProgressResponse>($"sportsbook/clientimport/progress/{batchId}") ?? new BaseResponse<ClientImportProgressResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientImportProgressResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<ClientExportRow>>> ExportClients(string search, int sortBy)
        {
            try
            {
                return await GetAsync<List<ClientExportRow>>($"sportsbook/clientimport/export?search={Uri.EscapeDataString(search ?? string.Empty)}&sortBy={sortBy}") ?? new BaseResponse<List<ClientExportRow>>();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientExportRow>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ValidateClientUpdateResponse>> ValidateClientUpdate(ValidateClientUpdateRequest request)
        {
            try
            {
                return await PostAsJsonAsync<ValidateClientUpdateResponse, ValidateClientUpdateRequest>($"sportsbook/clientimport/validate-update", request) ?? new BaseResponse<ValidateClientUpdateResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ValidateClientUpdateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<CommitClientUpdateResponse>> CommitClientUpdate(CommitClientUpdateRequest request)
        {
            try
            {
                return await PostAsJsonAsync<CommitClientUpdateResponse, CommitClientUpdateRequest>($"sportsbook/clientimport/commit-update", request) ?? new BaseResponse<CommitClientUpdateResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<CommitClientUpdateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<ClientImportBatchSummary>>> GetClientImportBatchHistory(string operationType)
        {
            try
            {
                return await GetAsync<List<ClientImportBatchSummary>>($"sportsbook/clientimport/batches?operationType={operationType}") ?? new BaseResponse<List<ClientImportBatchSummary>>();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientImportBatchSummary>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<ClientFieldChangeResponse>>> GetClientImportFieldChanges(Guid batchId)
        {
            try
            {
                return await GetAsync<List<ClientFieldChangeResponse>>($"sportsbook/clientimport/fieldchanges/{batchId}") ?? new BaseResponse<List<ClientFieldChangeResponse>>();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientFieldChangeResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ValidateClientImportResponse>> ValidateClientImport(ValidateClientImportRequest request)
        {
            try
            {
                return await PostAsJsonAsync<ValidateClientImportResponse, ValidateClientImportRequest>($"sportsbook/clientimport/validate", request) ?? new BaseResponse<ValidateClientImportResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ValidateClientImportResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<CommitClientImportResponse>> CommitClientImport(CommitClientImportRequest request)
        {
            try
            {
                return await PostAsJsonAsync<CommitClientImportResponse, CommitClientImportRequest>($"sportsbook/clientimport/commit", request) ?? new BaseResponse<CommitClientImportResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<CommitClientImportResponse>
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
                return await PostAsJsonAsync<BetSettingResponse, BetSettingRequest>($"sportsbook/client/bet/setting", request) ?? new BaseResponse<BetSettingResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                (await _httpClient.GetStreamAsync($"sportsbook/client/exchange/rates"), JsonSerializerOptionsProvider.CaseInsensitive);

                return result ?? new BaseResponse<ExchangeRatesResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new ExchangeRatesResponse()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                return await PostAsJsonAsync<ClientTopFixtureModel, GetClientTopFixtureRequest>($"sportsbook/settings/topfixtures", request);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                return await PostAsJsonAsync<PlatformMessageModel, PostClientTopFixtureRequest>($"sportsbook/settings/topfixtures/post", request);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
                        ?? new BaseResponse<bool>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<GetAllLeaguesModel>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<GetAllLeaguesModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<GetAllLeaguesModel>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<GetAllLeaguesModel>();
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<TopLeaguesModel>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<TopLeaguesModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<TopLeaguesModel>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<TopLeaguesModel>();
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<PlatformMessageModel>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<PlatformMessageModel>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PlatformMessageModel>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<PlatformMessageModel>();
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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
                Log.Error(ex, ex.Message);
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
        public async Task<BaseResponse<SportsTreeResponse>> GetSportsTreeAsync(int fixtureTypeId, LanguageCode langCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } }).GetAsync($"api/trading/sports/{fixtureTypeId}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(
                        await response.Content.ReadAsStringAsync(cancellationToken),
                        JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<SportsTreeResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>
                        (
                            await response.Content.ReadAsStringAsync(cancellationToken),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<SportsTreeResponse>();
                    }
                    else
                    {
                        Log.Error($"[api/trading/sports/{fixtureTypeId}] - {_userState.Model.AccessToken} - {response.ReasonPhrase}");

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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<SportsTreeResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        //public async Task<BaseResponse<SportsLookupModel>> GetSportsAsync()
        //{
        //    try
        //    {
        //        var result = await JsonSerializer.DeserializeAsync<BaseResponse<SportsLookupModel>>
        //        (await _httpClient.GetStreamAsync($"api/trading/sports/lookup"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        //        return result ?? new BaseResponse<SportsLookupModel>
        //        {
        //            StatusCode = StatusCode.Success,
        //            Message = $"{StatusCode.Success}",
        //            Result = new SportsLookupModel()
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //        if (ex.StackTrace != null)
        //            Log.Error(ex.StackTrace.ToString());

        //        return new BaseResponse<SportsLookupModel>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}

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
                        JsonSerializerOptionsProvider.CaseInsensitive) ??
                        new BaseResponse<ClientParlayBonusModel>();
                }
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<BaseResponse<ClientParlayBonusModel>>
                            (await Model.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive) ??
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<StatusCode>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
                        ?? new BaseResponse<StatusCode>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<StatusCode>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
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
                Log.Error(ex, ex.Message);
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
                var response = await _httpClient.GetAsync($"sportsbook/settings/topfixtures/sportstree?clientId={clientId}");
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BaseResponse<SportsTreeModel>>(body, JsonSerializerOptionsProvider.CaseInsensitive);
                    return result ?? new BaseResponse<SportsTreeModel>
                    {
                        StatusCode = StatusCode.Success,
                        Message = $"{StatusCode.Success}",
                        Result = new SportsTreeModel()
                    };
                }
                else
                {
                    return JsonSerializer.Deserialize<BaseResponse<SportsTreeModel>>(body, JsonSerializerOptionsProvider.CaseInsensitive)
                        ?? new BaseResponse<SportsTreeModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"Error ({response.ReasonPhrase})"
                        };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                var response = await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                  .GetAsync($"api/trading/sports/topmarkets/{sportId}");

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>(
                        await response.Content.ReadAsStringAsync(),
                        JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<List<MainMarketTemplateResponse>>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {

                        var content = await response.Content.ReadAsStringAsync();
                        Log.Error($"[api/trading/client] - {_userState.Model.AccessToken} - {response.ReasonPhrase}");
                        return JsonSerializer.Deserialize<BaseResponse<List<MainMarketTemplateResponse>>>
                        (
                            content,
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<List<MainMarketTemplateResponse>>();
                    }
                    else
                    {
                        Log.Error($"[api/trading/client] - {_userState.Model.AccessToken} - {response.ReasonPhrase}");

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
                Log.Error(ex, ex.Message);
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

                var response = await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                  .GetAsync(stringUri);

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(
                        await response.Content.ReadAsStringAsync(),
                        JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<MarketFixtureResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                            ) ?? new BaseResponse<MarketFixtureResponse>();
                    }
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MarketFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<MarketFixtureResponseV2>> GetMostEvenLineMarketFixtures(MarketFixtureRequest request, LanguageCode langCode)
        {
            try
            {
                var stringUri = $"api/trading/fixtures/{request.FixtureTypeId}/sports/{request.SportId}/segments/{request.SegmentId}/leagues/{request.LeagueId}/markets/{request.MarketTemplateId}/most-even-line";

                var response = await _httpClient.WithRequestHeader(new Dictionary<string, string>() { { "x-language-code", langCode.ToString() } })
                                  .GetAsync(stringUri);

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(
                        await response.Content.ReadAsStringAsync(),
                        JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<MarketFixtureResponseV2>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponseV2>>(
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                            ) ?? new BaseResponse<MarketFixtureResponseV2>();
                    }
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<MarketFixtureResponseV2>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
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
                                  .GetStreamAsync(stringUri), JsonSerializerOptionsProvider.CaseInsensitive);

                return result ?? new BaseResponse<MarketFixtureDetailResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MarketFixtureDetailResponse()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                (await _httpClient.GetStreamAsync(stringUri), JsonSerializerOptionsProvider.CaseInsensitive);

                return result ?? new BaseResponse<MarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success}",
                    Result = new MarketTemplateGroupResponse()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
                        ?? new BaseResponse<List<MarketTemplatePlayerPropsResponse>>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
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
                Log.Error(ex, ex.Message);
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

        #region Methods - Announcements
        public async Task<BaseResponse<AnnouncementResponse>> GetAnnouncements(AnnouncementRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/settings/announcements", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<AnnouncementResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<AnnouncementResponse>();
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<AnnouncementResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<AnnouncementResponse>> AddOrUpdateAnnouncements(AddOrUpdateAnnouncementRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/settings/announcements/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<AnnouncementResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<AnnouncementResponse>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<AnnouncementResponse>();
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<AnnouncementResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteAnnouncements(DeleteAnnouncementRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/settings/announcements/delete", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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
                Log.Error(ex, ex.Message);
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

        #region Bet Settlement

        public async Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bet/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<ManualBetSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualBetSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<ResendSettlementBetDetailModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ResendSettlementBetDetailModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ResendSettlementResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        // BO-125: resend a Cancelled bet's status to the provider (Bet Genius / GTS).
        public async Task<BaseResponse<ResendSettlementResponse>> ResendProviderCancellation(ResendProviderCancellationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bet/resendprovidercancellation", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ResendSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<BetModifyResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<BetModifyResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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
                Log.Error(ex, ex.Message);
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
                    return JsonSerializer.Deserialize<BaseResponse<TranslationResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<TranslationResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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

        public async Task<BaseResponse<List<SportsbookTranslationResponse>>> GetSportsbookTranslations(string languageCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/translation/sportsbook/translations?languageCode={languageCode}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<SportsbookTranslationResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<SportsbookTranslationResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<List<SportsbookTranslationResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SportsbookTranslationResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<TranslationStatsResponse>> GetSportsbookTranslationStats()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/translation/sportsbook/translations/stats");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<TranslationStatsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<TranslationStatsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<TranslationStatsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateSportsbookTranslation(AddOrUpdateSportsbookTranslationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/translations", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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

        public async Task<BaseResponse<bool>> DeleteSportsbookTranslation(DeleteSportsbookTranslationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/translations/delete", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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

        public async Task<BaseResponse<bool>> DeleteSportsbookTranslationKey(DeleteSportsbookTranslationKeyRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/translations/delete-key", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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

        public async Task<BaseResponse<SportsbookGlossaryListResponse>> GetSportsbookGlossary(GetSportsbookGlossaryRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/glossary/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportsbookGlossaryListResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<SportsbookGlossaryListResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportsbookGlossaryListResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<SportsbookGlossaryListResponse>();
                    else
                    {
                        return new BaseResponse<SportsbookGlossaryListResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportsbookGlossaryListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<TranslationStatsResponse>> GetSportsbookGlossaryStats()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/translation/sportsbook/glossary/stats");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<TranslationStatsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<TranslationStatsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<TranslationStatsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateSportsbookGlossary(AddOrUpdateSportsbookGlossaryRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/glossary", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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

        public async Task<BaseResponse<bool>> SetSportsbookGlossaryProcessed(SetSportsbookGlossaryProcessedRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/glossary/processed", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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

        public async Task<BaseResponse<bool>> DeleteSportsbookGlossary(DeleteSportsbookGlossaryRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/glossary/delete", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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

        public async Task<BaseResponse<List<SportsbookGlossaryExportRow>>> ExportSportsbookGlossary(string searchText, string targetLanguage = null, bool untranslatedOnly = false, bool exactMatch = false)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(searchText))
                    queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");
                if (!string.IsNullOrWhiteSpace(targetLanguage))
                    queryParams.Add($"targetLanguage={Uri.EscapeDataString(targetLanguage)}");
                if (untranslatedOnly)
                    queryParams.Add("untranslatedOnly=true");
                if (exactMatch)
                    queryParams.Add("exactMatch=true");

                var query = queryParams.Count == 0 ? string.Empty : $"?{string.Join("&", queryParams)}";
                var response = await _httpClient.GetAsync($"api/translation/sportsbook/glossary/export{query}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<SportsbookGlossaryExportRow>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<List<SportsbookGlossaryExportRow>>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<SportsbookGlossaryExportRow>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<List<SportsbookGlossaryExportRow>>();
                    else
                    {
                        return new BaseResponse<List<SportsbookGlossaryExportRow>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SportsbookGlossaryExportRow>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ImportSportsbookGlossaryResponse>> ImportSportsbookGlossary(ImportSportsbookGlossaryRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/glossary/import", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ImportSportsbookGlossaryResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ImportSportsbookGlossaryResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ImportSportsbookGlossaryResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ImportSportsbookGlossaryResponse>();
                    else
                    {
                        return new BaseResponse<ImportSportsbookGlossaryResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ImportSportsbookGlossaryResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<SportsbookTranslationExportRow>>> ExportSportsbookTranslations()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/translation/sportsbook/translations/export");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<SportsbookTranslationExportRow>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<List<SportsbookTranslationExportRow>>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<SportsbookTranslationExportRow>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<List<SportsbookTranslationExportRow>>();
                    else
                    {
                        return new BaseResponse<List<SportsbookTranslationExportRow>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SportsbookTranslationExportRow>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ImportSportsbookTranslationResponse>> ImportSportsbookTranslations(ImportSportsbookTranslationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/translation/sportsbook/translations/import", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ImportSportsbookTranslationResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ImportSportsbookTranslationResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ImportSportsbookTranslationResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ImportSportsbookTranslationResponse>();
                    else
                    {
                        return new BaseResponse<ImportSportsbookTranslationResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ImportSportsbookTranslationResponse>
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
                    return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ConfigTemplateModel>();
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

        // BO-142: the template editor's market groups and market list come from the BO API's Sportsbook data, not the Odds Engine Trading API.
        public async Task<BaseResponse<MarketTemplateGroupResponse>> GetConfigMarketTemplateGroups(CancellationToken cancellationToken = default) =>
            await GetConfigMarketData<MarketTemplateGroupResponse>("api/config/market-groups", cancellationToken);

        public async Task<BaseResponse<MarketLookupResponse>> GetConfigMarketLookup(MarketLookupRequest request, CancellationToken cancellationToken = default) =>
            await GetConfigMarketData<MarketLookupResponse>($"api/config/markets/{request.FixtureTypeId}/{request.SportId}", cancellationToken);

        private async Task<BaseResponse<T>> GetConfigMarketData<T>(string path, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(path, cancellationToken);

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var result = JsonSerializer.Deserialize<BaseResponse<T>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive);

                    if (result != null)
                        return result;
                }

                return new BaseResponse<T>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<T>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ConfigTemplateResponse>> GetConfigTemplate(string searchText, int sportId = -1, int scopeId = -1, CancellationToken cancellationToken = default)
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

                var response = await _httpClient.GetAsync($"{path}", cancellationToken);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateResponse>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateResponse>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<ConfigTemplateResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateResponse>
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
                var response = await _httpClient.PostAsync($"api/config/template", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<long>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<long>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<long>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<long>();
                    else
                    {
                        return new BaseResponse<long>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
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
                    return JsonSerializer.Deserialize<BaseResponse<List<ConfigTemplateModel>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<List<ConfigTemplateModel>>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<List<ConfigTemplateModel>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<List<ConfigTemplateModel>>();
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
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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
        public async Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigClient(ConfigState state, ConfigTemplateLeagueRequest payload, long templateId = -1, CancellationToken token = default)
        {
            try
            {
                var query = templateId != -1 ? $"?templateId={templateId}" : "";
                var response = await _httpClient.PostAsync($"api/config/template/{state.ToString()}{query}", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), token);

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateLeagueResponse>>(await response.Content.ReadAsStringAsync(token), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ConfigTemplateLeagueResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateLeagueResponse>>(await response.Content.ReadAsStringAsync(token), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ConfigTemplateLeagueResponse>();
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
                    return JsonSerializer.Deserialize<BaseResponse<int>>(await response.Content.ReadAsStringAsync(token), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<int>();
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
                    return JsonSerializer.Deserialize<BaseResponse<ConfigTemplateMarginModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<ConfigTemplateMarginModel>();
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
                var Model = await _httpClient.PostAsync($"api/config/template/margin", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (Model.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
                else
                {
                    if (Model.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await Model.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive) ?? new BaseResponse<bool>();
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
                    return JsonSerializer.Deserialize<BaseResponse<CopyClientConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
                        ?? new BaseResponse<CopyClientConfigResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<CopyClientConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive)
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
                Log.Error(ex, ex.Message);
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

        #region Market Group

        public async Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(Guid clientId, int sportId, int fixtureStatusId)
        {
            try
            {
                var request = new ClientMarketTemplateRequest
                {
                    ClientId = clientId,
                    SportId = sportId,
                    FixtureStatusId = fixtureStatusId
                };

                var response = await _httpClient.PostAsync($"sportsbook/client/markettemplategroups", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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


        public async Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/markettemplategroup/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientPostMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }


        public async Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/markettemplategroup/delete", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientCopyMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/markettemplategroup/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientCopyMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientCopyMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<ClientCopyMarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientCopyMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Market Sort Order
        public async Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, LanguageCode langCode)
        {
            try
            {
                var respond = await _httpClient.GetStreamAsync($"sportsbook/client/markettemplate/{clientCode}/{sportId}/{fixtureStatusId}");
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<ClientMarketTemplateResponse>>
                (respond, JsonSerializerOptionsProvider.CaseInsensitive);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientMarketTemplateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<CopyClientMarketSortOrderResponse>> CopyClientMarketSortOrder(CopyClientMarketSortOrderRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/markettemplate/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<CopyClientMarketSortOrderResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<CopyClientMarketSortOrderResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<CopyClientMarketSortOrderResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<CopyClientMarketSortOrderResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<CopyClientSportSettingsResponse>> CopyClientSportSettings(CopyClientSportSettingsRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/sportsettings/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                // A partial copy comes back as 400 with the per-client results.
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
                    return JsonSerializer.Deserialize<BaseResponse<CopyClientSportSettingsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);

                return new BaseResponse<CopyClientSportSettingsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);

                return new BaseResponse<CopyClientSportSettingsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<ClientSelectorMarketTemplateModel>>> GetClientSelectorMarketTemplates(Guid clientId)
        {
            try
            {
                var respond = await _httpClient.GetStreamAsync($"sportsbook/client/selectormarkettemplate/{clientId}");
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<List<ClientSelectorMarketTemplateModel>>>
                (respond, JsonSerializerOptionsProvider.CaseInsensitive);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientSelectorMarketTemplateModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<SelectorMarketTemplateLookupModel>>> GetSelectorMarketTemplateLookup()
        {
            try
            {
                var respond = await _httpClient.GetStreamAsync($"sportsbook/client/selectormarkettemplate/lookup");
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<List<SelectorMarketTemplateLookupModel>>>
                (respond, JsonSerializerOptionsProvider.CaseInsensitive);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<SelectorMarketTemplateLookupModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateClientSelectorMarketTemplates(ClientSelectorMarketTemplateRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/selectormarkettemplate", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> CopyClientSelectorMarketTemplates(CopyClientSelectorMarketTemplateRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/selectormarkettemplate/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/markettemplate", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientMarketTemplateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Sport Settings
        public async Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/sportorder/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderListResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderListResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientSportOrderListResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/segmentorder/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientSegmentOrderListResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientSegmentOrderListResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientSegmentOrderListResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/leagueorder/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueOrderListResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientLeagueOrderListResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientLeagueOrderListResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public Task<BaseResponse<List<ClientOddstableLeagueOrderListResponse>>> GetClientOddstableLeagueOrderList(ClientOddstableLeagueOrderListRequest request)
            => PostOddstableLeagueSorting<ClientOddstableLeagueOrderListRequest, List<ClientOddstableLeagueOrderListResponse>>("sportsbook/client/oddstableleagueorder/list", request);
        public Task<BaseResponse<ClientOddstableLeagueSettingResponse>> AddOrUpdateClientOddstableLeagueSettings(ClientOddstableLeagueSettingRequest request)
            => PostOddstableLeagueSorting<ClientOddstableLeagueSettingRequest, ClientOddstableLeagueSettingResponse>("sportsbook/client/oddstableleaguesettings", request);
        public Task<BaseResponse<ClientOddstableLeagueSettingResponse>> CopyClientOddstableLeagueSettings(CopyClientOddstableLeagueSettingRequest request)
            => PostOddstableLeagueSorting<CopyClientOddstableLeagueSettingRequest, ClientOddstableLeagueSettingResponse>("sportsbook/client/oddstableleaguesettings/copy", request);
        public Task<BaseResponse<ImportClientOddstableLeagueSettingResponse>> ImportClientOddstableLeagueSettings(ImportClientOddstableLeagueSettingRequest request)
            => PostOddstableLeagueSorting<ImportClientOddstableLeagueSettingRequest, ImportClientOddstableLeagueSettingResponse>("sportsbook/client/oddstableleaguesettings/import", request);
        public Task<BaseResponse<ActivityLogResponse>> GetOddstableLeagueSortingLogs(OddstableLeagueSortingLogRequest request)
            => PostOddstableLeagueSorting<OddstableLeagueSortingLogRequest, ActivityLogResponse>("sportsbook/client/oddstableleaguesettings/logs", request);
        private async Task<BaseResponse<TResponse>> PostOddstableLeagueSorting<TRequest, TResponse>(string path, TRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync(path, new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
                    return JsonSerializer.Deserialize<BaseResponse<TResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);

                return new BaseResponse<TResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);

                return new BaseResponse<TResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<TierLeagueOrderModel>>> GetTierLeagueOrder()
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/tier/leagueorder/list", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<TierLeagueOrderModel>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<TierLeagueOrderModel>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<List<TierLeagueOrderModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<TierLeagueOrderModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"sportsbook/client/sportsettings", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientSportSettingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientSportSettingResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<ClientSportSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/trading/client/sportorder/audit/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderAuditResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientSportOrderAuditResponse>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientSportOrderAuditResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Lookup - Sports/Segment/League

        public async Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string langCode)
        {
            try
            {
                //return new BaseResponse<SportsLookupResponse>()
                //{
                //    Result = new SportsLookupResponse
                //    {
                //        SportLookup = new List<Backoffice.Shared.Models.Response.SportLookup>
                //        {
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 17,
                //                SportName = "American Football",
                //                //SortOrder = 1
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 271554,
                //                SportName = "Badminton",
                //                //SortOrder = 2
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 3,
                //                SportName = "Baseball",
                //                //SortOrder = 3
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 4,
                //                SportName = "Basketball",
                //                //SortOrder = 4
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 7950337,
                //                SportName = "Beach Volleyball",
                //                //SortOrder = 5
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 5,
                //                SportName = "Boxing",
                //                //SortOrder = 6
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 6,
                //                SportName = "Cricket",
                //                //SortOrder = 7
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 8,
                //                SportName = "Darts",
                //                //SortOrder = 8
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 10915624,
                //                SportName = "eSports",
                //                //SortOrder = 9
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 10,
                //                SportName = "Football",
                //                //SortOrder = 10
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 491393,
                //                SportName = "Futsal",
                //                //SortOrder = 11
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 99614,
                //                SportName = "Handball",
                //                //SortOrder = 12
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 15,
                //                SportName = "Ice Hockey",
                //                //SortOrder = 13
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 300115,
                //                SportName = "Martial Arts/UFC",
                //                //SortOrder = 14
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 73744,
                //                SportName = "Rugby Union",
                //                //SortOrder = 15
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 437727,
                //                SportName = "Squash",
                //                //SortOrder = 16
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 269467,
                //                SportName = "Table Tennis",
                //                //SortOrder = 17
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 24,
                //                SportName = "Tennis",
                //                //SortOrder = 18
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 11205870,
                //                SportName = "Virtual Cricket",
                //                //SortOrder = 19
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 91189,
                //                SportName = "Volleyball",
                //                //SortOrder = 20
                //            },
                //            new Backoffice.Shared.Models.Response.SportLookup{
                //                SportId = 276032,
                //                SportName = "Water Polo",
                //                //SortOrder = 21
                //            }
                //        }
                //    }
                //};

                var response = await _httpClient.GetAsync($"sportsbook/client/sports/lookup");

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<SportsLookupResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<SportsLookupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<SportsLookupResponse>();
                    }
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, LanguageCode langCode = LanguageCode.EN)
        {
            try
            {
                var response = await _httpClient.GetAsync($"sportsbook/client/segment/lookup/{sportId}");

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<SegmentLookupResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<SegmentLookupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<SegmentLookupResponse>();
                    }
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<SegmentLookupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, LanguageCode langCode = LanguageCode.EN)
        {
            try
            {
                var response = await _httpClient.GetAsync($"sportsbook/client/league/lookup/{sportId}/{segmentId}");

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<LeagueLookupResponse>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<LeagueLookupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<LeagueLookupResponse>();
                    }
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<LeagueLookupResponse>
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

            ////Console.WriteLine("- {0} was disposed!", this.GetType().Name);
        }
    }
}
