namespace Backoffice.WebSocket.API.Events
{
    public interface IBackofficeEvent
    {
        Task OnOddsChange(OddsChangeFixture message);
    }
}
