using Microsoft.AspNetCore.Mvc;

namespace Projectpath.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

       

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}