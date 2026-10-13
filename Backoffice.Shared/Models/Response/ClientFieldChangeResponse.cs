using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientFieldChangeResponse
    {
        public string ClientCode { get; set; }
        public string FieldName { get; set; }
        public string PreviousValue { get; set; }
        public string NewValue { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
