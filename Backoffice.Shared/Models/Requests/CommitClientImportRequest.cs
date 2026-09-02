using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class CommitClientImportRequest
    {
        public Guid BatchId { get; set; }
        public string Environment { get; set; }
        public List<ClientImportRowRequest> Rows { get; set; } = new List<ClientImportRowRequest>();
    }
}
