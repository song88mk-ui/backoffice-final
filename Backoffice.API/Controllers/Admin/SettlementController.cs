using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Controllers.Admin
{
    [ApiController]
    [Route("admin/settlement")]
    public class SettlementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
