using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.Administration
{
    public class CustomerServiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
