using System.Text.Json.Serialization;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnWebPropsChangeMessage
    {
        // BO-108: this is the client's Platform GUID, not an SBC client code. The
        // JsonPropertyName keeps the wire key "ClientCode" unchanged, since
        // odds-engine-2's IncomingWebPropsChange binds to that exact key.
        [JsonPropertyName("ClientCode")]
        public string ClientId { get; set; }
    }
}
