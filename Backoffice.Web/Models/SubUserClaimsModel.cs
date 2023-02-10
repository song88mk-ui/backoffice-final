
using Backoffice.Shared.Models;

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
}
