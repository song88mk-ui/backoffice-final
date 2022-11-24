using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Response.ClientTopFixtureResponse;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backoffice.API.Repositories
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;
        public PlatformRepository(IConfiguration configuration)
        {
            _configuration = configuration;
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
        public async Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds)
        {
            try
            {
                var topFixture = new List<OrderedClientTopFixture>();
                foreach (var region in regionclientIds)
                {
                    var response = await GetHttpClient(region.Key).GetAsync($"v1/client/topfixtures?clientRefIds={region.Value}");

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var newresult = JsonConvert.DeserializeObject<BaseResponse<ClientTopFixtureResponse>>(response.Content.ReadAsStringAsync().Result).Result.TopFixtures;
                        if (newresult.Any())
                        {
                            topFixture.AddRange(newresult);
                        }
                    }
                }

                if (topFixture.Any())
                {
                    return new BaseResponse<ClientTopFixtureResponse>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ClientTopFixtureResponse
                        {
                            TopFixtures = topFixture
                        }
                    };
                }
                else
                {
                    return new BaseResponse<ClientTopFixtureResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientTopFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<SportsTreeResponse>> GetSportstreeDetails(int clientId, string region)
        {
            try
            {
                var response = await GetHttpClient(region).GetAsync($"v1/client/topfixtures/sportstree?clientRefId={clientId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<SportsTreeResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
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
                return new BaseResponse<SportsTreeResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

    }
}
