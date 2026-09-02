using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IClientImportRepository
    {
        Task<BaseResponse<ValidateClientImportResponse>> ValidateBatch(ValidateClientImportRequest request);
        Task<BaseResponse<CommitClientImportResponse>> CommitBatch(CommitClientImportRequest request, string userId, string userName);
        Task<BaseResponse<bool>> IsImportRunning(string environment);
        Task<BaseResponse<ClientImportProgressResponse>> GetProgress(Guid batchId);
        BaseResponse<string> GetEnvironment();
    }
}
