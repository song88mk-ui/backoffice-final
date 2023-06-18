using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    public class ClientBetSetting
    {
        [Key]
        public long ClientBetSettingId { get; set; }

        public long ClientId { get; set; }

        public int CategoryId { get; set; }

        public string Setting { get; set; }

        public long CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public long ModifiedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string CurrencyCode { get; set; }
    }
}
