using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly IConfiguration _configuration;
        public BackofficeRepository()
        {

        }

        public async Task<BaseResponse<List<ClientListResponse>>> GetClients()
        {
            try
            {


                return new BaseResponse<List<ClientListResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}",
                    Result = null
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientListResponse>>
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
