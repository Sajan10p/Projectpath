using Microsoft.AspNetCore.Mvc;
using Projectpath.Services;    

namespace Projectpath.Controllers
{
    public class HomeController : Controller
    {
        private readonly EmailService _emailService;

        public HomeController(EmailService emailService)
        {
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

       

        public IActionResult AccessDenied()
        {
            return View();
        }
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendEmailAsync(
                "sajanpathakleo.10@gmail.com",
                "ProjectPath Test Email",
                @"
                <h2>ProjectPath Email Test</h2>
                <p>If you received this email, SMTP integration is working.</p>
                "
            );

            return Content("Test email sent. Check your inbox/spam folder.");
        }
    }
}