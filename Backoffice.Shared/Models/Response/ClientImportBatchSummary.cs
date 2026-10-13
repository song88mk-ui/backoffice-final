using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientImportBatchSummary
    {
        public Guid BatchId { get; set; }
        public string Environment { get; set; }
        public string OperationType { get; set; }
        public string Status { get; set; }
        public string StartedByUserName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalRows { get; set; }
        public int SucceededRows { get; set; }
        public int FailedRows { get; set; }
    }
}
