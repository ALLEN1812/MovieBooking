using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Models.ViewModels;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IAuthService _auth;
    private readonly IBookingService _bookings;

    public ProfileController(IAuthService auth, IBookingService bookings)
    {
        _auth = auth;
        _bookings = bookings;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Account()
    {
        var user = await _auth.GetUserByIdAsync(UserId);
        if (user == null) return NotFound();
        var vm = new UpdateProfileViewModel { Name = user.Name, Phone = user.Phone, Address = user.Address };
        ViewBag.Email = user.Email;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Account(UpdateProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        await _auth.UpdateProfileAsync(UserId, model.Name, model.Phone, model.Address);
        TempData["Success"] = "Cập nhật thông tin thành công!";
        return RedirectToAction("Account");
    }

    public async Task<IActionResult> Bookings()
    {
        var bookings = await _bookings.GetUserBookingsAsync(UserId);
        return View(bookings);
    }

    [HttpPost]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var success = await _bookings.CancelBookingAsync(id, UserId);
        TempData[success ? "Success" : "Error"] = success ? "Hủy vé thành công!" : "Không thể hủy vé này.";
        return RedirectToAction("Bookings");
    }

    public IActionResult ChangePassword() => View();

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var success = await _auth.ChangePasswordAsync(UserId, model.CurrentPassword, model.NewPassword);
        if (!success)
        {
            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác");
            return View(model);
        }
        TempData["Success"] = "Đổi mật khẩu thành công!";
        return RedirectToAction("ChangePassword");
    }
}
