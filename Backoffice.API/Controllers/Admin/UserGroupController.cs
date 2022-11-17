using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Controllers.Admin
{
    [ApiController]
    [Route("admin/usergroup")]
    public class UserGroupController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
