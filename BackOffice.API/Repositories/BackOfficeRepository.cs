using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly IConfiguration _configuration;
        private BackofficeContext _backofficeContext;
        public BackofficeRepository(IConfiguration configuration, BackofficeContext backofficeContext)
        {
            _configuration = configuration;
            _backofficeContext = backofficeContext;
        }

        public async Task<BaseResponse<ClientListResponse>> GetClients()
        {
            try
            {
                var clients = _backofficeContext.ClientInformation
                    .Select(client => new ClientResponse
                    {
                        ClientId = client.ClientId,
                        BusinessName = client.Name,
                        Brand = client.Brand,
                        Region = client.Region
                    }).ToList();

                return new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientListResponse
                    {
                        Clients = clients ?? new List<ClientResponse>()
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }
        public async Task<BaseResponse<string>> GetClientRegion(int clientId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<BaseResponse<Dictionary<string, string>>> GetClientsByRegion(string clientIdRefs)
        {
            throw new System.NotImplementedException();
        }

        public async Task<BaseResponse<BaseResponse<bool>>> CopyClientTopFixtures(CopyClientTopFixtureRequest request)
        {
            throw new System.NotImplementedException();
        }

        public async Task<BaseResponse<List<CurrenciesResponse>>> GetCurrencies()
        {
            throw new System.NotImplementedException();
        }
    }
}
