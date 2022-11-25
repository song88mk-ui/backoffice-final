using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        public Task<BaseResponse<List<ClientListResponse>>> GetClients()
        {
            throw new System.NotImplementedException();
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
