using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Cdn.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Backoffice.API.Models.MaintenanceApi;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Backoffice.API.Repositories
{
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private HttpClient _httpClient { get; set; }
        private string _fileName = Path.Join("web", "config.json");
        private BlobClient _blobClient = null;
        private List<MaintenanceModel> _clientsConfigs = new List<MaintenanceModel>();
        private readonly bool _isStaging;
        private readonly string _internalClient;

        public MaintenanceRepository(IConfiguration configuration, HttpClient httpClient)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(configuration["ConnectionStrings:AzureStorage"]);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("assets");
            _blobClient = containerClient.GetBlobClient(_fileName);
            _clientsConfigs = GetConfig();
            _isStaging = bool.Parse(configuration["IsStaging"]);
            _internalClient = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64";

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
        public async Task<BaseResponse<List<string>>> GetMaintenanceV2(List<string> clientIds)
        {
            try
            {
                //var response = await _httpClient.GetAsync($"/v1.0/invoke/method/maintenance");
                var response = await _httpClient.GetAsync($"/v1.0/invoke/<APP>/method/maintenance"); // ??

                var content = JsonConvert.DeserializeObject<ExternalMaintenanceResponse>(response.Content.ReadAsStringAsync().Result);
                var clients = content.Live.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();

                var result = clients.Where(c => clientIds.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

                return new BaseResponse<List<string>>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<string>>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message.ToString()
                };
            }
        }
        public async Task<BaseResponse<bool>> SetMaintenanceV2(SetMaintenanceRequest request)
        {
            try
            {
                var result = await _httpClient.PostAsync($"/v1.0/invoke/method/maintenance", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    return new BaseResponse<bool>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                        Result = false
                    };

                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
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
