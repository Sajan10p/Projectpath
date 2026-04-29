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
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ProjectsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ============================================================
        // COMPANY: MY PROJECTS
        // ============================================================

        [Authorize(Roles = "Company")]
        public async Task<IActionResult> MyProjects()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var projects = await _context.Projects
                .Where(p => p.CompanyId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // ============================================================
        // COMPANY: CREATE PROJECT
        // File upload is OPTIONAL
        // ============================================================

        [Authorize(Roles = "Company")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Company")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model, IFormFile? projectFile)
        {
            if (!FileUploadValidator.IsValid(projectFile, out var fileError))
            {
                ModelState.AddModelError("projectFile", fileError);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            string? projectFileName = null;
            string? projectFilePath = null;

            if (projectFile != null && projectFile.Length > 0)
            {
                var saved = await FileUploadValidator.SaveAsync(projectFile, _environment, "projects");
                projectFileName = saved.FileName;
                projectFilePath = saved.RelativePath;
            }

            var project = new Project
            {
                Title = model.Title,
                Description = model.Description,
                Requirements = model.Requirements,
                Program = model.Program,
                ProjectType = model.ProjectType,

                CompanyId = user.Id,
                IsApproved = false,
                Status = "Pending",

                ContactPersonName = model.ContactPersonName,
                ContactPhone = model.ContactPhone,
                ContactPersonEmail = model.ContactPersonEmail,
                ExpectedDurationWeeks = model.ExpectedDurationWeeks,

                ProjectFileName = projectFileName,
                ProjectFilePath = projectFilePath,

                CreatedAt = DateTime.Now
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project submitted successfully. It is now waiting for admin approval.";
            return RedirectToAction("MyProjects");
        }

        // ============================================================
        // COMPANY: EDIT PROJECT
        // Company can upload/replace file later
        // ============================================================

        [Authorize(Roles = "Company")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == user.Id);

            if (project == null)
                return NotFound();

            var model = new CreateProjectViewModel
            {
                Title = project.Title,
                Description = project.Description,
                Requirements = project.Requirements,
                Program = project.Program,
                ProjectType = project.ProjectType,
                ContactPersonName = project.ContactPersonName,
                ContactPhone = project.ContactPhone,
                ContactPersonEmail = project.ContactPersonEmail,
                ExpectedDurationWeeks = project.ExpectedDurationWeeks
            };

            ViewBag.ProjectId = project.Id;
            ViewBag.CurrentFileName = project.ProjectFileName;
            ViewBag.CurrentFilePath = project.ProjectFilePath;
            ViewBag.Status = project.Status;

            return View(model);
        }

        [Authorize(Roles = "Company")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProjectViewModel model, IFormFile? projectFile)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == user.Id);

            if (project == null)
                return NotFound();

            if (!FileUploadValidator.IsValid(projectFile, out var fileError))
            {
                ModelState.AddModelError("projectFile", fileError);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = project.Id;
                ViewBag.CurrentFileName = project.ProjectFileName;
                ViewBag.CurrentFilePath = project.ProjectFilePath;
                ViewBag.Status = project.Status;
                return View(model);
            }

            project.Title = model.Title;
            project.Description = model.Description;
            project.Requirements = model.Requirements;
            project.Program = model.Program;
            project.ProjectType = model.ProjectType;
            project.ContactPersonName = model.ContactPersonName;
            project.ContactPhone = model.ContactPhone;
            project.ContactPersonEmail = model.ContactPersonEmail;
            project.ExpectedDurationWeeks = model.ExpectedDurationWeeks;

            if (projectFile != null && projectFile.Length > 0)
            {
                var saved = await FileUploadValidator.SaveAsync(projectFile, _environment, "projects");
                project.ProjectFileName = saved.FileName;
                project.ProjectFilePath = saved.RelativePath;
            }

            project.IsApproved = false;
            project.Status = "Pending";
            project.AdminDecisionMessage = null;
            project.AdminInternalNotes = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project updated successfully and sent back for admin approval.";
            return RedirectToAction("MyProjects");
        }

        // ============================================================
        // STUDENT: BROWSE APPROVED PROJECTS
        // ============================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Approved()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            ViewBag.AlreadyInGroup = await _context.GroupMembers
                .AnyAsync(m => m.StudentId == user.Id);

            var projects = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.StudentGroups)
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // ============================================================
        // STUDENT: PROJECT DETAILS BEFORE CREATING GROUP
        // This fixes the current issue where View Details goes straight
        // to Create Group.
        // ============================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var project = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.StudentGroups)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsApproved);

            if (project == null)
                return NotFound();

            ViewBag.AlreadyInGroup = await _context.GroupMembers
                .AnyAsync(m => m.StudentId == user.Id);

            return View(project);
        }

        // ============================================================
        // STUDENT: MY PROJECT
        // Student can view company project file and upload submissions
        // ============================================================

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyProject()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var membership = await _context.GroupMembers
                .Include(m => m.StudentGroup)!
                    .ThenInclude(g => g.Project)!
                        .ThenInclude(p => p.Company)
                .Include(m => m.StudentGroup)!
                    .ThenInclude(g => g.Project)!
                        .ThenInclude(p => p.Assignments)
                            .ThenInclude(a => a.ProgressUpdates)
                .Include(m => m.StudentGroup)!
                    .ThenInclude(g => g.Project)!
                        .ThenInclude(p => p.Assignments)
                            .ThenInclude(a => a.Tutor)
                .FirstOrDefaultAsync(m => m.StudentId == user.Id);

            if (membership == null ||
                membership.StudentGroup == null ||
                membership.StudentGroup.Project == null)
            {
                TempData["Error"] = "You are not assigned to any project yet.";
                return RedirectToAction("StudentDashboard", "Dashboard");
            }

            var project = membership.StudentGroup.Project;
            var assignment = project.Assignments
                .FirstOrDefault(a => a.StudentGroupId == membership.StudentGroupId);

            ViewBag.ProgressPercent = assignment?.CurrentProgressPercent ?? 0;
            ViewBag.Assignment = assignment;

            ViewBag.Submissions = assignment == null
                ? new List<Submission>()
                : await _context.Submissions
                    .Where(s => s.AssignmentId == assignment.Id && s.StudentId == user.Id)
                    .OrderByDescending(s => s.SubmittedAt)
                    .ToListAsync();

            return View(project);
        }

        // ============================================================
        // STUDENT: UPLOAD SUBMISSION
        // ============================================================

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSubmission(int assignmentId, IFormFile? submissionFile)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var assignment = await _context.Assignments
                .Include(a => a.StudentGroup)!
                    .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null ||
                assignment.StudentGroup == null ||
                !assignment.StudentGroup.Members.Any(m => m.StudentId == user.Id))
            {
                return Forbid();
            }

            if (submissionFile == null || submissionFile.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                return RedirectToAction("MyProject");
            }

            if (!FileUploadValidator.IsValid(submissionFile, out var fileError))
            {
                TempData["Error"] = fileError;
                return RedirectToAction("MyProject");
            }

            var saved = await FileUploadValidator.SaveAsync(submissionFile, _environment, "submissions");

            var submission = new Submission
            {
                AssignmentId = assignmentId,
                StudentId = user.Id,
                FileName = saved.FileName,
                FilePath = saved.RelativePath,
                Status = "Submitted",
                SubmittedAt = DateTime.Now
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Submission uploaded successfully.";
            return RedirectToAction("MyProject");
        }
    }
}