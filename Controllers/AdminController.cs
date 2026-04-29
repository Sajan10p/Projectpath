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
        private readonly IWebHostEnvironment _environment;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            NotificationService notificationService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return RedirectToAction("PendingRegistrations");
        }

        // ============================================================
        // ADMIN: PENDING USER REGISTRATIONS
        // ============================================================

        public async Task<IActionResult> PendingRegistrations()
        {
            var users = await _userManager.Users
                .Where(u => u.RegistrationStatus == "Pending" && !u.IsRegistrationApproved)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.IsActive = true;
            user.IsRegistrationApproved = true;
            user.RegistrationStatus = "Approved";
            user.RegistrationApprovedAt = DateTime.Now;

            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Registration Approved - ProjectPath",
                        $@"
Hello {user.FullName},

Your ProjectPath account has been approved.

You can now login using your registered email address.

Regards,
ProjectPath Team
");
                }
                catch
                {
                    // Email failure should not stop approval.
                }
            }

            TempData["Success"] = "User approved successfully.";
            return RedirectToAction("PendingRegistrations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.IsActive = false;
            user.IsRegistrationApproved = false;
            user.RegistrationStatus = "Rejected";

            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Registration Rejected - ProjectPath",
                        $@"
Hello {user.FullName},

Your ProjectPath registration application has been rejected.

Please contact the administrator if you believe this is incorrect.

Regards,
ProjectPath Team
");
                }
                catch
                {
                    // Email failure should not stop rejection.
                }
            }

            TempData["Success"] = "User rejected.";
            return RedirectToAction("PendingRegistrations");
        }

        // ============================================================
        // ADMIN: USER MANAGEMENT
        // ============================================================

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
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? user.UserRole,
                    StudentNumber = user.StudentNumber,
                    TutorNumber = user.TutorNumber
                });
            }

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            if (user.Email == "admin@projectpath.com")
            {
                TempData["Error"] = "Default admin account cannot be deleted.";
                return RedirectToAction("Users");
            }

            await _userManager.DeleteAsync(user);

            TempData["Success"] = "User deleted.";
            return RedirectToAction("Users");
        }

        // ============================================================
        // ADMIN: DASHBOARD STATS
        // ============================================================

        public async Task<IActionResult> DashboardStats()
        {
            ViewBag.PendingRegistrations = await _userManager.Users
                .CountAsync(u => u.RegistrationStatus == "Pending");

            ViewBag.TotalProjects = await _context.Projects.CountAsync();

            ViewBag.PendingProjects = await _context.Projects
                .CountAsync(p => !p.IsApproved);

            ViewBag.TotalSubmissions = await _context.Submissions.CountAsync();

            ViewBag.UnviewedSubmissions = await _context.Submissions
                .CountAsync(s => s.Status == "Submitted");

            return View();
        }

        // ============================================================
        // ADMIN: PROJECTS
        // ============================================================

        public async Task<IActionResult> Projects()
        {
            var projects = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.StudentGroups)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        public async Task<IActionResult> PendingApprovals()
        {
            var projects = await _context.Projects
                .Include(p => p.Company)
                .Where(p => !p.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        public async Task<IActionResult> ApprovalReview(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            return View(new ApprovalReviewViewModel
            {
                Project = project,
                InternalNotes = project.AdminInternalNotes,
                MessageToCompany = project.AdminDecisionMessage
            });
        }

        // ============================================================
        // ADMIN: APPROVE PROJECT
        // Sends email + notification to company
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProject(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            project.IsApproved = true;
            project.Status = "Approved";
            project.AdminInternalNotes = internalNotes;
            project.AdminDecisionMessage = messageToCompany;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                project.CompanyId,
                "Project Approved",
                $"Your project '{project.Title}' has been approved.",
                "/Projects/MyProjects"
            );

            if (!string.IsNullOrWhiteSpace(project.Company?.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Project Approved - ProjectPath",
                        $@"
Hello {project.Company.FullName},

Your project has been approved.

Project: {project.Title}

Admin Message:
{messageToCompany ?? "No additional message."}

You can now view the project status from your ProjectPath dashboard.

Regards,
ProjectPath Team
");
                }
                catch
                {
                    // Email failure should not stop approval.
                }
            }

            TempData["Success"] = "Project approved successfully.";
            return RedirectToAction("PendingApprovals");
        }

        // ============================================================
        // ADMIN: REJECT PROJECT
        // Sends email + notification to company
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProject(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            project.IsApproved = false;
            project.Status = "Rejected";
            project.AdminInternalNotes = internalNotes;
            project.AdminDecisionMessage = messageToCompany;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                project.CompanyId,
                "Project Rejected",
                $"Your project '{project.Title}' has been rejected.",
                "/Projects/MyProjects"
            );

            if (!string.IsNullOrWhiteSpace(project.Company?.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Project Rejected - ProjectPath",
                        $@"
Hello {project.Company.FullName},

Your project has been rejected.

Project: {project.Title}

Reason / Admin Message:
{messageToCompany ?? "No reason provided."}

Please review the message and edit your project if needed.

Regards,
ProjectPath Team
");
                }
                catch
                {
                    // Email failure should not stop rejection.
                }
            }

            TempData["Success"] = "Project rejected.";
            return RedirectToAction("PendingApprovals");
        }

        // ============================================================
        // ADMIN: REQUEST PROJECT CHANGES
        // Sends email + notification to company
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChanges(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            project.IsApproved = false;
            project.Status = "Changes Requested";
            project.AdminInternalNotes = internalNotes;
            project.AdminDecisionMessage = messageToCompany;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                project.CompanyId,
                "Changes Requested",
                $"Changes have been requested for your project '{project.Title}'.",
                "/Projects/MyProjects"
            );

            if (!string.IsNullOrWhiteSpace(project.Company?.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        project.Company.Email,
                        "Changes Requested - ProjectPath",
                        $@"
Hello {project.Company.FullName},

The admin has requested changes for your project.

Project: {project.Title}

Requested Changes:
{messageToCompany ?? "No message provided."}

Please edit your project and submit it again.

Regards,
ProjectPath Team
");
                }
                catch
                {
                    // Email failure should not stop workflow.
                }
            }

            TempData["Success"] = "Changes requested.";
            return RedirectToAction("PendingApprovals");
        }

        // ============================================================
        // ADMIN: TUTOR ASSIGNMENTS
        // ============================================================

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTutorAssignment(int projectId, int studentGroupId, string tutorId)
        {
            var project = await _context.Projects.FindAsync(projectId);

            var studentGroup = await _context.StudentGroups
                .Include(g => g.Members)
                    .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(g => g.Id == studentGroupId);

            var tutor = await _userManager.FindByIdAsync(tutorId);

            if (project == null || studentGroup == null || tutor == null)
            {
                TempData["Error"] = "Invalid tutor assignment data.";
                return RedirectToAction("Assignments");
            }

            var isTutor = await _userManager.IsInRoleAsync(tutor, "Tutor");

            if (!isTutor)
            {
                TempData["Error"] = "Selected user is not a tutor.";
                return RedirectToAction("Assignments");
            }

            var existingAssignment = await _context.Assignments
                .FirstOrDefaultAsync(a =>
                    a.ProjectId == projectId &&
                    a.StudentGroupId == studentGroupId);

            if (existingAssignment == null)
            {
                _context.Assignments.Add(new Assignment
                {
                    ProjectId = projectId,
                    StudentGroupId = studentGroupId,
                    TutorId = tutorId,
                    AssignedAt = DateTime.Now,
                    Status = "Assigned",
                    CurrentProgressPercent = 0
                });
            }
            else
            {
                existingAssignment.TutorId = tutorId;
                existingAssignment.Status = "Assigned";
            }

            project.Status = "Assigned";

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                tutorId,
                "New Tutor Assignment",
                $"You have been assigned to supervise '{project.Title}'.",
                "/Tutor/AssignedStudents"
            );

            if (!string.IsNullOrWhiteSpace(tutor.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        tutor.Email,
                        "New Tutor Assignment - ProjectPath",
                        $@"
Hello {tutor.FullName},

You have been assigned to supervise a student group.

Project: {project.Title}

Please login to ProjectPath to view the assigned students.

Regards,
ProjectPath Team
");
                }
                catch
                {
                    // Email failure should not stop assignment.
                }
            }

            foreach (var member in studentGroup.Members)
            {
                await _notificationService.CreateAsync(
                    member.StudentId,
                    "Tutor Assigned",
                    $"A tutor has been assigned to your project '{project.Title}'.",
                    "/Projects/MyProject"
                );

                if (!string.IsNullOrWhiteSpace(member.Student?.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            member.Student.Email,
                            "Tutor Assigned - ProjectPath",
                            $@"
Hello {member.Student.FullName},

A tutor has been assigned to your project.

Project: {project.Title}
Tutor: {tutor.FullName}

Regards,
ProjectPath Team
");
                    }
                    catch
                    {
                        // Email failure should not stop assignment.
                    }
                }
            }

            TempData["Success"] = "Tutor assignment saved successfully.";
            return RedirectToAction("Assignments");
        }

        // ============================================================
        // ADMIN: SUBMISSIONS
        // ============================================================

        public async Task<IActionResult> Submissions()
        {
            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Project)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.StudentGroup)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            return View(submissions);
        }

        public async Task<IActionResult> DownloadSubmission(int id)
        {
            var submission = await _context.Submissions.FindAsync(id);

            if (submission == null || string.IsNullOrWhiteSpace(submission.FilePath))
                return NotFound();

            var fullPath = Path.Combine(
                _environment.WebRootPath,
                submission.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
            );

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            submission.Status = "Viewed";
            submission.ViewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return PhysicalFile(fullPath, "application/octet-stream", submission.FileName);
        }

        // ============================================================
        // ADMIN: NOTIFICATIONS
        // ============================================================

        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }
    }
}