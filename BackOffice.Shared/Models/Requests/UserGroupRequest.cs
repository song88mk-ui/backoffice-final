
using Backoffice.Shared.Enums;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class UserGroupRequest
    {
        public UserGroupRequest()
        {
            Claims = new List<ClaimModel>();
        }
        public string SearchText { get; set; } = "";
        public string Selection { get; set; } = "";
        public string UserGroupId { get; set; } = "";
        public string UserGroupName { get; set; } = "";
        public UserSorting SortBy { get; set; } = UserSorting.SortByCreateDateDesc;
        public List<ClaimModel> Claims { get; set; } = new List<ClaimModel>();
        public MetaData MetaData { get; set; } = new MetaData();
    }
}
