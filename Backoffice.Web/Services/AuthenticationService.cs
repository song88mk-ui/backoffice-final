using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Telemetry;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backoffice.Web.Services
{
    public class AuthenticationService : IAuthenticationService, IDisposable
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        private readonly Histogram<double> _authLoginCallDurationHistogram;

        public AuthenticationService(HttpClient httpClient, AuthenticationStateProvider authStateProvider,
            ILocalStorageService localStorage, IHttpContextAccessor httpContextAccessor, Instrumentation instrumentation)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;

            this._authLoginCallDurationHistogram = instrumentation.AuthLoginCallDurationHistogram;
        }

        public async Task<BaseResponse<Tuple<bool, List<string>, TokenModel>>> Login(LogInModel user)
        {
            var policies = new List<string>();
            var result = new BaseResponse<Tuple<bool, List<string>, TokenModel>>
            {
                StatusCode = StatusCode.Error,
                Message = StatusCode.Error.ToString(),
                Result = new Tuple<bool, List<string>, TokenModel>(false, policies, null)
            };
            var userAgent = _httpContextAccessor?.HttpContext?.Request.Headers.UserAgent;
            var ip = _httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Trim() ?? "";

            if (string.IsNullOrEmpty(ip))
            {
                ip = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "";
            }
            ip = ip.Split(':')?.FirstOrDefault()?.Split(",").FirstOrDefault() ?? "";

            user.IpAddress = ip.Trim();
            user.Country = _httpContextAccessor?.HttpContext?.Request.Headers["CF-IPCountry"].FirstOrDefault() ?? "";
            user.UserAgent = userAgent;

            Stopwatch? duration = _authLoginCallDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

            var bodyContent = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/authentication/login", bodyContent);

            if (duration != null)
                _authLoginCallDurationHistogram.Record(duration.Elapsed.TotalMilliseconds);


            if (response.StatusCode == HttpStatusCode.OK)
            {
                var authPayload = await response.Content.ReadAsStringAsync();
                var token = JsonSerializer.Deserialize<BaseResponse<TokenModel>>(authPayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                _httpContextAccessor.HttpContext?.Session.SetString("UserToken", token.Result.AccessToken);
                _httpContextAccessor.HttpContext?.Session.SetString("OperatorToken", token.Result.OperatorToken);
                ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(user.Username);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token.Result.AccessToken);
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(token.Result.AccessToken), "jwtAuthType"));

                policies = claimsPrincipal.Claims.Where(x => x.Value != "0").Select(x => x.Type.Split('.').FirstOrDefault()).Distinct().ToList();

                result = new BaseResponse<Tuple<bool, List<string>, TokenModel>>
                {
                    StatusCode = token.StatusCode,
                    Message = token.Message,
                    Result = new Tuple<bool, List<string>, TokenModel>(true, policies, token.Result)
                };
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var authPayload = await response.Content.ReadAsStringAsync();
                var token = new BaseResponse<TokenModel>();

                if (!string.IsNullOrWhiteSpace(authPayload))
                {
                    token = JsonSerializer.Deserialize<BaseResponse<TokenModel>>(authPayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                }
                result = new BaseResponse<Tuple<bool, List<string>, TokenModel>>
                {
                    StatusCode = token.StatusCode,
                    Message = token.Message,
                    Result = new Tuple<bool, List<string>, TokenModel>(false, new List<string>(), null)
                };
            }

            return result;
        }

        public async Task<BaseResponse<TokenModel>> AuthenticateToken(string accesstoken = "")
        {
            var userToken = accesstoken;
            if(string.IsNullOrEmpty(accesstoken))
            {
                userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            var response = await _httpClient.GetAsync("api/authentication/authenticate");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<TokenModel>>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                var token = new TokenModel()
                {
                    AccessToken = result.Result.AccessToken
                };

                _httpContextAccessor.HttpContext?.Session.SetString("UserToken", token.AccessToken ?? "");

                var authState = await ((AuthStateProvider)_authStateProvider).GetAuthenticationStateAsync();
                var userName = authState.User.Claims.ToList().Where(x => x.Type == "TokenName").Select(x => x.Value).FirstOrDefault() ?? "";

                ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(userName);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                return result;
            }

            return new BaseResponse<TokenModel>
            {
                StatusCode = response.StatusCode == HttpStatusCode.Unauthorized ? StatusCode.Unauthorized : StatusCode.Error,
                Result = new TokenModel()
            };
        }
        public async Task<bool> Logout()
        {
            var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            var auth = _httpClient.GetAsync("api/authentication/logout").Result;

            _httpContextAccessor.HttpContext?.Session.Remove("UserToken");
            ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
            _httpClient.DefaultRequestHeaders.Authorization = null;

            return true;
        }

        public async Task<BaseResponse<long>> Register(UserRegisterRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/register", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<long>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<long>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<long>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<long>();
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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<long>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> UpdateUser(UserRegisterRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/user/modify", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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

        public async Task<BaseResponse<bool>> UpdateUserPassword(string userId, UserRegisterRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/user/modify/{userId}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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

        public async Task<BaseResponse<UserListResponse>> GetUserList(UserGroupRequest request, bool isShowSuperAdmin = false)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var query = $"?isShowSuperAdmin={isShowSuperAdmin}";
                var response = await _httpClient.PostAsync($"api/authentication/user/list{query}", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UserListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<UserListResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
                        return JsonSerializer.Deserialize<BaseResponse<UserListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<UserListResponse>();
                    else
                    {
                        return new BaseResponse<UserListResponse>
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

                return new BaseResponse<UserListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<LogInActivityLogResponse>> GetLogInActivityLogList(string userId, MetaData metaData, string searchText = "")
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var query = !string.IsNullOrWhiteSpace(searchText) ? $"?search={searchText}" : "";

                var response = await _httpClient.PostAsync($"api/authentication/activitylog/list/{userId}{query}", new StringContent(JsonSerializer.Serialize(metaData), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<LogInActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<LogInActivityLogResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
                        return JsonSerializer.Deserialize<BaseResponse<LogInActivityLogResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<LogInActivityLogResponse>();
                    else
                    {
                        return new BaseResponse<LogInActivityLogResponse>
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

                return new BaseResponse<LogInActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }


        #region Methods - SubUsers
        public async Task<BaseResponse<UserListResponse>> GetSubAccounts(SubAccountListRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/subaccount/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UserListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<UserListResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UserListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<UserListResponse>();
                    else
                    {
                        return new BaseResponse<UserListResponse>
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

                return new BaseResponse<UserListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<PermissionResponse>> GetPermissionList(bool isSubUser, bool limitVersion)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.GetAsync($"api/authentication/permission/list?isSubUser={isSubUser}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BaseResponse<PermissionResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<PermissionResponse>();

                    if (result.Result != null && limitVersion == true)
                        if (result.Result.Permissions != null)
                            result.Result.Permissions = result.Result.Permissions.Where(x => x.Version != 1).ToList();

                    return result;

                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PermissionResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<PermissionResponse>();
                    else
                    {
                        return new BaseResponse<PermissionResponse>
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

                return new BaseResponse<PermissionResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region User Group
        public async Task<BaseResponse<UserGroupListResponse>> GetUserGroupList(UserGroupListRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/usergroup/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UserGroupListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<UserGroupListResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UserGroupListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<UserGroupListResponse>();
                    else
                    {
                        return new BaseResponse<UserGroupListResponse>
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

                return new BaseResponse<UserGroupListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<UserListResponse>> GetUserGroupUsers(UserGroupRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/usergroup/user/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UserListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<UserListResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UserListResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<UserListResponse>();
                    else
                    {
                        return new BaseResponse<UserListResponse>
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

                return new BaseResponse<UserListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<UserGroupResponse>> AddOrUpdateUserGroup(UserGroupRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/usergroup/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<UserGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<UserGroupResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<UserGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<UserGroupResponse>();
                    else
                    {
                        return new BaseResponse<UserGroupResponse>
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

                return new BaseResponse<UserGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> DeleteUserGroup(UserGroupRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/usergroup/delete", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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

        #region Report Roles
        public async Task<BaseResponse<ReportRolesUrlResponse>> GetReportRolesUrl(ReportRolesUrlRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/report/roles/url", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ReportRolesUrlResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<ReportRolesUrlResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ReportRolesUrlResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<ReportRolesUrlResponse>();
                    else
                    {
                        return new BaseResponse<ReportRolesUrlResponse>
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

                return new BaseResponse<ReportRolesUrlResponse>
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

            Console.WriteLine("- {0} was disposed!", this.GetType().Name);
        }
    }
}
