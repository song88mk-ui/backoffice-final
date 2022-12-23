
using System.Collections.Generic;
using System.Security.Claims;

namespace Backoffice.Shared.Models.Requests
{
    public class UserRegisterRequest : UserResponse
    {
        public string RoleName { get; set; }
    }
}
