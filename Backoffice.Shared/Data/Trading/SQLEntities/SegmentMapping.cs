using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    [Table("SegmentMapping")]
    public class SegmentMapping
    {
        public int SegId { get; set; }
        public string SegNa { get; set; }
        public string In { get; set; }
        public int? ChTyId { get; set; }
        public DateTime Ti { get; set; }
    }
}
