using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projectpath.Data;
using Projectpath.Models;

namespace Projectpath.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Company")]
        public async Task<IActionResult> MyProjects()
        {
            var user = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects.Where(p => p.CompanyId == user!.Id).OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(projects);
        }

        [Authorize(Roles = "Company")]
        [HttpGet]
        public IActionResult Create() => View();

        [Authorize(Roles = "Company")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);

            _context.Projects.Add(new Project
            {
                Title = model.Title,
                Description = model.Description,
                Requirements = model.Requirements,
                Program = model.Program,
                ProjectType = model.ProjectType,
                CompanyId = user!.Id,
                IsApproved = false,
                Status = "Pending",
                ContactPersonName = model.ContactPersonName,
                ContactPhone = model.ContactPhone,
                ContactPersonEmail = model.ContactPersonEmail,
                ExpectedDurationWeeks = model.ExpectedDurationWeeks,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Project submitted successfully.";
            return RedirectToAction("MyProjects");
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Approved()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AlreadyInGroup = await _context.GroupMembers.AnyAsync(m => m.StudentId == user!.Id);
            var projects = await _context.Projects.Include(p => p.Company).Include(p => p.StudentGroups).Where(p => p.IsApproved).OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(projects);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyProject()
        {
            var user = await _userManager.GetUserAsync(User);
            var membership = await _context.GroupMembers
                .Include(m => m.StudentGroup)!.ThenInclude(g => g.Project)!.ThenInclude(p => p.Company)
                .Include(m => m.StudentGroup)!.ThenInclude(g => g.Project)!.ThenInclude(p => p.Assignments).ThenInclude(a => a.ProgressUpdates)
                .Include(m => m.StudentGroup)!.ThenInclude(g => g.Project)!.ThenInclude(p => p.Assignments).ThenInclude(a => a.Tutor)
                .FirstOrDefaultAsync(m => m.StudentId == user!.Id);

            if (membership == null || membership.StudentGroup == null || membership.StudentGroup.Project == null)
            {
                TempData["Error"] = "You are not assigned to any project yet.";
                return RedirectToAction("StudentDashboard", "Dashboard");
            }

            var project = membership.StudentGroup.Project;
            var assignment = project.Assignments.FirstOrDefault(a => a.StudentGroupId == membership.StudentGroupId);
            ViewBag.ProgressPercent = assignment?.CurrentProgressPercent ?? 0;
            ViewBag.Assignment = assignment;
            return View(project);
        }
    }
}