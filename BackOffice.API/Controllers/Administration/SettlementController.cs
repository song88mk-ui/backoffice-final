using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.Administration
{
    public class SettlementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
