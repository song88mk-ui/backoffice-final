using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Controllers.Admin
{
    [ApiController]
    [Route("admin/user")]
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
