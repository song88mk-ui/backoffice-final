
using Backoffice.Shared.Models;
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class SubUserClaimsModel
    {
        public SubUserClaimsModel()
        {
            SubUserClaims = new List<PermissionModel>();
        }
        public string MainUserId { get; set; }
        public string MainUserName { get; set; }
        public List<PermissionModel> SubUserClaims { get; set; }
    }

    public class SubUserClaim : ClaimModel
    {
        public string Name { get; set; }
        public bool HasReadOnly { get; set; }
        public bool IsEnabled { get; set; }
    }
}
