using Microsoft.AspNetCore.Mvc;

namespace BackOffice.API.Controllers.Administration
{
    public class UserGroupController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
