using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projectpath.Data;
using Projectpath.Models;
using Projectpath.Services;    

namespace Projectpath.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService )
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var vm = new AdminDashboardViewModel
            {
                PendingProjects = await _context.Projects.CountAsync(p => p.Status == "Pending"),
                ApprovedProjects = await _context.Projects.CountAsync(p => p.IsApproved),
                ActiveAssignments = await _context.Assignments.CountAsync(),
                CompletedProjects = await _context.Projects.CountAsync(p => p.Status == "Completed"),
                RecentPendingProjects = await _context.Projects
                    .Include(p => p.Company)
                    .Where(p => p.Status == "Pending" || p.Status == "Under Review")
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            vm.RecentActivities = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => n.Title)
                .Take(5)
                .ToListAsync();

            return View(vm);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentDashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var membership = await _context.GroupMembers
                .Include(m => m.StudentGroup)!
                    .ThenInclude(g => g.Project)
                .FirstOrDefaultAsync(m => m.StudentId == user!.Id);

            Assignment? assignment = null;
            List<ProgressUpdate> updates = new();

            if (membership != null)
            {
                assignment = await _context.Assignments
                    .Include(a => a.Project)
                        .ThenInclude(p => p!.Company)
                    .Include(a => a.Tutor)
                    .FirstOrDefaultAsync(a => a.StudentGroupId == membership.StudentGroupId);

                if (assignment != null)
                {
                    updates = await _context.ProgressUpdates
                        .Include(p => p.Tutor)
                        .Include(p => p.Student)
                        .Where(p => p.AssignmentId == assignment.Id &&
                                   (!p.IsIndividualFeedback || p.StudentId == user.Id))
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
                }
            }

            return View(new StudentDashboardViewModel
            {
                Assignment = assignment,
                Updates = updates
            });
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> TutorDashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var assignments = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)!
                    .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.Student)
                .Where(a => a.TutorId == user!.Id)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            return View(new TutorDashboardViewModel
            {
                Assignments = assignments,
                ActiveProjectsCount = assignments.Count,
                AssignedStudentsCount = assignments.Sum(a => a.StudentGroup?.Members.Count ?? 0),
                PendingUpdatesCount = assignments.Count(a => a.CurrentProgressPercent < 100)
            });
        }

        [Authorize(Roles = "Company")]
        public async Task<IActionResult> CompanyDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects
                .Where(p => p.CompanyId == user!.Id)
                .Include(p => p.Assignments)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("AdminDashboard");
            if (User.IsInRole("Student")) return RedirectToAction("StudentDashboard");
            if (User.IsInRole("Tutor")) return RedirectToAction("TutorDashboard");
            if (User.IsInRole("Company")) return RedirectToAction("CompanyDashboard");
            return RedirectToAction("Index", "Home");

        }
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendEmailAsync(
                "sajanpathakleo.10@gmail.com",
                "Test Email",
                "Your email system is working ✅"
            );

            return Content("Email sent!");
        }
    }
}