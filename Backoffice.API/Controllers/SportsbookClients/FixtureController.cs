using Microsoft.AspNetCore.Mvc;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [ApiController]
    [Route("sbc/fixture")]
    public class FixtureController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
