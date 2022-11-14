using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.SportsbookClients
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
