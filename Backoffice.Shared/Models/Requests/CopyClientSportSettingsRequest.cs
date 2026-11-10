using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    // BO-160: copies the Sport Settings shown on screen for one client (sport order, active sports, segment and
    // league order) to one or more other clients. Each target's existing sport settings are replaced.
    public class CopyClientSportSettingsRequest
    {
        public string FromClientCode { get; set; }
        public List<string> ToClientCodes { get; set; } = new List<string>();
        public List<SportsSortOrder> SportsSortOrder { get; set; } = new List<SportsSortOrder>();
        public string UserId { get; set; }
        public string UserName { get; set; }
    }
}
