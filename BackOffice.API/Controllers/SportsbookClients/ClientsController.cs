using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.SportsbookClients
{
    public class ClientsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
