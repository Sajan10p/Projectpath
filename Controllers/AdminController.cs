using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projectpath.Data;
using Projectpath.Models;
using Projectpath.Services;

namespace Projectpath.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<UserWithRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserWithRoleViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    StudentNumber = user.StudentNumber,
                    TutorNumber = user.TutorNumber
                });
            }

            return View(result);
        }

        public async Task<IActionResult> Projects()
        {
            var projects = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.StudentGroups)
                    .ThenInclude(g => g.Members)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveProject(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            project.IsApproved = true;
            project.Status = "Approved";
            project.AdminInternalNotes = internalNotes;
            project.AdminDecisionMessage = messageToCompany;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                project.CompanyId,
                "Project Approved",
                $"{project.Title} has been approved.",
                "/Projects/MyProjects"
            );

            if (project.Company != null && !string.IsNullOrWhiteSpace(project.Company.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Project Approved - ProjectPath",
                        $"Your project '{project.Title}' has been approved."
                    );
                }
                catch { }
            }

            TempData["Success"] = "Project approved successfully.";
            return RedirectToAction("Projects");
        }

        [HttpPost]
        public async Task<IActionResult> RejectProject(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            project.IsApproved = false;
            project.Status = "Rejected";
            project.AdminInternalNotes = internalNotes;
            project.AdminDecisionMessage = messageToCompany;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                project.CompanyId,
                "Project Rejected",
                $"{project.Title} has been rejected.",
                "/Projects/MyProjects"
            );

            if (project.Company != null && !string.IsNullOrWhiteSpace(project.Company.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Project Rejected - ProjectPath",
                        $"Your project '{project.Title}' was rejected. Please review the admin notes."
                    );
                }
                catch { }
            }

            TempData["Success"] = "Project rejected.";
            return RedirectToAction("Projects");
        }

        [HttpPost]
        public async Task<IActionResult> RequestChanges(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            project.Status = "Under Review";
            project.AdminInternalNotes = internalNotes;
            project.AdminDecisionMessage = messageToCompany;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                project.CompanyId,
                "Changes Requested",
                $"Changes have been requested for '{project.Title}'.",
                "/Projects/MyProjects"
            );

            if (project.Company != null && !string.IsNullOrWhiteSpace(project.Company.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Changes Requested - ProjectPath",
                        $"Changes have been requested for your project '{project.Title}'. Please log in and review admin notes."
                    );
                }
                catch { }
            }

            TempData["Success"] = "Changes requested from company.";
            return RedirectToAction("Projects");
        }

        public async Task<IActionResult> Assignments()
        {
            ViewBag.Projects = await _context.Projects
                .Where(p => p.IsApproved)
                .Include(p => p.StudentGroups)
                    .ThenInclude(g => g.Members)
                        .ThenInclude(m => m.Student)
                .ToListAsync();

            ViewBag.Tutors = await _userManager.GetUsersInRoleAsync("Tutor");

            var assignments = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)!
                    .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.Student)
                .Include(a => a.Tutor)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            return View(assignments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAssignment(AssignProjectViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Assignments");

            var exists = await _context.Assignments.AnyAsync(a => a.StudentGroupId == model.StudentGroupId);
            if (exists)
                return RedirectToAction("Assignments");

            var assignment = new Assignment
            {
                ProjectId = model.ProjectId,
                StudentGroupId = model.StudentGroupId,
                TutorId = model.TutorId,
                AssignedAt = DateTime.Now,
                Status = "Assigned"
            };

            _context.Assignments.Add(assignment);

            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project != null)
                project.Status = "Assigned";

            await _context.SaveChangesAsync();

            var tutor = await _userManager.FindByIdAsync(model.TutorId);
            if (tutor != null)
            {
                await _notificationService.CreateAsync(
                    tutor.Id,
                    "New Tutor Assignment",
                    "You have been assigned to a project group.",
                    "/Dashboard/TutorDashboard"
                );

                if (!string.IsNullOrWhiteSpace(tutor.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            tutor.Email,
                            "New Tutor Assignment - ProjectPath",
                            "You have been assigned to a project group. Please log in to review details."
                        );
                    }
                    catch { }
                }
            }

            var groupMembers = await _context.GroupMembers
                .Include(gm => gm.Student)
                .Where(gm => gm.StudentGroupId == model.StudentGroupId)
                .ToListAsync();

            foreach (var gm in groupMembers)
            {
                await _notificationService.CreateAsync(
                    gm.StudentId,
                    "Tutor Assigned",
                    "A tutor has been assigned to your group.",
                    "/Dashboard/StudentDashboard"
                );

                if (!string.IsNullOrWhiteSpace(gm.Student?.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            gm.Student.Email,
                            "Tutor Assigned - ProjectPath",
                            "A tutor has been assigned to your group. Please log in to review details."
                        );
                    }
                    catch { }
                }
            }

            TempData["Success"] = "Assignment created successfully.";
            return RedirectToAction("Assignments");
        }

        [HttpPost]
        public async Task<IActionResult> SaveTutorAssignment(int projectId, int studentGroupId, string tutorId)
        {
            var existing = await _context.Assignments
                .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.StudentGroupId == studentGroupId);

            if (existing == null)
            {
                existing = new Assignment
                {
                    ProjectId = projectId,
                    StudentGroupId = studentGroupId,
                    TutorId = tutorId,
                    AssignedAt = DateTime.Now,
                    Status = "Assigned"
                };
                _context.Assignments.Add(existing);
            }
            else
            {
                existing.TutorId = tutorId;
            }

            await _context.SaveChangesAsync();

            var tutor = await _userManager.FindByIdAsync(tutorId);
            if (tutor != null)
            {
                await _notificationService.CreateAsync(
                    tutor.Id,
                    "Tutor Assignment Updated",
                    "You have been assigned to a project group.",
                    "/Dashboard/TutorDashboard"
                );

                if (!string.IsNullOrWhiteSpace(tutor.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            tutor.Email,
                            "Tutor Assignment - ProjectPath",
                            "You have been assigned to a project group. Please log in to review details."
                        );
                    }
                    catch { }
                }
            }

            var groupMembers = await _context.GroupMembers
                .Include(gm => gm.Student)
                .Where(gm => gm.StudentGroupId == studentGroupId)
                .ToListAsync();

            foreach (var gm in groupMembers)
            {
                await _notificationService.CreateAsync(
                    gm.StudentId,
                    "Tutor Assigned",
                    "A tutor has been assigned to your group.",
                    "/Dashboard/StudentDashboard"
                );

                if (!string.IsNullOrWhiteSpace(gm.Student?.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            gm.Student.Email,
                            "Tutor Assigned - ProjectPath",
                            "A tutor has been assigned to your group. Please log in to review details."
                        );
                    }
                    catch { }
                }
            }

            TempData["Success"] = "Tutor assignment saved.";
            return RedirectToAction("Assignments");
        }

        public async Task<IActionResult> Progress()
        {
            var progress = await _context.ProgressUpdates
                .Include(p => p.Assignment)!
                    .ThenInclude(a => a.Project)
                .Include(p => p.Assignment)!
                    .ThenInclude(a => a.StudentGroup)!
                    .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.Student)
                .Include(p => p.Tutor)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(progress);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Email == "admin@projectpath.com")
                return RedirectToAction("Users");

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User deleted.";
            return RedirectToAction("Users");
        }
    }
}