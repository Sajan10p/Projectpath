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

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
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
        public async Task<IActionResult> Create(CreateProjectViewModel model, IFormFile? projectFile)
        {
            if (!FileUploadValidator.IsValid(projectFile, out var fileError))
            {
                ModelState.AddModelError("projectFile", fileError);
            }

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            string? projectFileName = null;
            string? projectFilePath = null;

            if (projectFile != null && projectFile.Length > 0)
            {
                var saved = await FileUploadValidator.SaveAsync(projectFile, _environment, "projects");
                projectFileName = saved.FileName;
                projectFilePath = saved.RelativePath;
            }

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
                ProjectFileName = projectFileName,
                ProjectFilePath = projectFilePath,
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
            ViewBag.Submissions = assignment == null ? new List<Submission>() : await _context.Submissions.Where(s => s.AssignmentId == assignment.Id && s.StudentId == user!.Id).OrderByDescending(s => s.SubmittedAt).ToListAsync();
            return View(project);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> UploadSubmission(int assignmentId, IFormFile? submissionFile)
        {
            var user = await _userManager.GetUserAsync(User);
            var assignment = await _context.Assignments.Include(a => a.StudentGroup)!.ThenInclude(g => g.Members).FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null || !assignment.StudentGroup!.Members.Any(m => m.StudentId == user!.Id)) return Forbid();

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

            _context.Submissions.Add(new Submission
            {
                AssignmentId = assignmentId,
                StudentId = user!.Id,
                FileName = saved.FileName,
                FilePath = saved.RelativePath,
                Status = "Submitted",
                SubmittedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Submission uploaded successfully.";
            return RedirectToAction("MyProject");
        }
    }
}