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
            return RedirectToAction("PendingApprovals");
        }

        // ================= USERS =================
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

        // ================= PROJECT LIST =================
        public async Task<IActionResult> Projects()
        {
            var projects = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.StudentGroups)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // ================= PENDING APPROVALS =================
        public async Task<IActionResult> PendingApprovals()
        {
            var projects = await _context.Projects
                .Include(p => p.Company)
                .Where(p => !p.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // ================= APPROVAL REVIEW PAGE =================
        public async Task<IActionResult> ApprovalReview(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var vm = new ApprovalReviewViewModel
            {
                Project = project,
                InternalNotes = project.AdminInternalNotes,
                MessageToCompany = project.AdminDecisionMessage
            };

            return View(vm);
        }

        // ================= APPROVE =================
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

            // Notification
            await _notificationService.CreateAsync(
                project.CompanyId,
                "Project Approved",
                $"{project.Title} has been approved.",
                "/Projects/MyProjects"
            );

            // Email
            if (!string.IsNullOrWhiteSpace(project.Company?.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Project Approved - ProjectPath",
                        $"Your project '{project.Title}' has been approved.\n\nMessage: {messageToCompany}"
                    );
                }
                catch { }
            }

            TempData["Success"] = "Project approved successfully.";
            return RedirectToAction("PendingApprovals");
        }

        // ================= REJECT =================
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

            if (!string.IsNullOrWhiteSpace(project.Company?.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Project Rejected - ProjectPath",
                        $"Your project '{project.Title}' was rejected.\n\nMessage: {messageToCompany}"
                    );
                }
                catch { }
            }

            TempData["Success"] = "Project rejected.";
            return RedirectToAction("PendingApprovals");
        }

        // ================= REQUEST CHANGES =================
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
                $"Changes requested for '{project.Title}'.",
                "/Projects/MyProjects"
            );

            if (!string.IsNullOrWhiteSpace(project.Company?.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Changes Requested - ProjectPath",
                        $"Changes requested for your project '{project.Title}'.\n\nMessage: {messageToCompany}"
                    );
                }
                catch { }
            }

            TempData["Success"] = "Changes requested.";
            return RedirectToAction("PendingApprovals");
        }


        // ================= ASSIGNMENTS =================
        public async Task<IActionResult> Assignments()
        {
            ViewBag.Projects = await _context.Projects
                .Where(p => p.IsApproved)
                .Include(p => p.StudentGroups)
                .ToListAsync();

            ViewBag.Tutors = await _userManager.GetUsersInRoleAsync("Tutor");

            var assignments = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)
                .Include(a => a.Tutor)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            return View(assignments);
        }

        [HttpPost]
        public async Task<IActionResult> SaveTutorAssignment(int projectId, int studentGroupId, string tutorId)
        {
            if (projectId <= 0 || studentGroupId <= 0 || string.IsNullOrWhiteSpace(tutorId))
            {
                TempData["Error"] = "Invalid tutor assignment data.";
                return RedirectToAction("Assignments");
            }

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                TempData["Error"] = "Project not found.";
                return RedirectToAction("Assignments");
            }

            var studentGroup = await _context.StudentGroups
                .Include(g => g.Members)
                    .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(g => g.Id == studentGroupId);

            if (studentGroup == null)
            {
                TempData["Error"] = "Student group not found.";
                return RedirectToAction("Assignments");
            }

            var tutor = await _userManager.FindByIdAsync(tutorId);
            if (tutor == null)
            {
                TempData["Error"] = "Tutor not found.";
                return RedirectToAction("Assignments");
            }

            var tutorRoles = await _userManager.GetRolesAsync(tutor);
            if (!tutorRoles.Contains("Tutor"))
            {
                TempData["Error"] = "Selected user is not a tutor.";
                return RedirectToAction("Assignments");
            }

            var existingAssignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.StudentGroupId == studentGroupId);

            if (existingAssignment == null)
            {
                existingAssignment = new Assignment
                {
                    ProjectId = projectId,
                    StudentGroupId = studentGroupId,
                    TutorId = tutorId,
                    AssignedAt = DateTime.Now,
                    Status = "Assigned",
                    CurrentProgressPercent = 0
                };

                _context.Assignments.Add(existingAssignment);
            }
            else
            {
                existingAssignment.TutorId = tutorId;
                existingAssignment.Status = "Assigned";
            }

            project.Status = "Assigned";

            await _context.SaveChangesAsync();

            // Notify tutor
            await _notificationService.CreateAsync(
                tutor.Id,
                "New Tutor Assignment",
                $"You have been assigned to supervise the group '{studentGroup.GroupName}' for project '{project.Title}'.",
                "/Dashboard/TutorDashboard"
            );

            if (!string.IsNullOrWhiteSpace(tutor.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        tutor.Email,
                        "New Tutor Assignment - ProjectPath",
                        $"You have been assigned to supervise the group '{studentGroup.GroupName}' for project '{project.Title}'."
                    );
                }
                catch { }
            }

            // Notify students in the group
            foreach (var member in studentGroup.Members)
            {
                await _notificationService.CreateAsync(
                    member.StudentId,
                    "Tutor Assigned",
                    $"Tutor {tutor.FullName} has been assigned to your group for project '{project.Title}'.",
                    "/Dashboard/StudentDashboard"
                );

                if (!string.IsNullOrWhiteSpace(member.Student?.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            member.Student.Email,
                            "Tutor Assigned - ProjectPath",
                            $"Tutor {tutor.FullName} has been assigned to your group for project '{project.Title}'."
                        );
                    }
                    catch { }
                }
            }

            TempData["Success"] = "Tutor assignment saved successfully.";
            return RedirectToAction("Assignments");
        }



        // ================= NOTIFICATIONS =================
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user!.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // ================= DELETE USER =================
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