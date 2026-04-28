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

            var userIds = await _context.ChatMessages
                .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
                .Select(m => m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.AvailableUsers = await _userManager.Users
                .Where(u => u.Id != currentUser.Id && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

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
    }
}