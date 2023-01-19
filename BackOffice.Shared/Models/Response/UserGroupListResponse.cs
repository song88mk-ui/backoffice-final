
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class UserGroupListResponse
    {
        public UserGroupListResponse()
        {
            UserGroups = new List<UserGroupResponse>();
        }

        public List<UserGroupResponse> UserGroups { get; set; }
        public MetaData MetaData { get; set; }
    }
}
