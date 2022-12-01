using System.ComponentModel.DataAnnotations;

namespace Backoffice.Web.Models
{
    public class UserModel
    {
        [Required(ErrorMessage = "Username is required to log in, please enter it above.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Password is required to log in, please enter it above.")]
        public string Password { get; set; }
    }
}
