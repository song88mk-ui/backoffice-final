using System.ComponentModel.DataAnnotations;

namespace Backoffice.Shared.Models
{
    public class LogInModel
    {
        [Required(ErrorMessage = "Username is required to log in, please enter it above.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Password is required to log in, please enter it above.")]
        public string Password { get; set; }
    }
}
