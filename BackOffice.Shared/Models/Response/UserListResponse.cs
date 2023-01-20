
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class UserListResponse
    {
        public UserListResponse()
        {
            Users = new List<UserResponse>();
        }

        public List<UserResponse> Users { get; set; }
        public MetaData MetaData { get; set; }
    }
}
