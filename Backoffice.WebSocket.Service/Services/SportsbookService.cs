using Backoffice.Shared.Enums;
using Backoffice.Shared.IncomingSbEngineMessages;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Backoffice.Shared.Utilities;
using Backoffice.WebSocket.Service.Repositories;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Services
{
    public class SportsbookService : ISportsbookService
    {

        private readonly HttpClient _platformHttpClient;
        private readonly HttpClient _sportsbookHttpClient;
        public SportsbookService(IConfiguration configuration)
        {
            _platformHttpClient = new HttpClient()
            {
                BaseAddress = new Uri(configuration["API:PlatformAPI:Uri"]),
                Timeout = TimeSpan.FromSeconds(5)
            };
            _platformHttpClient.DefaultRequestHeaders.Add("x-api-key", configuration["API:PlatformAPI:APIKey"]);
            
            _sportsbookHttpClient = new HttpClient()
            {
                BaseAddress = new Uri(configuration["API:Sportsbook:Uri"]),
                Timeout = TimeSpan.FromSeconds(5)
            };
        }
        public async Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps()
        {
            try
            {
                var response = await _platformHttpClient.GetAsync($"v1/sportsbook/client/markettemplateplayerprops");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<List<MarketTemplatePlayerPropsResponse>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<List<MarketTemplatePlayerPropsResponse>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
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
        public async Task<BaseResponse<NoMarginClientsResponse>> GetAsianclientCodes()
        {
            try
            {
                var response = await _sportsbookHttpClient.GetAsync("v1/sportsbook/client/asian");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<NoMarginClientsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<NoMarginClientsResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<NoMarginClientsResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<NoMarginClientsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}
