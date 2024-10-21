
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class UserModel
    {
        public UserModel()
        {
            ClientCodes = new List<string>();
            Claims = new List<Claim>();
        }

        public string ConnectionId { get; set; }
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleId { get; set; }
        public string MainUserId { get; set; }
        public List<string> ClientCodes { get; set; }
        public List<Claim> Claims { get; set; }
    }
}
