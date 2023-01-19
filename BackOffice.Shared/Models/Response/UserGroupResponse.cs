
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class UserGroupResponse
    {
        public UserGroupResponse()
        {
            Claims = new List<ClaimModel>();
        }
        public int Num { get; set; }
        public string UserGroupId { get; set; }
        public string UserGroupName { get; set; }
        public List<ClaimModel> Claims { get; set; }
    }
}
