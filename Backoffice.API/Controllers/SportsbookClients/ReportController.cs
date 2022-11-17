using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [ApiController]
    [Route("sbc/report")]
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
