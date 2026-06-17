using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    [Table("SportMapping")]
    public class SportMapping
    {
        public int SpId { get; set; }
        public string SpNa { get; set; }
        public string In { get; set; }
        public int? ChTyId { get; set; }
        public DateTime Ti { get; set; }
    }
}
