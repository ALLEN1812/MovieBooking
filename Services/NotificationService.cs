using Microsoft.EntityFrameworkCore;
using OnlineMovieBooking.Data;
using OnlineMovieBooking.Models;

namespace OnlineMovieBooking.Services;

public interface INotificationService
{
    Task<List<Notification>> GetUserNotificationsAsync(int userId);
    Task<Notification?> GetByIdAsync(int id, int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task CreateBookingNotificationAsync(int userId, int bookingId, string title, string message);
    Task CreateNotificationAsync(int userId, string title, string message);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId) =>
        await _db.Notifications
                 .Where(n => n.UserId == userId)
                 .OrderByDescending(n => n.CreatedAt)
                 .Take(50)
                 .ToListAsync();

    public async Task<Notification?> GetByIdAsync(int id, int userId) =>
        await _db.Notifications
                 .Include(n => n.Booking).ThenInclude(b => b!.Showtime).ThenInclude(s => s.Movie)
                 .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

    public async Task<int> GetUnreadCountAsync(int userId) =>
        await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task CreateBookingNotificationAsync(int userId, int bookingId, string title, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            BookingId = bookingId,
            Title = title,
            Message = message,
            Type = "booking",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(int userId, string title, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = "booking",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (n == null) return false;
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in notifications) n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}
