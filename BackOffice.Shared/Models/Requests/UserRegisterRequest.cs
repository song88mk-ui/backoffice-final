
namespace Backoffice.Shared.Models.Requests
{
    public class UserRegisterRequest : UserModel
    {
        public string RoleName { get; set; }
        public string Status { get; set; }
    }
}
