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

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService, NotificationService notificationService, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
            _environment = environment;
        }

        public IActionResult Index() => RedirectToAction("PendingRegistrations");

        public async Task<IActionResult> PendingRegistrations()
        {
            var users = await _userManager.Users.Where(u => u.RegistrationStatus == "Pending" && !u.IsRegistrationApproved).OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsActive = true; user.IsRegistrationApproved = true; user.RegistrationStatus = "Approved"; user.RegistrationApprovedAt = DateTime.Now;
            await _userManager.UpdateAsync(user);
            if (!string.IsNullOrWhiteSpace(user.Email)) { try { await _emailService.SendEmailAsync(user.Email, "Registration Approved - ProjectPath", "Your account has been approved. You can now login to ProjectPath."); } catch { } }
            TempData["Success"] = "User approved successfully.";
            return RedirectToAction("PendingRegistrations");
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsActive = false; user.IsRegistrationApproved = false; user.RegistrationStatus = "Rejected";
            await _userManager.UpdateAsync(user);
            if (!string.IsNullOrWhiteSpace(user.Email)) { try { await _emailService.SendEmailAsync(user.Email, "Registration Rejected - ProjectPath", "Your registration application has been rejected. Please contact the administrator if you believe this is incorrect."); } catch { } }
            TempData["Success"] = "User rejected.";
            return RedirectToAction("PendingRegistrations");
        }

        public async Task<IActionResult> Users() => View(await _userManager.Users.ToListAsync());

        public async Task<IActionResult> Submissions()
        {
            var submissions = await _context.Submissions.Include(s => s.Student).Include(s => s.Assignment).ThenInclude(a => a.Project).Include(s => s.Assignment).ThenInclude(a => a.StudentGroup).OrderByDescending(s => s.SubmittedAt).ToListAsync();
            return View(submissions);
        }

        public async Task<IActionResult> DownloadSubmission(int id)
        {
            var submission = await _context.Submissions.FindAsync(id);
            if (submission == null || string.IsNullOrWhiteSpace(submission.FilePath)) return NotFound();
            var fullPath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();
            submission.Status = "Viewed";
            submission.ViewedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return PhysicalFile(fullPath, "application/octet-stream", submission.FileName);
        }

        public async Task<IActionResult> DashboardStats()
        {
            ViewBag.PendingRegistrations = await _userManager.Users.CountAsync(u => u.RegistrationStatus == "Pending");
            ViewBag.TotalProjects = await _context.Projects.CountAsync();
            ViewBag.PendingProjects = await _context.Projects.CountAsync(p => !p.IsApproved);
            ViewBag.TotalSubmissions = await _context.Submissions.CountAsync();
            ViewBag.UnviewedSubmissions = await _context.Submissions.CountAsync(s => s.Status == "Submitted");
            return View();
        }

        public async Task<IActionResult> Projects() => View(await _context.Projects.Include(p => p.Company).Include(p => p.StudentGroups).OrderByDescending(p => p.CreatedAt).ToListAsync());
        public async Task<IActionResult> PendingApprovals() => View(await _context.Projects.Include(p => p.Company).Where(p => !p.IsApproved).OrderByDescending(p => p.CreatedAt).ToListAsync());

        public async Task<IActionResult> ApprovalReview(int id)
        {
            var project = await _context.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();
            return View(new ApprovalReviewViewModel { Project = project, InternalNotes = project.AdminInternalNotes, MessageToCompany = project.AdminDecisionMessage });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveProject(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();
            project.IsApproved = true; project.Status = "Approved"; project.AdminInternalNotes = internalNotes; project.AdminDecisionMessage = messageToCompany;
            await _context.SaveChangesAsync();
            await _notificationService.CreateAsync(project.CompanyId, "Project Approved", $"{project.Title} has been approved.", "/Projects/MyProjects");
            if (!string.IsNullOrWhiteSpace(project.Company?.Email)) { try { await _emailService.SendEmailAsync(project.Company.Email, "Project Approved - ProjectPath", $"Your project '{project.Title}' has been approved.\n\nMessage: {messageToCompany}"); } catch { } }
            TempData["Success"] = "Project approved successfully.";
            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        public async Task<IActionResult> RejectProject(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();
            project.IsApproved = false; project.Status = "Rejected"; project.AdminInternalNotes = internalNotes; project.AdminDecisionMessage = messageToCompany;
            await _context.SaveChangesAsync();
            await _notificationService.CreateAsync(project.CompanyId, "Project Rejected", $"{project.Title} has been rejected.", "/Projects/MyProjects");
            if (!string.IsNullOrWhiteSpace(project.Company?.Email)) { try { await _emailService.SendEmailAsync(project.Company.Email, "Project Rejected - ProjectPath", $"Your project '{project.Title}' was rejected.\n\nMessage: {messageToCompany}"); } catch { } }
            TempData["Success"] = "Project rejected.";
            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        public async Task<IActionResult> RequestChanges(int id, string? internalNotes, string? messageToCompany)
        {
            var project = await _context.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();
            project.Status = "Under Review"; project.AdminInternalNotes = internalNotes; project.AdminDecisionMessage = messageToCompany;
            await _context.SaveChangesAsync();
            await _notificationService.CreateAsync(project.CompanyId, "Changes Requested", $"Changes requested for '{project.Title}'.", "/Projects/MyProjects");
            if (!string.IsNullOrWhiteSpace(project.Company?.Email)) { try { await _emailService.SendEmailAsync(project.Company.Email, "Changes Requested - ProjectPath", $"Changes requested for your project '{project.Title}'.\n\nMessage: {messageToCompany}"); } catch { } }
            TempData["Success"] = "Changes requested.";
            return RedirectToAction("PendingApprovals");
        }

        public async Task<IActionResult> Assignments()
        {
            ViewBag.Projects = await _context.Projects.Where(p => p.IsApproved).Include(p => p.StudentGroups).ToListAsync();
            ViewBag.Tutors = await _userManager.GetUsersInRoleAsync("Tutor");
            return View(await _context.Assignments.Include(a => a.Project).Include(a => a.StudentGroup).Include(a => a.Tutor).OrderByDescending(a => a.AssignedAt).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SaveTutorAssignment(int projectId, int studentGroupId, string tutorId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            var studentGroup = await _context.StudentGroups.Include(g => g.Members).ThenInclude(m => m.Student).FirstOrDefaultAsync(g => g.Id == studentGroupId);
            var tutor = await _userManager.FindByIdAsync(tutorId);
            if (project == null || studentGroup == null || tutor == null) { TempData["Error"] = "Invalid tutor assignment data."; return RedirectToAction("Assignments"); }
            var existingAssignment = await _context.Assignments.FirstOrDefaultAsync(a => a.ProjectId == projectId && a.StudentGroupId == studentGroupId);
            if (existingAssignment == null) _context.Assignments.Add(new Assignment { ProjectId = projectId, StudentGroupId = studentGroupId, TutorId = tutorId, AssignedAt = DateTime.Now, Status = "Assigned", CurrentProgressPercent = 0 });
            else { existingAssignment.TutorId = tutorId; existingAssignment.Status = "Assigned"; }
            project.Status = "Assigned";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tutor assignment saved successfully.";
            return RedirectToAction("Assignments");
        }

        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(await _context.Notifications.Where(n => n.UserId == user!.Id).OrderByDescending(n => n.CreatedAt).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user.Email == "admin@projectpath.com") return RedirectToAction("Users");
            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User deleted.";
            return RedirectToAction("Users");
        }
    }
}