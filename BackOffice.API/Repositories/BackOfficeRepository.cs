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
        public async Task<BaseResponse<PageListResponse<ClientListResponse>>> GetClients(int page, int pageSize, string businessName, string sortOrder)
        {
            var rowCount = 0.0;
            var result = new List<ClientListResponse>();

            return new BaseResponse<PageListResponse<ClientListResponse>>
            {
                StatusCode = Shared.Enums.StatusCode.Success,
                Message = Shared.Enums.StatusCode.Success.ToString(),
                Result = new PageListResponse<ClientListResponse>
                {
                    CurrentPage = page,
                    PageCount = (int)Math.Ceiling(rowCount / pageSize),
                    PageSize = pageSize,
                    RowCount = (int)rowCount,
                    Data = result
                }
            };
        }

        public Task<BaseResponse<string>> GetClientRegion(int clientId)
        {
            throw new System.NotImplementedException();
        }

        public Task<BaseResponse<Dictionary<string, string>>> GetClientsByRegion(string clientIdRefs)
        {
            throw new System.NotImplementedException();
        }

        public Task<BaseResponse<BaseResponse<bool>>> CopyClientTopFixtures(CopyClientTopFixtureRequest request)
        {
            throw new System.NotImplementedException();
        }

        public List<CurrenciesResponse> GetCurrencies()
        {
            throw new System.NotImplementedException();
        }
    }
}
