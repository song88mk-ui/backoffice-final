using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.ExternalApis.Maintenance;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly string _maintenanceConfigJson;
        private readonly string _maintenanceConfigJsonTest;


        public MaintenanceRepository(HttpClient httpClient, IConfiguration configuration, IPlatformRepository platformRepository,
                Instrumentation instrumentation, ILogger<MaintenanceRepository> logger)
        {
            _isStaging = bool.Parse(configuration["IsStaging"]);

            _platformRepository = platformRepository;
            _activitySource = instrumentation.ActivitySource;
            _logger = logger;

            _maintenanceConfigJson = configuration["Custom:MaintenanceConfigFile"];
            _maintenanceConfigJsonTest = configuration["Custom:MaintenanceConfigFileTest"];
            _maintenanceApps = configuration["Maintenance:Apps"]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.Select(s => s.Trim().ToUpper()).ToList() ?? null;
            _maintenanceHttpClient = httpClient;
        }

        #region Methods
        public async Task<BaseResponse<MaintenanceResponse>> GetMaintenanceV2(List<string> clientCodes)
        {
            using (var activity = _activitySource.StartActivity("get.maintenance.v2"))
            {
                activity?.SetTag("client.codes", clientCodes?.Join(","));
                try
                {
                    _logger.LogInformation($"[GetMaintenanceV2] clientCodes={clientCodes?.Join(",") ?? "NULL"}");

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
                            Message = $"Empty client reference : {clientCodes?.Join(",") ?? "No Clients"}"
                        };
                    }

                    var clientsConfig = new List<MaintenanceConfigModel>();

                    foreach (var app in _maintenanceApps)
                    {
                        var configs = new List<ExternalMaintenanceConfig>();

                        _logger.LogInformation($"[GetMaintenanceV2] Processing app={app}");

                        if (app.Equals("bl", StringComparison.OrdinalIgnoreCase))
                        {
                            var tempConfigs = new List<ExternalMaintenanceConfig>();
                            using (StreamReader reader = new StreamReader(_maintenanceConfigJson))
                            {
                                string text = await reader.ReadToEndAsync();

                                tempConfigs = JsonSerializer.Deserialize<List<ExternalMaintenanceConfig>>(text);
                            }

                            _logger.LogInformation($"[GetMaintenanceV2] bl file configs count={tempConfigs?.Count ?? -1}");

                            if (tempConfigs != null && tempConfigs.Any())
                                configs.AddRange(tempConfigs);
                        }
                        else
                        {
                            try
                            {
                                var response = await _maintenanceHttpClient.GetAsync($"/v1.0/invoke/{app.ToLower()}/method/maintenance");
                                var content = await response.Content.ReadAsStringAsync();

                                _logger.LogInformation($"[GetMaintenanceV2] Dapr app={app} status={response.StatusCode}");

                                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                {
                                    activity?.SetTag("app", app);
                                    activity?.SetTag("status", response.StatusCode.ToString());
                                    activity?.SetTag("events", content);
                                    continue;
                                }

                                var externalContent = JsonSerializer.Deserialize<ExternalMaintenanceResponse>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                                configs.AddRange(externalContent.Config);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"[GetMaintenanceV2] Dapr error app={app}: {e.Message}");
                            }
                        }

                        _logger.LogInformation($"[GetMaintenanceV2] app={app} configs count={configs?.Count ?? -1}");

                        if (configs != null && configs.Any())
                        {
                            foreach (var config in configs)
                            {
                                var reference = clientReference.FirstOrDefault(c => c.ClientId.ToString().Equals(config.ClientId, StringComparison.OrdinalIgnoreCase));

                                if (reference == null)
                                    continue;

                                clientsConfig.Add(new MaintenanceConfigModel()
                                {
                                    ClientId = config.ClientId,
                                    IsMaintenance = config.IsMaintenance,
                                    Site = config.Site,
                                    ClientCode = reference.ClientCode,
                                    ClientName = reference.ClientName,
                                    App = app
                                });
                            }
                        }
                    }

                    // If no app returned any config entries, seed from Platform clients
                    // so the dropdown is populated for first-time setup
                    if (!clientsConfig.Any() && clientReference.Any())
                    {
                        foreach (var client in clientReference)
                        {
                            clientsConfig.Add(new MaintenanceConfigModel()
                            {
                                ClientId = client.ClientId.ToString(),
                                IsMaintenance = false,
                                Site = string.Empty,
                                ClientCode = client.ClientCode,
                                ClientName = client.ClientName,
                                App = "BL"
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
                activity?.SetTag("client.codes", clientCodes?.Join(","));
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
                        var app = appGroup.Key.ToLower();
                        var clientIds = (requestClientIds.Count() == 1 && requestClientIds.FirstOrDefault().ToUpper() == "ALL") ? "all" : string.Join(",", appGroup.Select(c => c.ClientId));


                        if (app.Equals("bl", StringComparison.OrdinalIgnoreCase))
                        {
                            var configs = new List<ExternalMaintenanceConfig>();
                            if (clientIds.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                            {
                                if (appGroup != null)
                                {
                                    configs = referenceClients.Where(c => c.App.Equals("bl", StringComparison.OrdinalIgnoreCase)).Select(c => new ExternalMaintenanceConfig()
                                    {
                                        ClientId = c.ClientId,
                                        IsMaintenance = request.IsMaintenance,
                                        Site = c.Site
                                    }).ToList();
                                }
                            }
                            else
                            {
                                // Default all to false — only the selected clients get request.IsMaintenance
                                configs = referenceClients.Where(c => c.App.Equals("bl", StringComparison.OrdinalIgnoreCase)).Select(c => new ExternalMaintenanceConfig()
                                {
                                    ClientId = c.ClientId,
                                    IsMaintenance = false,
                                    Site = c.Site
                                }).ToList();

                                if (configs != null)
                                {
                                    configs.ForEach(config =>
                                    {
                                        if (clientIds.Contains(config.ClientId, StringComparison.OrdinalIgnoreCase))
                                            config.IsMaintenance = request.IsMaintenance;
                                    });
                                }
                            }

                            if (configs != null && configs.Any())
                            {
                                using (FileStream fs = new FileStream(_maintenanceConfigJson, FileMode.Create, FileAccess.Write, FileShare.None))
                                using (StreamWriter writer = new StreamWriter(fs))
                                {
                                    var configText = JsonSerializer.Serialize(configs, new JsonSerializerOptions
                                    {
                                        WriteIndented = true
                                    });
                                    writer.Write(configText);
                                }
                            }
                        }
                        else
                        {
                            var stringPayload = JsonSerializer.Serialize(new
                            {
                                ClientIds = clientIds,
                                request.IsMaintenance
                            }); 

                            var result = await _maintenanceHttpClient.PostAsync($"/v1.0/invoke/{app}/method/maintenance", new StringContent(stringPayload, Encoding.UTF8, "application/json"));

                            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                var content = await result.Content.ReadAsStringAsync();
                                activity?.SetTag("app", app);
                                activity?.SetTag("status", result.StatusCode.ToString());
                                activity?.SetTag("payload", stringPayload);
                                activity?.SetTag("content", content);
                            }
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
        #endregion
    }
}
