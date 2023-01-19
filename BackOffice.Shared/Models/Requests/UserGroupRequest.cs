
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class UserGroupRequest
    {
        public UserGroupRequest()
        {
            Claims = new List<ClaimModel>();
        }
        public string UserGroupId { get; set; }
        public string UserGroupName { get; set; }
        public List<ClaimModel> Claims { get; set; }
    }
}
