using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.ExternalApis.Maintenance;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private readonly IPlatformRepository _platformRepository;

        private HttpClient _maintenanceHttpClient;
        private readonly bool _isStaging;
        private readonly ActivitySource _activitySource;
        private readonly ILogger<MaintenanceRepository> _logger;

        private readonly List<string>? _maintenanceApps;


        public MaintenanceRepository(HttpClient httpClient, IConfiguration configuration, IPlatformRepository platformRepository,
                Instrumentation instrumentation, ILogger<MaintenanceRepository> logger)
        {
            _isStaging = bool.Parse(configuration["IsStaging"]);

            _platformRepository = platformRepository;
            _activitySource = instrumentation.ActivitySource;
            _logger = logger;

            _maintenanceApps = configuration["Maintenance:Apps"]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.Select(s => s.Trim().ToUpper()).ToList() ?? null;
            _maintenanceHttpClient = httpClient;
        }

        #region Methods
        public async Task<BaseResponse<MaintenanceResponse>> GetMaintenanceV2(List<string> clientCodes)
        {
            using (var activity = _activitySource.StartActivity("get.maintenance.v2"))
            {
                activity?.SetTag("client.codes", clientCodes != null ? string.Join(",", clientCodes) : null);
                try
                {
                    _logger.LogInformation($"[GetMaintenanceV2] clientCodes={(clientCodes != null ? string.Join(",", clientCodes) : "NULL")}");

                    var clientResponse = await _platformRepository.GetClients("HK");

                    _logger.LogInformation($"[GetMaintenanceV2] GetClients — StatusCode={clientResponse?.StatusCode}, Count={clientResponse?.Result?.Count ?? -1}");

                    if (clientResponse?.Result == null)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, clientResponse.StatusCode.ToString());
                        return new BaseResponse<MaintenanceResponse>()
                        {
                            StatusCode = clientResponse.StatusCode,
                            Message = clientResponse.Message
                        };
                    }

                    var clientReference = clientResponse.Result;

                    if (clientCodes?.Count > 1)
                        clientReference = clientReference.Where(c => clientCodes.Contains(c.ClientCode, StringComparer.OrdinalIgnoreCase)).ToList();

                    _logger.LogInformation($"[GetMaintenanceV2] clientReference count after filter={clientReference?.Count ?? -1}");

                    if (clientReference == null)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, clientResponse.StatusCode.ToString());
                        return new BaseResponse<MaintenanceResponse>()
                        {
                            StatusCode = clientResponse.StatusCode,
                            Message = $"Empty client reference : {(clientCodes != null ? string.Join(",", clientCodes) : "No Clients")}"
                        };
                    }

                    var clientsConfig = new List<MaintenanceConfigModel>();

                    foreach (var app in _maintenanceApps)
                    {
                        _logger.LogInformation($"[GetMaintenanceV2] Processing app={app}");

                        var configs = new List<ExternalMaintenanceConfig>();

                        try
                        {
                            configs = await FetchLayoutConfigsAsync(app, activity) ?? new List<ExternalMaintenanceConfig>();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"[GetMaintenanceV2] webprops error app={app}: {e.Message}");
                        }

                        _logger.LogInformation($"[GetMaintenanceV2] app={app} configs count={configs.Count}");

                        // The exposed ClientId is always the platform client Guid: it is the
                        // identifier the sportsbook frontends pass to webprops, so flags must
                        // live in {LAYOUT}/{GUID}.json to take effect. Legacy code-keyed objects
                        // are still read (matched by ClientCode) but a Guid-keyed object for the
                        // same client wins, and SetMaintenanceV2 only ever writes Guid-keyed ones.
                        var matched = new Dictionary<Guid, (ExternalMaintenanceConfig Config, bool IsGuidKeyed)>();

                        foreach (var config in configs)
                        {
                            var reference = clientReference.FirstOrDefault(c => c.ClientId.ToString().Equals(config.ClientId, StringComparison.OrdinalIgnoreCase)
                                                    || (c.ClientCode?.Equals(config.ClientId, StringComparison.OrdinalIgnoreCase) ?? false));

                            if (reference == null)
                                continue;

                            var isGuidKeyed = reference.ClientId.ToString().Equals(config.ClientId, StringComparison.OrdinalIgnoreCase);

                            if (!matched.TryGetValue(reference.ClientId, out var existing) || (isGuidKeyed && !existing.IsGuidKeyed))
                                matched[reference.ClientId] = (config, isGuidKeyed);
                        }

                        foreach (var client in clientReference)
                        {
                            var hasMatch = matched.TryGetValue(client.ClientId, out var entry);

                            // Clients with no config object under this layout yet are still listed
                            // (IsMaintenance=false) so operators can toggle them; SetMaintenanceV2
                            // creates the missing object on demand.
                            clientsConfig.Add(new MaintenanceConfigModel()
                            {
                                ClientId = client.ClientId.ToString().ToUpper(),
                                IsMaintenance = hasMatch && entry.Config.IsMaintenance,
                                Site = hasMatch ? entry.Config.Site : string.Empty,
                                ClientCode = client.ClientCode,
                                ClientName = client.ClientName,
                                App = app
                            });
                        }
                    }

                    _logger.LogInformation($"[GetMaintenanceV2] final clientsConfig count={clientsConfig.Count}");
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return new BaseResponse<MaintenanceResponse>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MaintenanceResponse()
                        {
                            ClientConfig = clientsConfig
                        }
                    };
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError($"error on method: {ex.Message}");
                    return new BaseResponse<MaintenanceResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = ex.Message.ToString()
                    };
                }
            }
        }

        public async Task<BaseResponse<MaintenanceResponse>> SetMaintenanceV2(SetMaintenanceRequest request, List<string> clientCodes)
        {
            using (var activity = _activitySource.StartActivity("set.maintenance.v2"))
            {
                activity?.SetTag("client.codes", clientCodes != null ? string.Join(",", clientCodes) : null);
                try
                {
                    var requestClientIds = request?.ClientIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToList() ?? new List<string>();

                    var clients = await GetMaintenanceV2(clientCodes);

                    if (clients.StatusCode != StatusCode.Success)
                        return clients;

                    var referenceClients = clients?.Result?.ClientConfig;
                    if (referenceClients == null)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, "referenceClients=null");
                        return new BaseResponse<MaintenanceResponse>()
                        {
                            StatusCode = StatusCode.Error,
                            Message = StatusCode.Error.ToString()
                        };
                    }

                    var appGroups = referenceClients.Where(c => (requestClientIds.Count() == 1 && requestClientIds.FirstOrDefault() == "ALL")
                                                    || (requestClientIds.Any(ci => (ci.Contains(":") && ci.Contains($"{c.App}:{c.ClientId}", StringComparison.OrdinalIgnoreCase)) || ci.Equals(c.ClientId, StringComparison.OrdinalIgnoreCase))))
                                                    .GroupBy(c => c.App).ToList();

                    foreach (var appGroup in appGroups)
                    {
                        var app = appGroup.Key;

                        try
                        {
                            // webprops setMaintenance fails on clients with no config object under
                            // the layout, so create missing objects first (idempotent: existing
                            // objects are left untouched). This also applies to "ALL": webprops'
                            // own "all" only flips clients whose objects already exist, so the
                            // explicit id list is always sent instead.
                            var existing = await FetchLayoutConfigsAsync(app, activity) ?? new List<ExternalMaintenanceConfig>();

                            foreach (var clientId in appGroup.Select(c => c.ClientId))
                            {
                                if (existing.Any(e => e.ClientId?.Equals(clientId, StringComparison.OrdinalIgnoreCase) ?? false))
                                    continue;

                                await CreateLayoutClientConfigAsync(app, clientId, activity);
                            }

                            var clientIds = string.Join(",", appGroup.Select(c => c.ClientId));

                            var stringPayload = JsonSerializer.Serialize(new
                            {
                                ClientIds = clientIds,
                                request.IsMaintenance
                            });

                            using (var maintenanceRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/maintenance")
                            {
                                Content = new StringContent(stringPayload, Encoding.UTF8, "application/json")
                            })
                            {
                                maintenanceRequest.Headers.Add("x-layout", app);

                                var result = await _maintenanceHttpClient.SendAsync(maintenanceRequest);

                                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                                {
                                    var content = await result.Content.ReadAsStringAsync();
                                    _logger.LogError($"[SetMaintenanceV2] webprops set failed app={app} status={result.StatusCode} content={content}");
                                    activity?.SetTag("app", app);
                                    activity?.SetTag("status", result.StatusCode.ToString());
                                    activity?.SetTag("payload", stringPayload);
                                    activity?.SetTag("content", content);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"[SetMaintenanceV2] webprops error app={app}: {e.Message}");
                        }
                    }

                    clients = await GetMaintenanceV2(clientCodes);

                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return new BaseResponse<MaintenanceResponse>()
                    {
                        StatusCode = clients.StatusCode,
                        Message = clients.Message,
                        Result = clients.Result
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"error on method: {ex.Message}");
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    return new BaseResponse<MaintenanceResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = ex.Message.ToString()
                    };
                }
            } // using
        }
        #endregion

        #region Utilities
        private async Task<List<ExternalMaintenanceConfig>> FetchLayoutConfigsAsync(string app, System.Diagnostics.Activity activity)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/maintenance"))
            {
                request.Headers.Add("x-layout", app);

                var response = await _maintenanceHttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"[FetchLayoutConfigs] app={app} status={response.StatusCode} content={content}");
                    activity?.SetTag("app", app);
                    activity?.SetTag("status", response.StatusCode.ToString());
                    activity?.SetTag("events", content);
                    return null;
                }

                // webprops-api returns a bare JSON array: [{"clientId":...,"isMaintenance":...,"site":...}]
                return JsonSerializer.Deserialize<List<ExternalMaintenanceConfig>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            }
        }

        private async Task CreateLayoutClientConfigAsync(string app, string clientId, System.Diagnostics.Activity activity)
        {
            var stringPayload = JsonSerializer.Serialize(new
            {
                clientId = clientId,
                site = string.Empty,
                isMaintenance = false
            });

            using (var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/config/{clientId}")
            {
                Content = new StringContent(stringPayload, Encoding.UTF8, "application/json")
            })
            {
                request.Headers.Add("x-layout", app);

                var response = await _maintenanceHttpClient.SendAsync(request);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[CreateLayoutClientConfig] app={app} clientId={clientId} status={response.StatusCode} content={content}");
                    activity?.SetTag("create.app", app);
                    activity?.SetTag("create.clientId", clientId);
                    activity?.SetTag("create.status", response.StatusCode.ToString());
                }
                else
                {
                    _logger.LogInformation($"[CreateLayoutClientConfig] created config for app={app} clientId={clientId}");
                }
            }
        }
        #endregion
    }
}
