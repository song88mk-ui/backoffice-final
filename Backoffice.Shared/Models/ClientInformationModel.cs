using System;

namespace Backoffice.Shared.Models
{
    public class ClientInformationModel
    {
        public string ClientCode { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string Websites { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
    }
}
