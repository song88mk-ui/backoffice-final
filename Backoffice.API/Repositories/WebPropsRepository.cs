using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class WebPropsRepository : IWebPropsRepository
    {
        private readonly HttpClient _webPropsHttpClient;
        private readonly ILogger<WebPropsRepository> _logger;

        public WebPropsRepository(HttpClient httpClient, ILogger<WebPropsRepository> logger)
        {
            _webPropsHttpClient = httpClient;
            _logger = logger;
        }

        #region Methods
        public async Task<BaseResponse<ClientFeatureFlagsResponse>> GetClientFeatureFlagsAsync(string clientCode)
        {
            try
            {
                var response = await _webPropsHttpClient.GetAsync($"/api/v1/config/{clientCode}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[GetClientFeatureFlagsAsync] clientCode={clientCode} status={response.StatusCode} content={content}");
                    return new BaseResponse<ClientFeatureFlagsResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = content
                    };
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<ClientFeatureFlagsResponse>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return new BaseResponse<ClientFeatureFlagsResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GetClientFeatureFlagsAsync] error clientCode={clientCode}: {ex.Message}");
                return new BaseResponse<ClientFeatureFlagsResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<BaseResponse<ClientFeatureFlagsResponse>> UpdateClientFeatureFlagsAsync(UpdateClientFeatureFlagsRequest request)
        {
            try
            {
                // webprops-api's POST /config/{clientId} is a full replace, not a merge, so the
                // current document is fetched and only the Features the UI edits are overwritten
                // in place (via JObject, not the typed model) so Layout/Themes and any fields not
                // modeled here are never clobbered.
                var getResponse = await _webPropsHttpClient.GetAsync($"/api/v1/config/{request.ClientCode}");
                var getContent = await getResponse.Content.ReadAsStringAsync();

                JObject configNode;
                if (getResponse.IsSuccessStatusCode)
                    configNode = JObject.Parse(getContent);
                else
                {
                    _logger.LogError($"[UpdateClientFeatureFlagsAsync] fetch before update failed clientCode={request.ClientCode} status={getResponse.StatusCode} content={getContent}");
                    return new BaseResponse<ClientFeatureFlagsResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = getContent
                    };
                }

                var features = configNode["Features"] as JObject ?? new JObject();
                features["BettingRules"] = request.BettingRules;
                features["DarkMode"] = request.DarkMode;
                features["LanguageOptions"] = request.LanguageOptions;
                features["OGAuth"] = request.OGAuth;
                features["PartialCashout"] = request.PartialCashout;
                configNode["Features"] = features;

                var payload = JsonConvert.SerializeObject(configNode);
                var postResponse = await _webPropsHttpClient.PostAsync($"/api/v1/config/{request.ClientCode}", new StringContent(payload, Encoding.UTF8, "application/json"));
                var postContent = await postResponse.Content.ReadAsStringAsync();

                if (!postResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"[UpdateClientFeatureFlagsAsync] update failed clientCode={request.ClientCode} status={postResponse.StatusCode} content={postContent}");
                    return new BaseResponse<ClientFeatureFlagsResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = postContent
                    };
                }

                return await GetClientFeatureFlagsAsync(request.ClientCode);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UpdateClientFeatureFlagsAsync] error clientCode={request.ClientCode}: {ex.Message}");
                return new BaseResponse<ClientFeatureFlagsResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }
        #endregion
    }
}
