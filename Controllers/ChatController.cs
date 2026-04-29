using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projectpath.Data;
using Projectpath.Models;

namespace Projectpath.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var currentUserRole = await GetPrimaryRoleAsync(currentUser);
            var availableUsers = await GetAllowedChatUsersAsync(currentUser);

            var conversationUserIds = await _context.ChatMessages
                .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
                .Select(m => m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var conversations = availableUsers
                .Where(u => conversationUserIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.AvailableUsers = availableUsers;
            ViewBag.CurrentUserRole = currentUserRole;

            if (currentUserRole == "Student")
            {
                ViewBag.AdminUsers = availableUsers
                    .Where(u => u.UserRole == "Admin")
                    .ToList();

                ViewBag.GroupMembers = availableUsers
                    .Where(u => u.UserRole == "Student")
                    .ToList();

                ViewBag.TutorUsers = availableUsers
                    .Where(u => u.UserRole == "Tutor")
                    .ToList();

                ViewBag.CompanyUsers = availableUsers
                    .Where(u => u.UserRole == "Company")
                    .ToList();
            }

            return View(conversations);
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            if (!await CanChatWithAsync(currentUser, userId))
                return Forbid();

            var receiver = await _userManager.FindByIdAsync(userId);

            if (receiver == null)
                return NotFound();

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            foreach (var message in messages.Where(m => m.ReceiverId == currentUser.Id && !m.IsRead))
            {
                message.IsRead = true;
                message.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            ViewBag.Receiver = receiver;
            ViewBag.CurrentUserId = currentUser.Id;

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string receiverId, string messageText)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            if (!await CanChatWithAsync(currentUser, receiverId))
                return Forbid();

            if (!string.IsNullOrWhiteSpace(messageText))
            {
                _context.ChatMessages.Add(new ChatMessage
                {
                    SenderId = currentUser.Id,
                    ReceiverId = receiverId,
                    MessageText = messageText.Trim(),
                    SentAt = DateTime.Now,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Conversation", new { userId = receiverId });
        }

        private async Task<bool> CanChatWithAsync(ApplicationUser currentUser, string receiverId)
        {
            var allowedUsers = await GetAllowedChatUsersAsync(currentUser);
            return allowedUsers.Any(u => u.Id == receiverId);
        }

        private async Task<List<ApplicationUser>> GetAllowedChatUsersAsync(ApplicationUser currentUser)
        {
            var users = new List<ApplicationUser>();
            var currentUserRole = await GetPrimaryRoleAsync(currentUser);

            // ================= ADMIN =================
            // Admin can chat with all approved and active users.
            if (currentUserRole == "Admin")
            {
                users = await _userManager.Users
                    .Where(u =>
                        u.Id != currentUser.Id &&
                        u.IsActive &&
                        u.IsRegistrationApproved)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }

            // ================= COMPANY =================
            // Company can chat with Admin and group leaders of this company's projects.
            else if (currentUserRole == "Company")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                users.AddRange(admins);

                var groupLeaders = await _context.StudentGroups
                    .Include(g => g.Project)
                    .Include(g => g.Leader)
                    .Where(g =>
                        g.Project != null &&
                        g.Project.CompanyId == currentUser.Id &&
                        g.Leader != null)
                    .Select(g => g.Leader!)
                    .ToListAsync();

                users.AddRange(groupLeaders);
            }

            // ================= TUTOR =================
            // Tutor can chat with Admin and assigned students only.
            else if (currentUserRole == "Tutor")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                users.AddRange(admins);

                var assignedStudents = await _context.Assignments
                    .Include(a => a.StudentGroup)!
                        .ThenInclude(g => g.Members)
                            .ThenInclude(m => m.Student)
                    .Where(a => a.TutorId == currentUser.Id)
                    .SelectMany(a => a.StudentGroup!.Members)
                    .Where(m => m.Student != null)
                    .Select(m => m.Student!)
                    .ToListAsync();

                users.AddRange(assignedStudents);
            }

            // ================= STUDENT =================
            // Student can chat with:
            // Admin, group members, assigned tutor, project company.
            else if (currentUserRole == "Student")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                users.AddRange(admins);

                var membership = await _context.GroupMembers
                    .Include(m => m.StudentGroup)!
                        .ThenInclude(g => g.Members)
                            .ThenInclude(m => m.Student)
                    .Include(m => m.StudentGroup)!
                        .ThenInclude(g => g.Project)!
                            .ThenInclude(p => p.Company)
                    .Include(m => m.StudentGroup)!
                        .ThenInclude(g => g.Project)!
                            .ThenInclude(p => p.Assignments)
                                .ThenInclude(a => a.Tutor)
                    .FirstOrDefaultAsync(m => m.StudentId == currentUser.Id);

                if (membership?.StudentGroup != null)
                {
                    var groupMembers = membership.StudentGroup.Members
                        .Where(m => m.StudentId != currentUser.Id && m.Student != null)
                        .Select(m => m.Student!)
                        .ToList();

                    users.AddRange(groupMembers);

                    var assignedTutor = membership.StudentGroup.Project?
                        .Assignments
                        .FirstOrDefault(a => a.StudentGroupId == membership.StudentGroupId)?
                        .Tutor;

                    if (assignedTutor != null)
                    {
                        users.Add(assignedTutor);
                    }

                    var projectCompany = membership.StudentGroup.Project?.Company;

                    if (projectCompany != null)
                    {
                        users.Add(projectCompany);
                    }
                }
            }

            return users
                .Where(u =>
                    u != null &&
                    u.Id != currentUser.Id &&
                    u.IsActive &&
                    u.IsRegistrationApproved)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .OrderBy(u => u.FullName)
                .ToList();
        }

        private async Task<string> GetPrimaryRoleAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? user.UserRole ?? "";
        }
    }
}