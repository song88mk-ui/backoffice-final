using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyClientMarketSortOrderRequest
    {
        public Guid CopyFromClientId { get; set; }
        public int CopyFromSportId { get; set; }
        public int CopyFromFixtureStatusId { get; set; }
        // Single target, as sent before BO-159. Used only when CopyToClientIds is empty.
        public Guid CopyToClientId { get; set; }
        // BO-159: every target client. Each one gets the same sport and scope.
        public List<Guid> CopyToClientIds { get; set; } = new List<Guid>();
        public int CopyToSportId { get; set; }
        public int CopyToFixtureStatusId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }
}
