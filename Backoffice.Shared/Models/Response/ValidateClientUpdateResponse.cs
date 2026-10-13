using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ValidateClientUpdateResponse
    {
        public int TotalClients { get; set; }
        public int ReadyCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ClientUpdateRowResult> Rows { get; set; } = new List<ClientUpdateRowResult>();
    }
}
