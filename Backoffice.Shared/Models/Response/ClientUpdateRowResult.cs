using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ClientUpdateRowResult
    {
        public int RowNumber { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }

        // Ready | Error (validate) — Succeeded | Failed (commit)
        public string Status { get; set; }
        public string Message { get; set; }
        public List<ClientFieldChangeResponse> Changes { get; set; } = new List<ClientFieldChangeResponse>();
    }
}
