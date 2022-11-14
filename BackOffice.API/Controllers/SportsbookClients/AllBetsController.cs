using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.SportsbookClients
{
    public class AllBetsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
