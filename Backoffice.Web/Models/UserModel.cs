
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class UserModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public List<string> ClientCodes { get; set; }
        public List<Claim> Claims { get; set; }
    }
}
