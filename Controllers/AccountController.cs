using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projectpath.Models;
using Projectpath.Services;

namespace Projectpath.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
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
                EmailConfirmed = true,
                IsActive = false,
                IsRegistrationApproved = false,
                RegistrationStatus = "Pending"
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(user.Email, "Registration Received - ProjectPath", "Your registration application has been received. You will be able to login after the admin approves your account.");
                    }
                    catch { }
                }

                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins.Where(a => !string.IsNullOrWhiteSpace(a.Email)))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(admin.Email!, "New Registration Pending - ProjectPath", $"{user.FullName} has applied to register as {user.UserRole}. Please login as admin to approve or reject this registration.");
                    }
                    catch { }
                }

                TempData["Success"] = "Registration submitted successfully. Please wait for admin approval before logging in.";
                return RedirectToAction("Login");
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
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            if (!user.IsActive || !user.IsRegistrationApproved || user.RegistrationStatus != "Approved")
            {
                ModelState.AddModelError("", "Your account is pending admin approval. You can login after the admin approves your registration.");
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
                "Student" => RedirectToAction("Approved", "Projects"),
                "Tutor" => RedirectToAction("TutorDashboard", "Dashboard"),
                "Company" => RedirectToAction("CompanyDashboard", "Dashboard"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}