
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class UserListResponse
    {
        public UserListResponse()
        {
            Users = new List<UserModel>();
        }

        public List<UserModel> Users { get; set; }
    }
}
