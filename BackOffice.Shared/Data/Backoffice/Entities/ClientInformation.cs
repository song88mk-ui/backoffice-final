using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    public class ClientInformation
    {
        [Key]
        public int ClientId { get; set; }

        public string Name { get; set; }

        public string TelephoneNumber { get; set; }

        public string Website { get; set; }

        public int StatusId { get; set; }

        [Column(TypeName = "decimal(30,18)")]
        public decimal MonthlyFee { get; set; }

        public int ApplicationId { get; set; }

        [Column(TypeName = "decimal(30,18)")]
        public decimal CompanyShare { get; set; }

        [Column(TypeName = "decimal(30,18)")]
        public decimal SportsbookShare { get; set; }
        [Column(TypeName = "decimal(30,18)")]
        public decimal JackpotShare { get; set; }

        public string RevenueShareSportsbook { get; set; }

        public string RevenueShareJackpot { get; set; }

        public bool IsTierRevenueShare { get; set; }

        [Column(TypeName = "decimal(30,18)")]
        public decimal ClientShare { get; set; }

        [Column(TypeName = "decimal(30,18)")]
        public decimal SetupFee { get; set; }

        public long UserId { get; set; }

        public string Brand { get; set; }

        public long CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public long ModifiedBy { get; set; }

        public DateTime ModifiedDate { get; set; }
        public string TaxConfig { get; set; }
        public string Region { get; set; }
    }
}
