using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Models.ViewModels;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookings;
    private readonly IMovieService _movies;

    public BookingController(IBookingService bookings, IMovieService movies)
    {
        _bookings = bookings;
        _movies = movies;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Seats(int showtimeId)
    {
        var showtime = await _movies.GetShowtimeByIdAsync(showtimeId);
        if (showtime == null) return NotFound();

        var seats = await _movies.GetSeatsByShowtimeAsync(showtimeId);

        var vm = new SeatMapViewModel
        {
            Showtime = showtime,
            Seats = seats,
            RoomName = showtime.Room.Name,
            TotalRows = showtime.Room.TotalRows,
            TotalCols = showtime.Room.TotalCols
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int showtimeId, string seatIds)
    {
        var ids = seatIds.Split(',').Select(int.Parse).ToList();
        if (!ids.Any()) return RedirectToAction("Seats", new { showtimeId });

        var showtime = await _movies.GetShowtimeByIdAsync(showtimeId);
        if (showtime == null) return NotFound();

        var seatInfos = await _bookings.GetPaymentSummaryAsync(ids);
        var user = HttpContext.User;

        var vm = new BookingConfirmViewModel
        {
            ShowtimeId = showtimeId,
            SeatIds = ids,
            SelectedSeats = seatInfos,
            Showtime = showtime,
            TotalPrice = seatInfos.Sum(s => s.Price),
            Name = user.FindFirstValue(ClaimTypes.Name) ?? "",
            Email = user.FindFirstValue(ClaimTypes.Email) ?? ""
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(BookingConfirmViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var showtime = await _movies.GetShowtimeByIdAsync(model.ShowtimeId);
            model.Showtime = showtime;
            model.SelectedSeats = await _bookings.GetPaymentSummaryAsync(model.SeatIds);
            model.TotalPrice = model.SelectedSeats.Sum(s => s.Price);
            return View(model);
        }

        var (success, bookingId, message) = await _bookings.CreateBookingAsync(
            UserId, model.ShowtimeId, model.SeatIds,
            model.Name, model.Phone, model.Email, model.PaymentMethod, model.Note);

        if (!success)
        {
            TempData["Error"] = message;
            return RedirectToAction("Seats", new { showtimeId = model.ShowtimeId });
        }

        // MoMo: hiện QR chuyển khoản
        if (string.Equals(model.PaymentMethod, "momo", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("QRCode", "Payment", new { bookingId });

        // COD: confirm immediately
        await _bookings.ConfirmPaymentAsync(bookingId, "COD");
        return RedirectToAction("Success", new { id = bookingId });
    }

    public async Task<IActionResult> Invoice(int id)
    {
        var booking = await _bookings.GetBookingByIdAsync(id);
        if (booking == null || booking.UserId != UserId) return NotFound();
        return View(booking);
    }

    public async Task<IActionResult> Success(int id)
    {
        var booking = await _bookings.GetBookingByIdAsync(id);
        if (booking == null || booking.UserId != UserId) return NotFound();

        var vm = new BookingSuccessViewModel
        {
            BookingId = booking.Id,
            MovieTitle = booking.Showtime.Movie.Title,
            ShowtimeStart = booking.Showtime.StartTime,
            RoomName = booking.Showtime.Room.Name,
            CinemaName = booking.Showtime.Room.Cinema.Name,
            SeatLabels = booking.BookingDetails.Select(bd => $"{bd.Seat.RowLabel}{bd.Seat.ColNumber}").ToList(),
            TotalPrice = booking.TotalPrice,
            PaymentMethod = booking.PaymentMethod,
            Status = booking.Status
        };
        return View(vm);
    }
}
