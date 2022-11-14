using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.Administration
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
