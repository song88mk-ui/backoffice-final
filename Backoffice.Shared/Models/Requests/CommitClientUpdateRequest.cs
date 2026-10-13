using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class CommitClientUpdateRequest
    {
        public Guid BatchId { get; set; }
        public string Environment { get; set; }
        public List<ClientUpdateRowRequest> Rows { get; set; } = new List<ClientUpdateRowRequest>();
    }
}
