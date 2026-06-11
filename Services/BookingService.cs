using Microsoft.EntityFrameworkCore;
using OnlineMovieBooking.Data;
using OnlineMovieBooking.Models;
using OnlineMovieBooking.Models.ViewModels;

namespace OnlineMovieBooking.Services;

public interface IBookingService
{
    Task<(bool Success, int BookingId, string Message)> CreateBookingAsync(
        int userId, int showtimeId, List<int> seatIds,
        string name, string phone, string email, string paymentMethod, string? note);
    Task<List<Booking>> GetUserBookingsAsync(int userId);
    Task<Booking?> GetBookingByIdAsync(int id);
    Task<bool> CancelBookingAsync(int bookingId, int userId);
    Task<List<SelectedSeatInfo>> GetPaymentSummaryAsync(List<int> seatIds);
    Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId);
    Task<bool> CancelPaymentAsync(int bookingId);
    Task<bool> SaveBillImageAsync(int bookingId, string imageUrl);
}

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public BookingService(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<(bool Success, int BookingId, string Message)> CreateBookingAsync(
        int userId, int showtimeId, List<int> seatIds,
        string name, string phone, string email, string paymentMethod, string? note)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var showtime = await _db.Showtimes.Include(s => s.Room).FirstOrDefaultAsync(s => s.Id == showtimeId);
            if (showtime == null) return (false, 0, "Suất chiếu không tồn tại");

            // Verify seats belong to this showtime's room and are available
            var bookedSeatIds = await _db.BookingDetails
                .Include(bd => bd.Booking)
                .Where(bd => bd.Booking.ShowtimeId == showtimeId && bd.Booking.Status != "cancelled")
                .Select(bd => bd.SeatId)
                .ToListAsync();

            var seats = await _db.Seats
                .Where(s => seatIds.Contains(s.Id) && s.RoomId == showtime.RoomId)
                .ToListAsync();

            if (seats.Count != seatIds.Count)
                return (false, 0, "Một số ghế không hợp lệ");

            if (seats.Any(s => bookedSeatIds.Contains(s.Id)))
                return (false, 0, "Một số ghế đã được đặt");

            decimal totalPrice = seats.Sum(s => s.Type == "vip" ? 120000 : 90000);

            var booking = new Booking
            {
                UserId = userId,
                ShowtimeId = showtimeId,
                Name = name,
                Phone = phone,
                Email = email,
                PaymentMethod = paymentMethod,
                TotalPrice = totalPrice,
                Status = "pending",
                Note = note,
                CreatedAt = DateTime.UtcNow
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            foreach (var seat in seats)
            {
                _db.BookingDetails.Add(new BookingDetail
                {
                    BookingId = booking.Id,
                    SeatId = seat.Id,
                    Price = seat.Type == "vip" ? 120000 : 90000
                });
            }
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notifications.CreateBookingNotificationAsync(userId, booking.Id,
                "Đặt vé thành công", $"Bạn đã đặt {seats.Count} ghế thành công. Mã đặt vé: #{booking.Id}");

            return (true, booking.Id, "Đặt vé thành công");
        }
        catch
        {
            await transaction.RollbackAsync();
            return (false, 0, "Đã xảy ra lỗi khi đặt vé");
        }
    }

    public async Task<List<Booking>> GetUserBookingsAsync(int userId) =>
        await _db.Bookings
                 .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                 .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                 .Include(b => b.BookingDetails).ThenInclude(bd => bd.Seat)
                 .Where(b => b.UserId == userId)
                 .OrderByDescending(b => b.CreatedAt)
                 .ToListAsync();

    public async Task<Booking?> GetBookingByIdAsync(int id) =>
        await _db.Bookings
                 .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                 .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                 .Include(b => b.BookingDetails).ThenInclude(bd => bd.Seat)
                 .Include(b => b.User)
                 .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<bool> CancelBookingAsync(int bookingId, int userId)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
        if (booking == null || booking.Status == "cancelled") return false;
        booking.Status = "cancelled";
        await _db.SaveChangesAsync();
        await _notifications.CreateBookingNotificationAsync(userId, bookingId,
            "Hủy vé thành công", $"Đơn đặt vé #{bookingId} đã được hủy.");
        return true;
    }

    public async Task<List<SelectedSeatInfo>> GetPaymentSummaryAsync(List<int> seatIds)
    {
        var seats = await _db.Seats.Where(s => seatIds.Contains(s.Id)).ToListAsync();
        return seats.Select(s => new SelectedSeatInfo
        {
            SeatId = s.Id,
            Label = $"{s.RowLabel}{s.ColNumber}",
            Type = s.Type,
            Price = s.Type == "vip" ? 120000 : 90000
        }).ToList();
    }

    public async Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null || booking.Status == "cancelled") return false;
        booking.Status = "confirmed";
        booking.Note = string.IsNullOrEmpty(booking.Note)
            ? $"TxnID:{transactionId}"
            : $"{booking.Note} | TxnID:{transactionId}";
        await _db.SaveChangesAsync();
        await _notifications.CreateBookingNotificationAsync(booking.UserId, bookingId,
            "Thanh toán thành công", $"Đơn đặt vé #{bookingId} đã được thanh toán thành công.");
        return true;
    }

    public async Task<bool> CancelPaymentAsync(int bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;
        booking.Status = "cancelled";
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveBillImageAsync(int bookingId, string imageUrl)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;
        booking.BillImageUrl = imageUrl;
        await _db.SaveChangesAsync();
        return true;
    }
}
