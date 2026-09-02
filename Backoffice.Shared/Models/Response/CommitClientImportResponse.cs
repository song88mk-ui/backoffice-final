using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class CommitClientImportResponse
    {
        public Guid BatchId { get; set; }
        public int TotalClients { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public List<ClientImportRowResult> Rows { get; set; } = new List<ClientImportRowResult>();
    }
}
