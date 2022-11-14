using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.SportsbookClients
{
    public class TopLeaguesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
