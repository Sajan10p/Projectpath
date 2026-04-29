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

        public TutorController(
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

        // ============================================================
        // TUTOR: ASSIGNED STUDENTS / GROUPS
        // ============================================================

        public async Task<IActionResult> AssignedStudents()
        {
            var tutor = await _userManager.GetUserAsync(User);

            if (tutor == null)
                return Unauthorized();

            var assignments = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)!
                    .ThenInclude(g => g.Leader)
                .Include(a => a.StudentGroup)!
                    .ThenInclude(g => g.Members)
                        .ThenInclude(m => m.Student)
                .Where(a => a.TutorId == tutor.Id)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            return View(assignments);
        }

        // ============================================================
        // TUTOR: VIEW SUBMISSIONS FROM ASSIGNED STUDENTS ONLY
        // ============================================================

        public async Task<IActionResult> Submissions()
        {
            var tutor = await _userManager.GetUserAsync(User);

            if (tutor == null)
                return Unauthorized();

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Project)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.StudentGroup)
                .Where(s => s.Assignment != null && s.Assignment.TutorId == tutor.Id)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            return View(submissions);
        }

        // ============================================================
        // TUTOR: DOWNLOAD SUBMISSION FILE
        // Only works if submission belongs to tutor's assigned group
        // ============================================================

        public async Task<IActionResult> DownloadSubmission(int id)
        {
            var tutor = await _userManager.GetUserAsync(User);

            if (tutor == null)
                return Unauthorized();

            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Assignment != null &&
                    s.Assignment.TutorId == tutor.Id);

            if (submission == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(submission.FilePath))
                return NotFound();

            var relativePath = submission.FilePath
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            submission.Status = "Viewed";
            submission.ViewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return PhysicalFile(
                fullPath,
                "application/octet-stream",
                submission.FileName
            );
        }

        // ============================================================
        // TUTOR: ADD PROJECT PROGRESS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AddProgress(int assignmentId)
        {
            var tutor = await _userManager.GetUserAsync(User);

            if (tutor == null)
                return Unauthorized();

            var assignment = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)!
                    .ThenInclude(g => g.Members)
                        .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(a =>
                    a.Id == assignmentId &&
                    a.TutorId == tutor.Id);

            if (assignment == null)
                return NotFound();

            ViewBag.Assignment = assignment;

            var model = new TutorProgressViewModel
            {
                AssignmentId = assignmentId,
                ProgressPercent = assignment.CurrentProgressPercent
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProgress(TutorProgressViewModel model)
        {
            var tutor = await _userManager.GetUserAsync(User);

            if (tutor == null)
                return Unauthorized();

            var assignment = await _context.Assignments
                .Include(a => a.Project)
                .Include(a => a.StudentGroup)!
                    .ThenInclude(g => g.Members)
                        .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(a =>
                    a.Id == model.AssignmentId &&
                    a.TutorId == tutor.Id);

            if (assignment == null)
                return NotFound();

            if (model.IsIndividualFeedback)
            {
                if (string.IsNullOrWhiteSpace(model.StudentId) ||
                    assignment.StudentGroup == null ||
                    !assignment.StudentGroup.Members.Any(m => m.StudentId == model.StudentId))
                {
                    ModelState.AddModelError("StudentId", "Select a valid student.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Assignment = assignment;
                return View(model);
            }

            assignment.CurrentProgressPercent = model.ProgressPercent;

            var progress = new ProgressUpdate
            {
                AssignmentId = model.AssignmentId,
                TutorId = tutor.Id,
                Note = model.Note,
                IsIndividualFeedback = model.IsIndividualFeedback,
                StudentId = model.IsIndividualFeedback ? model.StudentId : null,
                CreatedAt = DateTime.Now
            };

            _context.ProgressUpdates.Add(progress);

            await _context.SaveChangesAsync();

            if (assignment.StudentGroup != null)
            {
                foreach (var member in assignment.StudentGroup.Members)
                {
                    await _notificationService.CreateAsync(
                        member.StudentId,
                        "Project Progress Updated",
                        $"Project progress was updated to {assignment.CurrentProgressPercent}%.",
                        "/Projects/MyProject"
                    );

                    if (!string.IsNullOrWhiteSpace(member.Student?.Email))
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(
                                member.Student.Email,
                                "Project Progress Updated - ProjectPath",
                                $@"
Hello {member.Student.FullName},

Your tutor has updated the project progress.

Project: {assignment.Project?.Title}
Progress: {assignment.CurrentProgressPercent}%

Regards,
ProjectPath Team
"
                            );
                        }
                        catch
                        {
                            // Email failure should not stop progress update.
                        }
                    }
                }
            }

            TempData["Success"] = "Progress updated successfully.";
            return RedirectToAction("AssignedStudents");
        }
    }
}