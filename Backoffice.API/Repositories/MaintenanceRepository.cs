using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Cdn.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
using System.Collections.Immutable;
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

        private readonly TelemetryClient _telemetryClient;
        private HttpClient _httpClient;
        private string _fileName = Path.Join("web", "config.json");
        private BlobClient _blobClient = null;
        private List<MaintenanceModel> _clientsConfigs = new List<MaintenanceModel>();
        private readonly bool _isStaging;
        private readonly string _internalClient;
        private readonly List<string>? _maintenanceApps;
        private readonly ActivitySource _activitySource;
        private BlobContainerClient _containerClient;


        public MaintenanceRepository(IConfiguration configuration, HttpClient httpClient, ITradingRepository tradingRepository, TelemetryClient telemetryClient)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(configuration["ConnectionStrings:AzureStorage"]);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("assets");
            _blobClient = containerClient.GetBlobClient(_fileName);
            _clientsConfigs = GetConfig();
            _isStaging = bool.Parse(configuration["IsStaging"]);
            _internalClient = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64";
            _tradingRepository = tradingRepository;
            _telemetryClient = telemetryClient;

            var blobServiceClientv2 = new BlobServiceClient(
                    new Uri("https://prodstoragecdn.blob.core.windows.net"),
                    new DefaultAzureCredential());
            _containerClient = blobServiceClientv2.GetBlobContainerClient("assets");

            var env = Environment.GetEnvironmentVariable("PLATFORM_WEB_ENV_NAME") ?? "dev";
            var configFilename = "configv2.json";

            _fileName = env.ToLower() switch
            {
                "production" => Path.Join("web", "production", configFilename),
                _ => Path.Join("web", "staging", configFilename)
            };

            _maintenanceApps = configuration["Maintenance:Apps"]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.Select(s => s.Trim().ToUpper()).ToList() ?? null;

            _httpClient = httpClient;
        }

        #region Methods
        public async Task<BaseResponse<bool>> SetMaintenance(bool content)
        {
            try
            {
                var newClientConfigs = new List<MaintenanceModel>();

                foreach (var item in _clientsConfigs)
                {
                    newClientConfigs.Add(new MaintenanceModel()
                    {
                        ClientId = item.ClientId,
                        IsMaintenance = _isStaging && item.ClientId == _internalClient ? content : false,
                        Site = item.Site
                    });
                }

                _clientsConfigs = newClientConfigs;
                var result = await UploadToAzure(newClientConfigs);

                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message.ToString(),
                    Result = false
                };
            }
        }
        public async Task<BaseResponse<bool>> GetMaintenance()
        {
            try
            {
                if (_isStaging)
                {
                    return new BaseResponse<bool>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = _clientsConfigs.Where(x => x.ClientId == _internalClient).FirstOrDefault().IsMaintenance
                    };
                }
                else if (_clientsConfigs.AsEnumerable().Any(x => x.IsMaintenance))
                {
                    return new BaseResponse<bool>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = true
                    };
                }

                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = false
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message.ToString(),
                    Result = false
                };
            }
        }
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
                        configs = GetConfigV2();
                    }
                    else
                    {
                        var response = await _httpClient.GetAsync($"/v1.0/invoke/{app.ToLower()}/method/maintenance");
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

                        var extexrnalConfigs = JsonSerializer.Deserialize<ExternalMaintenanceResponse>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                        configs = extexrnalConfigs.Config;
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
                var referenceClients = clients?.Result?.ClientConfig;

                if (referenceClients == null)
                    return new BaseResponse<MaintenanceResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };

                var hasError = false;
                var errorMessages = new List<string>();

                var appGroups = referenceClients.Where(c => (requestClientIds.Count() == 1 && requestClientIds.FirstOrDefault().ToUpper() == "ALL")
                                                || (requestClientIds.Any(ci => (ci.Contains(":") && ci.Contains($"{c.App}:{c.ClientId}", StringComparison.OrdinalIgnoreCase)) || ci == c.ClientId)))
                                                .GroupBy(c => c.App).ToList();

                foreach (var appGroup in appGroups)
                {
                    var app = appGroup.Key.ToLower();
                    var clientIds = (requestClientIds.Count() == 1 && requestClientIds.FirstOrDefault().ToUpper() == "ALL") ? "all" : string.Join(",", appGroup.Select(c => c.ClientId));

                    if (app.Equals("bl", StringComparison.OrdinalIgnoreCase))
                    {
                        var configs = appGroup.Select(s => new ExternalMaintenanceConfig
                        {
                            ClientId = s.ClientId,
                            IsMaintenance = s.IsMaintenance
                        }).ToList();

                        configs.ForEach(config =>
                        {
                            if (clientIds.Contains(config.ClientId, StringComparison.OrdinalIgnoreCase))
                            {
                                config.IsMaintenance = request.IsMaintenance;
                            }
                        });

                        await UploadToAzureV2(configs.Select(s => new
                        {
                            s.ClientId,
                            s.IsMaintenance
                        }).ToList());
                    }
                    else
                    {
                        var stringPayload = JsonSerializer.Serialize(new
                        {
                            ClientIds = clientIds,
                            request.IsMaintenance
                        });

                        var result = await _httpClient.PostAsync($"/v1.0/invoke/{app}/method/maintenance", new StringContent(stringPayload, Encoding.UTF8, "application/json"));

                        if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            hasError = true;

                            var content = await result.Content.ReadAsStringAsync();
                            errorMessages.Add($"{result.StatusCode} - {app} - {content}");

                            var properties = new Dictionary<string, string>
                        {
                            { "app", app },
                            { "status" , result.StatusCode.ToString() },
                            { "payload", stringPayload },
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
        private List<MaintenanceModel> GetConfig()
        {
            var result = "";
            if (_blobClient.ExistsAsync().Result)
            {
                var response = _blobClient.DownloadAsync().Result;
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    while (!streamReader.EndOfStream)
                    {
                        result += streamReader.ReadLineAsync().Result;
                    }
                }
            }

            return JsonSerializer.Deserialize<List<MaintenanceModel>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, });
        }
        private List<ExternalMaintenanceConfig> GetConfigV2()
        {
            var bobClient = _containerClient.GetBlobClient(_fileName);

            var result = "";
            if (bobClient.ExistsAsync().Result)
            {
                var response = bobClient.DownloadAsync().Result;
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    while (!streamReader.EndOfStream)
                    {
                        result += streamReader.ReadLineAsync().Result;
                    }
                }
            }

            return string.IsNullOrEmpty(result) ? new List<ExternalMaintenanceConfig>() : JsonSerializer.Deserialize<List<ExternalMaintenanceConfig>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        private async Task<bool> UploadToAzure(List<MaintenanceModel> content)
        {
            try
            {
                var stream = new MemoryStream(Encoding.Default.GetBytes(JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true })));
                _blobClient.Upload(stream, new BlobHttpHeaders { ContentType = "application/json" });

                // Purge
                TokenCredential cred = new ClientSecretCredential("522e8ff6-5c95-49b0-b127-31f1c46fa371", "5b88a332-75a7-471a-a9ee-1deb918631ab", "4AZ8Q~fOaV_aHYnUxg6vHJdGCGRO0s-oePbzobTL");

                var client = new ArmClient(cred);
                string subscriptionId = "6433466a-099a-4d98-8f8a-b12c5975c274";
                string resourceGroupName = "rg-production-song88";
                string profileName = "cdn-song88";
                string endpointName = "song88";
                ResourceIdentifier cdnEndpointResourceId = CdnEndpointResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, profileName, endpointName);
                CdnEndpointResource cdnEndpoint = client.GetCdnEndpointResource(cdnEndpointResourceId);

                await cdnEndpoint.PurgeContentAsync(WaitUntil.Completed, new PurgeContent(new string[] { "/web/config.json" }));
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private async Task<bool> UploadToAzureV2(object content)
        {
            try
            {
                BlobClient blobClient = _containerClient.GetBlobClient(_fileName);

                var stream = new MemoryStream(Encoding.Default.GetBytes(JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true })));
                await blobClient.UploadAsync(stream, true);
                var test = await blobClient.UploadAsync(_fileName, true);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
