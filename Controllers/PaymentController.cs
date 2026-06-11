using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly IBookingService _bookings;
    private readonly IConfiguration _config;
    private readonly ICloudinaryService _cloudinary;

    public PaymentController(IBookingService bookings, IConfiguration config, ICloudinaryService cloudinary)
    {
        _bookings = bookings;
        _config = config;
        _cloudinary = cloudinary;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> QRCode(int bookingId)
    {
        var booking = await _bookings.GetBookingByIdAsync(bookingId);
        if (booking == null || booking.UserId != UserId) return NotFound();

        if (booking.Status == "confirmed")
            return RedirectToAction("Success", "Booking", new { id = bookingId });

        var bankId      = _config["BankTransfer:BankId"]!;
        var accountNo   = _config["BankTransfer:AccountNo"]!;
        var accountName = _config["BankTransfer:AccountName"]!;
        var amount      = (long)booking.TotalPrice;
        var addInfo     = $"DAT VE {bookingId}";

        ViewBag.QRUrl       = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png" +
                              $"?amount={amount}&addInfo={Uri.EscapeDataString(addInfo)}" +
                              $"&accountName={Uri.EscapeDataString(accountName)}";
        ViewBag.Amount      = amount;
        ViewBag.AddInfo     = addInfo;
        ViewBag.AccountNo   = accountNo;
        ViewBag.AccountName = accountName;
        ViewBag.BankId      = bankId;

        return View(booking);
    }

    [HttpGet]
    public async Task<IActionResult> QRStatus(int bookingId)
    {
        var booking = await _bookings.GetBookingByIdAsync(bookingId);
        if (booking == null || booking.UserId != UserId)
            return Json(new { status = "error" });
        return Json(new { status = booking.Status, hasBill = !string.IsNullOrEmpty(booking.BillImageUrl) });
    }

    [HttpPost]
    public async Task<IActionResult> UploadBill(int bookingId, IFormFile bill)
    {
        var booking = await _bookings.GetBookingByIdAsync(bookingId);
        if (booking == null || booking.UserId != UserId)
            return Json(new { success = false, message = "Không tìm thấy đơn đặt vé." });

        if (bill == null || bill.Length == 0)
            return Json(new { success = false, message = "Vui lòng chọn ảnh bill." });

        if (bill.Length > 5 * 1024 * 1024)
            return Json(new { success = false, message = "Ảnh không được vượt quá 5MB." });

        var allowed = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };
        if (!allowed.Contains(bill.ContentType.ToLower()))
            return Json(new { success = false, message = "Chỉ chấp nhận ảnh JPG, PNG, WEBP." });

        var url = await _cloudinary.UploadImageAsync(bill, "bills");
        if (string.IsNullOrEmpty(url))
            return Json(new { success = false, message = "Tải ảnh thất bại, vui lòng thử lại." });

        await _bookings.SaveBillImageAsync(bookingId, url);
        return Json(new { success = true, url });
    }
}
