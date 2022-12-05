
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

        public string Id { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
        public List<string> ClientCodes { get; set; }
        public List<Claim> Claims { get; set; }
    }
}
