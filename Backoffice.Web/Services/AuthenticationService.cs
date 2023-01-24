using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.ActionFilter;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backoffice.Web.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthenticationService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<BaseResponse<Tuple<bool, List<string>>>> Login(LogInModel user)
        {
            var policies = new List<string>();
            var result = new BaseResponse<Tuple<bool, List<string>>>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString(),
                    Result = new Tuple<bool, List<string>>(false, policies)
                };
            user.Provider = "BoWeb";
            var bodyContent = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers.UserAgent;

            _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent.ToString());
            var response = await _httpClient.PostAsync("api/authentication/login", bodyContent);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var authPayload = await response.Content.ReadAsStringAsync();
                var token = JsonSerializer.Deserialize<BaseResponse<TokenModel>>(authPayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                _httpContextAccessor.HttpContext?.Session.SetString("UserToken", token.Result.AccessToken);
                ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(user.Name);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token.Result.AccessToken);
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(token.Result.AccessToken), "jwtAuthType"));

                policies = claimsPrincipal.Claims.Where(x => x.Value != "0").Select(x => x.Type.Split('.').FirstOrDefault()).Distinct().ToList();

                result = new BaseResponse<Tuple<bool, List<string>>>
                {
                    StatusCode = token.StatusCode,
                    Message = token.Message,
                    Result = new Tuple<bool, List<string>>(true, policies)
                };
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var authPayload = await response.Content.ReadAsStringAsync();
                var token = JsonSerializer.Deserialize<BaseResponse<TokenModel>>(authPayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                result = new BaseResponse<Tuple<bool, List<string>>>
                {
                    StatusCode = token.StatusCode,
                    Message = token.Message,
                    Result = new Tuple<bool, List<string>>(false, new List<string>())
                };
            }

            return result;
        }

        public async Task<TokenModel> AuthenticateToken()
        {
            var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            var response = await _httpClient.GetAsync("api/authentication/authenticate");
            var token = new TokenModel();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<TokenModel>>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                token = new TokenModel()
                {
                    AccessToken = result.Result.AccessToken
                };

                _httpContextAccessor.HttpContext?.Session.SetString("UserToken", token.AccessToken ?? "");

                var authState = await ((AuthStateProvider)_authStateProvider).GetAuthenticationStateAsync();
                var userName = authState.User.Claims.ToList().Where(x => x.Type == "TokenName").Select(x => x.Value).FirstOrDefault() ?? "";

                ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(userName);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            }

            return token;
        }
        public async Task<bool> Logout()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("UserToken");
            ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
            _httpClient.DefaultRequestHeaders.Authorization = null;

            await _httpClient.GetAsync("/logout");

            return true;
        }

        public async Task<BaseResponse<bool>> Register(UserRegisterRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/register", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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
                return new BaseResponse<UserListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<LogInActivityLogResponse>> GetLogInActivityLogList(string userId, MetaData metaData)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsJsonAsync($"api/authentication/user/list?userId={userId}", new StringContent(JsonSerializer.Serialize(metaData), Encoding.UTF8, "application/json"));

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
                return new BaseResponse<UserListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<PermissionResponse>> GetPermissionList(bool isSubUser)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.GetAsync($"api/authentication/permission/list?isSubUser={isSubUser}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PermissionResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<PermissionResponse>();
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
                return new BaseResponse<UserListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        
        public async Task<BaseResponse<bool>> AddOrUpdateUserGroup(UserGroupRequest request)
        {
            try
            {
                var userToken = _httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsync($"api/authentication/usergroup/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion
    }
}