using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ValidateClientImportRequest
    {
        public string Environment { get; set; }
        public List<ClientImportRowRequest> Rows { get; set; } = new List<ClientImportRowRequest>();
    }
}
