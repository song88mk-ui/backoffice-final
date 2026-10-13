using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class CommitClientUpdateResponse
    {
        public Guid BatchId { get; set; }
        public int TotalClients { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public List<ClientUpdateRowResult> Rows { get; set; } = new List<ClientUpdateRowResult>();
    }
}
