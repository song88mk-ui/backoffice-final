using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    [Table("LeagueMapping")]
    public class LeagueMapping
    {
        public int LeId { get; set; }
        public string LeNa { get; set; }
        public string In { get; set; }
        public int? ChTyId { get; set; }
        public DateTime Ti { get; set; }
    }
}
