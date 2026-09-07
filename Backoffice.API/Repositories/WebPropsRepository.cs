using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Shared.OutgoingEngineMessages;
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
        private readonly IRabbitMqRepository _rabbitMqRepository;

        public WebPropsRepository(HttpClient httpClient, ILogger<WebPropsRepository> logger, IRabbitMqRepository rabbitMqRepository)
        {
            _webPropsHttpClient = httpClient;
            _logger = logger;
            _rabbitMqRepository = rabbitMqRepository;
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

                var result = System.Text.Json.JsonSerializer.Deserialize<ClientFeatureFlagsResponse>(content, JsonSerializerOptionsProvider.CaseInsensitive);

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
                // current document is fetched and only the fields the caller edits are overwritten
                // in place (via JObject, not the typed model) so any fields not modeled here -
                // and any modeled field the caller left null - are never clobbered.
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
                features["FullCashout"] = request.FullCashout;
                // Full-before-partial dependency (GM-790): partial cashout cannot be enabled
                // without full cashout. The UI already prevents the combination; coercing here
                // keeps a direct API call from storing an inconsistent config.
                features["PartialCashout"] = request.FullCashout && request.PartialCashout;
                features["BetRecommendation"] = request.BetRecommendation;
                features["IsEuroLayout"] = request.IsEuroLayout;

                // Null means the caller didn't edit these - leave the stored values untouched.
                if (request.GameTrackerProductNames != null)
                    features["GameTrackerProductNames"] = JArray.FromObject(request.GameTrackerProductNames);

                configNode["Features"] = features;

                if (request.LayoutThemes != null)
                {
                    var layout = configNode["Layout"] as JObject ?? new JObject();
                    foreach (var layoutThemes in request.LayoutThemes)
                    {
                        var layoutNode = layout[layoutThemes.Key] as JObject ?? new JObject();
                        layoutNode["Themes"] = JArray.FromObject(layoutThemes.Value ?? new System.Collections.Generic.List<string>());
                        layout[layoutThemes.Key] = layoutNode;
                    }
                    configNode["Layout"] = layout;
                }

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

                // Signal-only, deliberately no flag values in the payload: a burst of rapid saves
                // (e.g. several admins toggling the same client back and forth) would otherwise
                // queue up through RabbitMQ -> OE.Websocket.Service -> the hub and replay every
                // intermediate value to platform-web, causing the UI to visibly flicker through
                // stale states. Each push instead tells platform-web "go re-fetch and compare",
                // so only the current, converged value from webprops-api is ever applied.
                //
                // Routing key carries the client code as its last segment, matching the working
                // precedent for single-client-targeted pushes (OnVisitFavorites/OnVisitBoostedFixtures
                // in platform-final/Platform.Sportsbook.API/Repository/SportsbookRepository.cs) -
                // OE.Websocket.Service's ClientRegistry.ResolveByRoutingKey() resolves the client
                // from the routing key's last dot-segment.
                await _rabbitMqRepository.Publish(new MessageRequest
                {
                    Payload = System.Text.Json.JsonSerializer.Serialize(new OnWebPropsChangeMessage
                    {
                        ClientCode = request.ClientCode
                    }),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnWebPropsChange",
                    RoutingKey = $"r.oe.cachesync.onwebpropschange.{request.ClientCode.ToLower()}"
                });

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
