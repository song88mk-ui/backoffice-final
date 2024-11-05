using Azure.Storage.Blobs;
using Backoffice.Shared.Enums;
using Backoffice.Shared.ExternalApis.Maintenance;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
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
        private readonly ITradingRepository _tradingRepository;

        private HttpClient _maintenanceHttpClient;
        private readonly TelemetryClient _telemetryClient;
        private readonly bool _isStaging;
        private readonly string _internalClient;
        private readonly ActivitySource _activitySource;
        private BlobContainerClient _containerClient;

        private readonly List<string>? _maintenanceApps;
        private readonly string _maintenanceConfigJson;
        private readonly string _maintenanceConfigJsonTest;


        public MaintenanceRepository(HttpClient httpClient, IConfiguration configuration, ITradingRepository tradingRepository, TelemetryClient telemetryClient)
        {
            _isStaging = bool.Parse(configuration["IsStaging"]);
            _internalClient = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64";

            _tradingRepository = tradingRepository;
            _telemetryClient = telemetryClient;

            _maintenanceConfigJson = configuration["Custom:MaintenanceConfigFile"];
            _maintenanceConfigJsonTest = configuration["Custom:MaintenanceConfigFileTest"];
            _maintenanceApps = configuration["Maintenance:Apps"]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.Select(s => s.Trim().ToUpper()).ToList() ?? null;
            _maintenanceHttpClient = httpClient;
        }

        #region Methods
        public async Task<BaseResponse<MaintenanceResponse>> GetMaintenanceV2(List<string> clientCodes)
        {
            try
            {
                var clientResponse = await _tradingRepository.GetClientList("");

                if (clientResponse?.Result == null)
                    return new BaseResponse<MaintenanceResponse>()
                    {
                        StatusCode = clientResponse.StatusCode,
                        Message = clientResponse.Message
                    };

                var clientReference = clientResponse.Result;

                if (clientCodes.Count > 1)
                    clientReference = clientReference.Where(c => clientCodes.Contains(c.ClientCode, StringComparer.OrdinalIgnoreCase)).ToList();

                if (clientReference == null)
                {
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

                    if (app.Equals("bl", StringComparison.OrdinalIgnoreCase))
                    {
                        var tempConfigs = new List<ExternalMaintenanceConfig>();
                        using (StreamReader reader = new StreamReader(_maintenanceConfigJson))
                        {
                            string text = await reader.ReadToEndAsync();

                            tempConfigs = JsonSerializer.Deserialize<List<ExternalMaintenanceConfig>>(text);
                        }

                        if (tempConfigs != null && tempConfigs.Any())
                            configs.AddRange(tempConfigs);
                    }
                    else
                    {
                        try
                        {
                            var response = await _maintenanceHttpClient.GetAsync($"/v1.0/invoke/{app.ToLower()}/method/maintenance");
                            var content = await response.Content.ReadAsStringAsync();

                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                var properties = new Dictionary<string, string>
                            {
                                { "app", app },
                                { "status", response.StatusCode.ToString() },
                                { "events", content }
                            };

                                _telemetryClient.TrackEvent($"e:backoffice-maintenance.get.client", properties);
                                continue;
                            }

                            var externalContent = JsonSerializer.Deserialize<ExternalMaintenanceResponse>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                            configs.AddRange(externalContent.Config);
                        }
                        catch (Exception)
                        {

                        }
                    }

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
                return new BaseResponse<MaintenanceResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message.ToString()
                };
            }
        }
        public async Task<BaseResponse<MaintenanceResponse>> SetMaintenanceV2(SetMaintenanceRequest request, List<string> clientCodes)
        {
            try
            {
                var requestClientIds = request?.ClientIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToList() ?? new List<string>();

                var clients = await GetMaintenanceV2(clientCodes);

                if (clients.StatusCode != StatusCode.Success)
                    return clients;

                var referenceClients = clients?.Result?.ClientConfig;
                if (referenceClients == null)
                    return new BaseResponse<MaintenanceResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };

                var appGroups = referenceClients.Where(c => (requestClientIds.Count() == 1 && requestClientIds.FirstOrDefault().ToUpper() == "ALL")
                                                || (requestClientIds.Any(ci => (ci.Contains(":") && ci.Contains($"{c.App}:{c.ClientId}", StringComparison.OrdinalIgnoreCase)) || ci == c.ClientId)))
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
                            configs = referenceClients.Where(c => c.App.Equals("bl", StringComparison.OrdinalIgnoreCase)).Select(c => new ExternalMaintenanceConfig()
                            {
                                ClientId = c.ClientId,
                                IsMaintenance = c.IsMaintenance,
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
                            using (StreamWriter writer = new StreamWriter(_maintenanceConfigJsonTest))
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

                            var properties = new Dictionary<string, string>
                            {
                                { "app", app },
                                { "status" , result.StatusCode.ToString() },
                                { "payload", stringPayload },
                                { "message", $"{result.StatusCode} - {app} - {content}"},
                                {  "event" , content }
                            };

                            _telemetryClient.TrackEvent($"e:backoffice-maintenance.set.client", properties);
                        }
                    }
                }

                clients = await GetMaintenanceV2(clientCodes);

                return new BaseResponse<MaintenanceResponse>()
                {
                    StatusCode = clients.StatusCode,
                    Message = clients.Message,
                    Result = clients.Result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MaintenanceResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message.ToString()
                };
            }
        }
        #endregion

        #region Utilities
        #endregion
    }
}
