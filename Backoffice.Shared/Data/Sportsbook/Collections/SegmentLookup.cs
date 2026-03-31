using MongoDB.Bson.Serialization.Attributes;

namespace OE.Shared.Data.Sportsbook.Collections
{
    [BsonIgnoreExtraElements]
    public class SegmentLookup
    {
        [BsonId]
        public string SeLoId { get; set; }
        public int SpId { get; set; }
        public int SegId { get; set; }
        public string SegNa { get; set; }
        public bool IsMap { get; set; }
    }

}
