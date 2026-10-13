using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ValidateClientUpdateRequest
    {
        public string Environment { get; set; }
        public List<ClientUpdateRowRequest> Rows { get; set; } = new List<ClientUpdateRowRequest>();
    }
}
