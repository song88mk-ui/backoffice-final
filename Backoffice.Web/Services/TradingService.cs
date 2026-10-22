using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Extensions;
using Backoffice.Web.Models;
using Backoffice.Web.States;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class TradingService : ITradingService, IDisposable
    {
        public string Region { get; set; } = "HK";

        private HttpClient _client;
        private readonly IAuthenticationService _authenticationService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserState _userState;

        public TradingService(HttpClient httpClient, IConfiguration configuration, IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor, UserState userState)
        {
            _client = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _authenticationService = authenticationService;
            _userState = userState;
        }

        private HttpClient GetHttpClient()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userState.Model.AccessToken);

            if (_client.DefaultRequestHeaders.Contains("x-region"))
            {
                _client.DefaultRequestHeaders.Remove("x-region");
            }
            _client.DefaultRequestHeaders.Add("x-region", Region);
            return _client;
        }

        public async Task<BaseResponse<List<ClientInformationModel>>> GetClientList(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await GetHttpClient().GetAsync($"api/trading/client", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponse<List<ClientInformationModel>>>(
                        await response.Content.ReadAsStringAsync(cancellationToken),
                        JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<List<ClientInformationModel>>();
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return JsonSerializer.Deserialize<BaseResponse<List<ClientInformationModel>>>
                        (
                            await response.Content.ReadAsStringAsync(cancellationToken),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<List<ClientInformationModel>>();
                    }
                    else
                    {
                        Log.Error($"[api/trading/client] - {_userState.Model.AccessToken} - {response.ReasonPhrase}");

                        return new BaseResponse<List<ClientInformationModel>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Client List - {ex.Message}");
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                return new BaseResponse<List<ClientInformationModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #region Priceboost
        public async Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/priceboost/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigModel>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<PriceBoostConfigModel>>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<List<PriceBoostConfigModel>>
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

                return new BaseResponse<List<PriceBoostConfigModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/priceboost/settings/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync(cancellationToken), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<List<BoostedSettingsModel>>
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

                return new BaseResponse<List<BoostedSettingsModel>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> AddOrUpdateBoostedSetting(BoostedSettingsModel request, string userId)
        {
            try
            {
                var query = !(string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId)) ? $"?userId={userId}" : "";
                var response = await GetHttpClient().PostAsync($"api/trading/priceboost/settings{query}", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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

        public async Task<BaseResponse<PriceBoostConfigResponse>> AddOrUpdatePriceBoost(Models.AddPriceBoostFilterModel request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/priceboost", new StringContent(JsonSerializer.Serialize(request.PriceBoostConfigList), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PriceBoostConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PriceBoostConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<PriceBoostConfigResponse>
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

                return new BaseResponse<PriceBoostConfigResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<PriceBoostConfigResponse>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/priceboost", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<PriceBoostConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<PriceBoostConfigResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<PriceBoostConfigResponse>
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

                return new BaseResponse<PriceBoostConfigResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        #region Admin
        public async Task<BaseResponse<MarketFixtureResponse>> GetSportsBookFixtures(MarketFixtureRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/sportsbook/fixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }
        public async Task<BaseResponse<MarketFixtureResponse>> GetSportsBookOutrightFixtures(MarketFixtureRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/sportsbook/outrightfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MarketFixtureResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }


        //public async Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(string clientCode, int sportId, int fixtureStatusId)
        //{
        //    try
        //    {
        //        var result = await JsonSerializer.DeserializeAsync<BaseResponse<ClientMarketTemplateGroupResponse>>
        //        (await GetHttpClient().GetStreamAsync($"api/trading/client/marketgroup/{clientCode}/{sportId}/{fixtureStatusId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //        if (ex.StackTrace != null)
        //            Log.Error(ex.StackTrace.ToString());

        //        return new BaseResponse<ClientMarketTemplateGroupResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}

        public async Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/bet/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
        public async Task<BaseResponse<string>> AddManualMessageSettlement(List<MessageSettlementRequest> request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/manual/messagesettlement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<string>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<string>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<string>
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

                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        //public async Task<BaseResponse<BetSelectionDetailsResponse>> GetBetSelectionDetails(BetSelectionDetailsRequest request)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient().PostAsync($"api/trading/bet/selections", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<BetSelectionDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<BetSelectionDetailsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<BetSelectionDetailsResponse>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //        if (ex.StackTrace != null)
        //            Log.Error(ex.StackTrace.ToString());

        //        return new BaseResponse<BetSelectionDetailsResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}

        public async Task<BaseResponse<ManualMarketSettlementDetailsResponse>> GetManualMarketSettlementDetails(ManualMarketSettlementDetailsRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/manual/marketsettlement/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementDetailsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementDetailsResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<ManualMarketSettlementDetailsResponse>
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

                return new BaseResponse<ManualMarketSettlementDetailsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ManualMarketSettlementResponse>> AddOrUpdateManualMarketSettlement(List<ManualMarketSettlementRequest> request, string userId)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/manual/marketsettlement?userid={userId}", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ManualMarketSettlementResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<ManualMarketSettlementResponse>
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

                return new BaseResponse<ManualMarketSettlementResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #region Configuration
        public async Task<BaseResponse<ConfigModel>> GetClientConfig(ClientConfigRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/client/config/all", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ConfigModel>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<ConfigModel>
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

                return new BaseResponse<ConfigModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups(int fixtureTypeId)
        {
            try
            {
                var response = await GetHttpClient().GetAsync($"api/trading/markettemplategroups/{fixtureTypeId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketTemplateGroupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<MarketTemplateGroupResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            return JsonSerializer.Deserialize<BaseResponse<MarketTemplateGroupResponse>>
                            (
                                await response.Content.ReadAsStringAsync(),
                                JsonSerializerOptionsProvider.CaseInsensitive
                            ) ?? new BaseResponse<MarketTemplateGroupResponse>();
                        }
                        else
                        {
                            return new BaseResponse<MarketTemplateGroupResponse>
                            {
                                StatusCode = StatusCode.Error,
                                Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                            };
                        }
                    else
                    {
                        return new BaseResponse<MarketTemplateGroupResponse>
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

                return new BaseResponse<MarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<MarketLookupResponse>> GetMarketLookup(MarketLookupRequest request)
        {
            try
            {
                var response = await GetHttpClient().GetAsync($"api/trading/marketlookup/{request.FixtureTypeId}/{request.SportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MarketLookupResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<MarketLookupResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            return JsonSerializer.Deserialize<BaseResponse<MarketLookupResponse>>
                            (
                                await response.Content.ReadAsStringAsync(),
                                JsonSerializerOptionsProvider.CaseInsensitive
                            ) ?? new BaseResponse<MarketLookupResponse>();
                        }
                        else
                        {
                            return new BaseResponse<MarketLookupResponse>
                            {
                                StatusCode = StatusCode.Error,
                                Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                            };
                        }
                    else
                    {
                        return new BaseResponse<MarketLookupResponse>
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

                return new BaseResponse<MarketLookupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientConfigResponse>> AddOrUpdateConfig(ConfigSettingsModel request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/client/config", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientConfigResponse>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<ClientConfigResponse>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            return JsonSerializer.Deserialize<BaseResponse<ClientConfigResponse>>
                            (
                                await response.Content.ReadAsStringAsync(),
                                JsonSerializerOptionsProvider.CaseInsensitive
                            ) ?? new BaseResponse<ClientConfigResponse>();
                        }
                        else
                        {
                            return new BaseResponse<ClientConfigResponse>
                            {
                                StatusCode = StatusCode.Error,
                                Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                            };
                        }
                    else
                    {
                        return new BaseResponse<ClientConfigResponse>
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

                return new BaseResponse<ClientConfigResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>> GetConfigLogs(ConfigLogsRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/client/config/audit", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>>
                        (
                            await response.Content.ReadAsStringAsync(),
                            JsonSerializerOptionsProvider.CaseInsensitive
                        ) ?? new BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            return JsonSerializer.Deserialize<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>>
                            (
                                await response.Content.ReadAsStringAsync(),
                                JsonSerializerOptionsProvider.CaseInsensitive
                            ) ?? new BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>();
                        }
                        else
                        {
                            return new BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>
                            {
                                StatusCode = StatusCode.Error,
                                Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                            };
                        }
                    else
                    {
                        return new BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>
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

                return new BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        public async Task<BaseResponse<MessageResponse>> Publish(MessageRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"api/trading/publish", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MessageResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MessageResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                    else
                    {
                        return new BaseResponse<MessageResponse>
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

                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public void Dispose()
        {
            _client.Dispose();
            _authenticationService.Dispose();

            //Console.WriteLine("- {0} was disposed!", this.GetType().Name);
        }
    }
}
