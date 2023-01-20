using Backoffice.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace Backoffice.Web.Models
{
    public class UserRegisterModel
    {
        public string UserId { get; set; }
        public string? NickName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } = "";
        public int AppId { get; set; }
        public string AppName { get; set; }
        public string Status { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public List<ClaimModel> Claims { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
