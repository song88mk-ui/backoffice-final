using Backoffice.Shared.Enums;
using System;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnDeleteCacheMessage
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public CacheType CacheType { get; set; }
        public string CacheKey { get; set; }
    }
}
