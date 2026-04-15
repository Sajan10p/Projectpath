using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projectpath.Models;

namespace Projectpath.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role == "Student" && string.IsNullOrWhiteSpace(model.StudentNumber))
                ModelState.AddModelError("StudentNumber", "Student ID is required for students.");

            if (model.Role == "Tutor" && string.IsNullOrWhiteSpace(model.TutorNumber))
                ModelState.AddModelError("TutorNumber", "Tutor ID is required for tutors.");

            if (!ModelState.IsValid)
                return View(model);

            if (model.Role == "Admin")
            {
                ModelState.AddModelError("", "Admin registration is not allowed.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email,
                UserRole = model.Role,
                StudentNumber = model.Role == "Student" ? model.StudentNumber : null,
                TutorNumber = model.Role == "Tutor" ? model.TutorNumber : null,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                await _signInManager.SignInAsync(user, false);
                return RedirectToRoleDashboard(model.Role);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
                if (role != null)
                    return RedirectToRoleDashboard(role);
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        private RedirectToActionResult RedirectToRoleDashboard(string role)
        {
            return role switch
            {
                "Admin" => RedirectToAction("AdminDashboard", "Dashboard"),
                "Student" => RedirectToAction("StudentDashboard", "Dashboard"),
                "Tutor" => RedirectToAction("TutorDashboard", "Dashboard"),
                "Company" => RedirectToAction("CompanyDashboard", "Dashboard"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}