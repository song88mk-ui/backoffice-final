
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class SubUserClaimsModel
    {
        public SubUserClaimsModel()
        {
            SubUserClaims = new List<SubUserClaim>();
        }
        public List<SubUserClaim> SubUserClaims { get; set; }
    }

    public class SubUserClaim
    {
        public string Name { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        public bool HasReadOnly { get; set; }
        public bool IsEnabled { get; set; }
    }
}
