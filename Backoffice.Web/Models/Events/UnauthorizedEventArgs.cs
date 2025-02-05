using Backoffice.Shared.Enums;

namespace Backoffice.Web.Models.Events
{
    public class UnauthorizedEventArgs : EventArgs
    {
        public StatusCode StatusCode { get; }
        public string Message { get; }

        public UnauthorizedEventArgs(StatusCode statusCode, string message)
        {
            this.StatusCode = statusCode;
            this.Message = message;
        }
    }
}
