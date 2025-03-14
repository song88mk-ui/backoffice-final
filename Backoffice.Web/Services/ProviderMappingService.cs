using System.Net.Http.Headers;
using System.Text.Json;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.States;
using Serilog;
using System.Text;

namespace Backoffice.Web.Services;

public class ProviderMappingService : IProviderMappingService
{
    public string Region { get; set; } = "HK";
    private HttpClient _client;
    private readonly UserState _userState;

    public ProviderMappingService(HttpClient httpClient, UserState userState)
    {
        _client = httpClient;
        _userState = userState;
    }

    private HttpClient GetHttpClient()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _userState.Model.AccessToken);
        if (_client.DefaultRequestHeaders.Contains("x-region"))
        {
            _client.DefaultRequestHeaders.Remove("x-region");
        }

        _client.DefaultRequestHeaders.Add("x-region", Region);
        return _client;
    }

    public async Task<BaseResponse<UnmappedResponse>> GetUnmappedSports()
    {
        try
        {
            var response = await GetHttpClient().GetAsync($"/api/trading/unmapped/sports");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<BaseResponse<UnmappedResponse>>
                (
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
                ) ?? new BaseResponse<UnmappedResponse>();
            }
            else
            {
                return new BaseResponse<UnmappedResponse>();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            if (ex.StackTrace != null)
                Log.Error(ex.StackTrace.ToString());

            return new BaseResponse<UnmappedResponse>
            {
                StatusCode = StatusCode.Error,
                Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
            };
        }
    }

    public async Task<BaseResponse<SportMappingResponse>> GetSportMapping(MappingRequest request)
    {
        try
        {
            var response = await GetHttpClient().PostAsync("/api/trading/mapping/sports", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<BaseResponse<SportMappingResponse>>
                (
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
                ) ?? new BaseResponse<SportMappingResponse>();
            }
            else
            {
                return new BaseResponse<SportMappingResponse>();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            if (ex.StackTrace != null)
                Log.Error(ex.StackTrace.ToString());

            return new BaseResponse<SportMappingResponse>
            {
                StatusCode = StatusCode.Error,
                Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
            };
        }
    }
}