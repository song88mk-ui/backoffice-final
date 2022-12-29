using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Pages.OddsEngine;
using System.Net;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class TradingService : ITradingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public TradingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            //var userToken = httpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty;

            _httpClient.BaseAddress = new Uri(_configuration["API:Backoffice:Uri"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["API:Backoffice:Security:APIKey"]);
        }

        public async Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/trading/priceboost/settings/list", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<BoostedSettingsModel>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
                var query = !(string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId)) ? "?userId=" + userId : "";
                var response = await _httpClient.PostAsync($"api/trading/priceboost/settings{query}", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}
