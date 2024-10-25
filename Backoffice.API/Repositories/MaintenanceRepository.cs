using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Cdn.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.ExternalApis.Maintenance;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
using static System.Net.Mime.MediaTypeNames;

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

                        _telemetryClient.TrackEvent($"e:backoffice-maintenance.client", properties);
                        continue;
                    }

                    var extexrnalConfigs = JsonSerializer.Deserialize<ExternalMaintenanceResponse>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (extexrnalConfigs?.Config != null)
                    {
                        foreach (var config in extexrnalConfigs.Config)
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
                var clientIds = request?.ClientIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToList() ?? new List<string>();

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

                foreach (var item in clientIds)
                {
                    var reference = referenceClients.FirstOrDefault(c => string.Equals(c.ClientId, item, StringComparison.OrdinalIgnoreCase));

                    if (reference == null)
                    {
                        hasError = true;

                        errorMessages.Add($"No reference - Unknown - {item}");
                        continue;
                    }

                    var result = await _httpClient.PostAsync($"/v1.0/invoke/{reference.App.ToLower()}/method/maintenance", new StringContent(JsonSerializer.Serialize(new
                    {
                        ClientId = item,
                        reference.IsMaintenance,
                    }), Encoding.UTF8, "application/json"));

                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        hasError = true;

                        errorMessages.Add($"{result.StatusCode} - {reference.App} - {await result.Content.ReadAsStringAsync()}");
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

            return System.Text.Json.JsonSerializer.Deserialize<List<MaintenanceModel>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
        #endregion
    }
}
