namespace Backoffice.Shared.OutgoingEngineBookmakerMessages
{
    public class OnExternalBetStopGlobalMessage
    {
        public bool IsBetStop { get; set; }
        public string CorrelationId { get; set; }
    }
}
