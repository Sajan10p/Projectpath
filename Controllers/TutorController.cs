using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projectpath.Data;
using Projectpath.Models;
using Projectpath.Services;

namespace Projectpath.Controllers
{
    [Authorize(Roles = "Tutor")]
    public class TutorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;
        private readonly IWebHostEnvironment _environment;

        public TutorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService, NotificationService notificationService, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
            _environment = environment;
        }

        public async Task<IActionResult> AssignedStudents()
        {
            var user = await _userManager.GetUserAsync(User);
            var assignments = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)!.ThenInclude(g => g.Leader)
                .Include(a => a.StudentGroup)!.ThenInclude(g => g.Members).ThenInclude(m => m.Student)
                .Where(a => a.TutorId == user!.Id)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
            return View(assignments);
        }

        public async Task<IActionResult> Submissions()
        {
            var tutor = await _userManager.GetUserAsync(User);
            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment).ThenInclude(a => a.Project)
                .Include(s => s.Assignment).ThenInclude(a => a.StudentGroup)
                .Where(s => s.Assignment.TutorId == tutor!.Id)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
            return View(submissions);
        }

        public async Task<IActionResult> DownloadSubmission(int id)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == id && s.Assignment.TutorId == tutor!.Id);

            if (submission == null || string.IsNullOrWhiteSpace(submission.FilePath)) return NotFound();

            var fullPath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            submission.Status = "Viewed";
            submission.ViewedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return PhysicalFile(fullPath, "application/octet-stream", submission.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> AddProgress(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var assignment = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)!.ThenInclude(g => g.Members).ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TutorId == user!.Id);
            if (assignment == null) return NotFound();
            ViewBag.Assignment = assignment;
            return View(new TutorProgressViewModel { AssignmentId = assignmentId, ProgressPercent = assignment.CurrentProgressPercent });
        }

        [HttpPost]
        public async Task<IActionResult> AddProgress(TutorProgressViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var assignment = await _context.Assignments
                .Include(a => a.StudentGroup)!.ThenInclude(g => g.Members).ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(a => a.Id == model.AssignmentId && a.TutorId == user!.Id);
            if (assignment == null) return NotFound();

            if (model.IsIndividualFeedback)
            {
                if (string.IsNullOrWhiteSpace(model.StudentId) || !assignment.StudentGroup!.Members.Any(m => m.StudentId == model.StudentId))
                    ModelState.AddModelError("StudentId", "Select a valid student.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Assignment = await _context.Assignments.Include(a => a.Project).Include(a => a.StudentGroup)!.ThenInclude(g => g.Members).ThenInclude(m => m.Student).FirstOrDefaultAsync(a => a.Id == model.AssignmentId);
                return View(model);
            }

            assignment.CurrentProgressPercent = model.ProgressPercent;
            _context.ProgressUpdates.Add(new ProgressUpdate { AssignmentId = model.AssignmentId, TutorId = user!.Id, Note = model.Note, IsIndividualFeedback = model.IsIndividualFeedback, StudentId = model.IsIndividualFeedback ? model.StudentId : null, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();

            var members = await _context.GroupMembers.Include(m => m.Student).Where(m => m.StudentGroupId == assignment.StudentGroupId).ToListAsync();
            foreach (var member in members)
            {
                await _notificationService.CreateAsync(member.StudentId, "Progress Updated", $"Project progress was updated to {assignment.CurrentProgressPercent}%.", "/Dashboard/StudentDashboard");
                if (!string.IsNullOrWhiteSpace(member.Student?.Email))
                {
                    try { await _emailService.SendEmailAsync(member.Student.Email, "Project Progress Updated - ProjectPath", $"Progress for your project has been updated to {assignment.CurrentProgressPercent}%."); } catch { }
                }
            }
            TempData["Success"] = "Progress updated successfully.";
            return RedirectToAction("TutorDashboard", "Dashboard");
        }
    }
}