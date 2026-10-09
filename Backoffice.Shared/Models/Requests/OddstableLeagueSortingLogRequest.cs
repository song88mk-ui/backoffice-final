using System;

namespace Backoffice.Shared.Models.Requests
{
    public class OddstableLeagueSortingLogRequest
    {
        // Empty = every client.
        public string ClientCode { get; set; }
        // Entries older than this are left out. Left unset, the last 30 days are returned.
        public DateTime DateFrom { get; set; }
        public MetaData MetaData { get; set; }
    }
}
