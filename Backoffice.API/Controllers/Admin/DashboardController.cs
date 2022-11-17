using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Controllers.Admin
{
    [ApiController]
    [Route("admin/dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
