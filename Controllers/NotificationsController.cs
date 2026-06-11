using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;
    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        await _notifications.MarkAllAsReadAsync(UserId);
        var list = await _notifications.GetUserNotificationsAsync(UserId);
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _notifications.GetUnreadCountAsync(UserId);
        return Json(new { count });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var n = await _notifications.GetByIdAsync(id, UserId);
        if (n == null) return NotFound();
        if (!n.IsRead) await _notifications.MarkAsReadAsync(id, UserId);
        return View(n);
    }
}
