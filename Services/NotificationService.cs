using Projectpath.Data;
using Projectpath.Models;

namespace Projectpath.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(string userId, string title, string message, string? link = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false,
                LinkUrl = link
            });

            await _context.SaveChangesAsync();
        }
    }
}