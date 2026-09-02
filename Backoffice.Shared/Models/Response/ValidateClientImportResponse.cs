using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ValidateClientImportResponse
    {
        public int TotalClients { get; set; }
        public int ReadyCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ClientImportRowResult> Rows { get; set; } = new List<ClientImportRowResult>();
    }
}
