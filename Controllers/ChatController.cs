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
            if (currentUser == null) return Unauthorized();

            var allowedUsers = await GetAllowedChatUsersAsync(currentUser);
            ViewBag.AvailableUsers = allowedUsers;

            var userIds = await _context.ChatMessages
                .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
                .Select(m => m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var conversations = allowedUsers
                .Where(u => userIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .ToList();

            return View(conversations);
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (!await CanChatWithAsync(currentUser, userId)) return Forbid();

            var receiver = await _userManager.FindByIdAsync(userId);
            if (receiver == null) return NotFound();

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
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
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverId, string messageText)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (!await CanChatWithAsync(currentUser, receiverId)) return Forbid();

            if (!string.IsNullOrWhiteSpace(messageText))
            {
                _context.ChatMessages.Add(new ChatMessage
                {
                    SenderId = currentUser.Id,
                    ReceiverId = receiverId,
                    MessageText = messageText.Trim(),
                    SentAt = DateTime.Now
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

            if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                users = await _userManager.Users
                    .Where(u => u.Id != currentUser.Id && u.IsActive && u.IsRegistrationApproved)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "Company"))
            {
                users.AddRange(await _userManager.GetUsersInRoleAsync("Admin"));
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "Tutor"))
            {
                var assignments = await _context.Assignments
                    .Include(a => a.StudentGroup)!
                        .ThenInclude(g => g.Members)
                        .ThenInclude(m => m.Student)
                    .Where(a => a.TutorId == currentUser.Id)
                    .ToListAsync();

                foreach (var assignment in assignments)
                {
                    users.AddRange(assignment.StudentGroup!.Members.Select(m => m.Student));
                }

                users.AddRange(await _userManager.GetUsersInRoleAsync("Admin"));
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "Student"))
            {
                var membership = await _context.GroupMembers
                    .Include(m => m.StudentGroup)!
                        .ThenInclude(g => g.Members)
                        .ThenInclude(m => m.Student)
                    .Include(m => m.StudentGroup)!
                        .ThenInclude(g => g.Project)!
                        .ThenInclude(p => p.Assignments)
                        .ThenInclude(a => a.Tutor)
                    .FirstOrDefaultAsync(m => m.StudentId == currentUser.Id);

                if (membership?.StudentGroup != null)
                {
                    users.AddRange(membership.StudentGroup.Members
                        .Where(m => m.StudentId != currentUser.Id)
                        .Select(m => m.Student));

                    var tutor = membership.StudentGroup.Project?.Assignments.FirstOrDefault()?.Tutor;
                    if (tutor != null) users.Add(tutor);
                }

                users.AddRange(await _userManager.GetUsersInRoleAsync("Admin"));
            }

            return users
                .Where(u => u != null && u.Id != currentUser.Id && u.IsActive && u.IsRegistrationApproved)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .OrderBy(u => u.FullName)
                .ToList();
        }
    }
}