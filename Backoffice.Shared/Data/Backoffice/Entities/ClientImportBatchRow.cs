using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    [Table("ClientImportBatchRow")]
    public class ClientImportBatchRow
    {
        [Key]
        public long Id { get; set; }
        public Guid BatchId { get; set; }
        public int RowNumber { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Status { get; set; }
        public string StepsCompleted { get; set; }
        public string ErrorMessage { get; set; }
        public Guid? ResultClientId { get; set; }
    }
}
