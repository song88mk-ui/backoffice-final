using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
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
                    if (IsClientConfigNotFound(response, content))
                    {
                        return new BaseResponse<ClientFeatureFlagsResponse>()
                        {
                            StatusCode = StatusCode.NotFound,
                            Message = content
                        };
                    }

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
                else if (IsClientConfigNotFound(getResponse, getContent))
                {
                    // Confirmed absence (webprops-api's specific "no properties for client" 404),
                    // not just any failed GET - safe to create a fresh config. Any other failure
                    // (5xx, network error, a differently-shaped error body) still hard-fails below,
                    // since we can't tell those apart from "client has an existing config we
                    // couldn't read" and a full-replace POST would silently wipe it.
                    _logger.LogWarning($"[UpdateClientFeatureFlagsAsync] no existing config for clientCode={request.ClientCode}, creating new");
                    configNode = new JObject
                    {
                        ["ClientCode"] = request.ClientCode,
                        ["Features"] = new JObject()
                    };
                }
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
                features["BetRecommendation"] = request.BetRecommendation;
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

        #region Utilities
        // Narrowly matches webprops-api's specific "client has no config yet" response
        // (get_config.go's only 404 path), rather than treating any non-2xx as absence -
        // a 5xx/network failure must never be silently treated as "safe to create".
        private static bool IsClientConfigNotFound(System.Net.Http.HttpResponseMessage response, string content)
        {
            if (response.StatusCode != HttpStatusCode.NotFound)
                return false;

            try
            {
                var error = JObject.Parse(content);
                var message = error["message"]?.ToString() ?? string.Empty;
                return message.Contains("no properties for client", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
