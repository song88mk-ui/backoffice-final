
namespace Backoffice.Shared.Models.Requests
{
    public class UserRegisterRequest : UserResponse
    {
        public string RoleName { get; set; }
        public string Status { get; set; }
    }
}
