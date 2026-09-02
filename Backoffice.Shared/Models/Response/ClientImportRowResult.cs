using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientImportRowResult
    {
        public int RowNumber { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }

        public string Status { get; set; }
        public string Message { get; set; }
        public Guid? ResultClientId { get; set; }
    }
}
