using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    [Table("ClientImportBatch")]
    public class ClientImportBatch
    {
        [Key]
        public Guid BatchId { get; set; }
        public string Environment { get; set; }
        public string Status { get; set; }
        public string StartedByUserId { get; set; }
        public string StartedByUserName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalRows { get; set; }
        public int SucceededRows { get; set; }
        public int FailedRows { get; set; }
    }
}
