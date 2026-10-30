using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IClientImportRepository
    {
        Task<BaseResponse<ValidateClientImportResponse>> ValidateBatch(ValidateClientImportRequest request);
        Task<BaseResponse<CommitClientImportResponse>> CommitBatch(CommitClientImportRequest request, string userId, string userName);
        Task<BaseResponse<bool>> IsImportRunning(string environment);
        Task<BaseResponse<int>> InterruptAbandonedBatches();
        Task<BaseResponse<ClientImportProgressResponse>> GetProgress(Guid batchId);
        BaseResponse<string> GetEnvironment();

        Task<BaseResponse<List<ClientExportRow>>> ExportClients();
        Task<BaseResponse<ValidateClientUpdateResponse>> ValidateUpdateBatch(ValidateClientUpdateRequest request);
        Task<BaseResponse<CommitClientUpdateResponse>> CommitUpdateBatch(CommitClientUpdateRequest request, string userId, string userName);
        Task<BaseResponse<List<ClientFieldChangeResponse>>> GetFieldChanges(Guid batchId);
        Task<BaseResponse<List<ClientImportBatchSummary>>> GetBatchHistory(string operationType);
    }
}
