using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [ApiController]
    [Route("sbc/dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
