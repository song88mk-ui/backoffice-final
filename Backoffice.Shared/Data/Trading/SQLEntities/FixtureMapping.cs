using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class FixtureMapping
    {
        public string FiId { get; set; }
        public string FiNa { get; set; }
        public string In { get; set; }
        public int SpId { get; set; }
        public int SegId { get; set; }
        public int LeId { get; set; }
        public int? ChTyId { get; set; }
        public DateTime Ti { get; set; }
    }
}
