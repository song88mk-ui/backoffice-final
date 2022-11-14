using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.SportsbookClients
{
    public class TopFixturesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
