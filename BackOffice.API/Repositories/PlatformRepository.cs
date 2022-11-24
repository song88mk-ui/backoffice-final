using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;
        public PlatformRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<BaseResponse<ClientTopFixtureMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"v1/client/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureMessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientTopFixtureMessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientTopFixtureMessageResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientTopFixtureMessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #region Methods
        private HttpClient GetHttpClient(string region)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(GetBaseUrl(region))
            };
            client.DefaultRequestHeaders.Add("x-api-key", GetApikey(region));

            return client;
        }
        private string GetBaseUrl(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:PlatformApi:SA:Uri"];
            else
                return _configuration[$"API:PlatformApi:HK:Uri"];
        }
        private string GetApikey(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:PlatformApi:SA:ApiKey"];
            else
                return _configuration[$"API:PlatformApi:HK:ApiKey"];
        }
        #endregion
    }
}
