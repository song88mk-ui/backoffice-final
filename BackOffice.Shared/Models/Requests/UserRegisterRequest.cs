
using System.Collections.Generic;
using System.Security.Claims;

namespace Backoffice.Shared.Models.Requests
{
    public class UserRegisterRequest : UserResponse
    {
        public List<Claim>? UserClaims { get; set; }
        public string RoleName { get; set; }
        public string Status { get; set; }
    }
}
