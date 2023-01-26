using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    [Table("ActivityLog2")]
    public class ActivityLog
    {
        [Key]
        public long Id { get; set; }
        public string Page { get; set; }
        public string Payload { get; set; }
        public string Activity { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
