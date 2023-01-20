using System;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyClientMarketSortOrderRequest
    {
        public Guid CopyFromClientId { get; set; }
        public int CopyFromSportId { get; set; }
        public int CopyFromFixtureStatusId { get; set; }
        public Guid CopyToClientId { get; set; }
        public int CopyToSportId { get; set; }
        public int CopyToFixtureStatusId { get; set; }
        public string UserId { get; set; }
    }
}
