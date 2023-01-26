using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class RabbitMqRepository : IRabbitMqRepository
    {
        private readonly IConfiguration _configuration;
        public RabbitMqRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<BaseResponse<MessageResponse>> Publish(MessageRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/broker/publish", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    return new BaseResponse<MessageResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        Result = new MessageResponse
                        {
                            IsSuccess = false
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MessageResponse
                    {
                        IsSuccess = false
                    }
                };
            }
        }

        #region Utilities
        private HttpClient GetHttpClient(string region)
        {
            var uri = GetBaseUrl(region);
            if (uri.LastOrDefault() != '/')
                uri += "/";

            var client = new HttpClient()
            {
                BaseAddress = new Uri($"{uri}")
            };

            return client;
        }
        private string GetBaseUrl(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:RabbitMqApi:SA:Uri"];
            else
                return _configuration[$"API:RabbitMqApi:HK:Uri"];
        }
        #endregion
    }
}
