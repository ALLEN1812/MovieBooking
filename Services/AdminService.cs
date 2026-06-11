using Microsoft.EntityFrameworkCore;
using OnlineMovieBooking.Data;
using OnlineMovieBooking.Models;
using OnlineMovieBooking.Models.ViewModels;

namespace OnlineMovieBooking.Services;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync();
    Task<List<User>> GetAllUsersAsync(string? search = null);
    Task<User?> GetUserByIdAsync(int id);
    Task<bool> CreateUserAsync(User user, string password);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> UpdateUserRoleAsync(int id, string role);
    Task<List<Room>> GetAllRoomsAsync();
    Task<Room?> GetRoomByIdAsync(int id);
    Task<bool> CreateRoomAsync(Room room);
    Task<bool> UpdateRoomAsync(Room room);
    Task<bool> DeleteRoomAsync(int id);
    Task<List<Room>> GetRoomsByCinemaAsync(int? cinemaId = null);
    Task<List<Seat>> GetSeatsByRoomAsync(int roomId);
    Task<bool> CreateSeatAsync(Seat seat);
    Task<bool> UpdateSeatAsync(Seat seat);
    Task<bool> DeleteSeatAsync(int id);
    Task<List<Booking>> GetAllBookingsAsync(string? status = null, int? cinemaId = null, DateTime? from = null, DateTime? to = null);
    Task<bool> UpdateBookingStatusAsync(int bookingId, string status);
    Task<List<Cinema>> GetCinemasForFilterAsync();
    Task<List<Cinema>> GetAllCinemasAsync();
    Task<Cinema?> GetCinemaByIdAsync(int id);
    Task<bool> CreateCinemaAsync(Cinema cinema);
    Task<bool> UpdateCinemaAsync(Cinema cinema);
    Task<bool> DeleteCinemaAsync(int id);
}

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    private readonly IConfiguration _config;

    public AdminService(ApplicationDbContext db, INotificationService notifications, IConfiguration config)
    {
        _db = db;
        _notifications = notifications;
        _config = config;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync()
    {
        var bookings = await _db.Bookings
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
            .OrderByDescending(b => b.CreatedAt).Take(10).ToListAsync();

        return new AdminDashboardViewModel
        {
            TotalUsers = await _db.Users.CountAsync(u => u.Role == "user"),
            TotalMovies = await _db.Movies.CountAsync(),
            TotalBookings = await _db.Bookings.CountAsync(),
            TotalShowtimes = await _db.Showtimes.CountAsync(),
            PendingBookings = await _db.Bookings.CountAsync(b => b.Status == "pending"),
            ConfirmedBookings = await _db.Bookings.CountAsync(b => b.Status == "confirmed"),
            CancelledBookings = await _db.Bookings.CountAsync(b => b.Status == "cancelled"),
            TotalRevenue = await _db.Bookings.Where(b => b.Status == "confirmed").SumAsync(b => b.TotalPrice),
            RecentBookings = bookings
        };
    }

    public async Task<List<User>> GetAllUsersAsync(string? search = null)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
        return await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id) => await _db.Users.FindAsync(id);

    public async Task<bool> CreateUserAsync(User user, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == user.Email)) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        var existing = await _db.Users.FindAsync(user.Id);
        if (existing == null) return false;
        existing.Name = user.Name;
        existing.Email = user.Email;
        existing.Phone = user.Phone;
        existing.Address = user.Address;
        existing.Role = user.Role;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(int id, string role)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;
        user.Role = role;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Room>> GetAllRoomsAsync() =>
        await _db.Rooms.Include(r => r.Cinema).OrderBy(r => r.CinemaId).ThenBy(r => r.Name).ToListAsync();

    public async Task<List<Room>> GetRoomsByCinemaAsync(int? cinemaId = null) =>
        await _db.Rooms.Include(r => r.Cinema)
            .Where(r => !cinemaId.HasValue || r.CinemaId == cinemaId)
            .OrderBy(r => r.CinemaId).ThenBy(r => r.Name).ToListAsync();

    public async Task<Room?> GetRoomByIdAsync(int id) =>
        await _db.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<bool> CreateRoomAsync(Room room)
    {
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        // Auto-generate seats
        string[] rows = GenerateRows(room.TotalRows);
        for (int col = 1; col <= room.TotalCols; col++)
        {
            foreach (var row in rows)
            {
                _db.Seats.Add(new Seat
                {
                    RoomId = room.Id,
                    RowLabel = row,
                    ColNumber = col,
                    Type = Array.IndexOf(rows, row) >= rows.Length - 2 ? "vip" : "standard"
                });
            }
        }
        await _db.SaveChangesAsync();
        return true;
    }

    private static string[] GenerateRows(int count)
    {
        var rows = new string[count];
        for (int i = 0; i < count; i++)
            rows[i] = ((char)('A' + i)).ToString();
        return rows;
    }

    public async Task<bool> UpdateRoomAsync(Room room)
    {
        var existing = await _db.Rooms.FindAsync(room.Id);
        if (existing == null) return false;
        existing.Name = room.Name;
        existing.CinemaId = room.CinemaId;
        existing.TotalRows = room.TotalRows;
        existing.TotalCols = room.TotalCols;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return false;
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Seat>> GetSeatsByRoomAsync(int roomId) =>
        await _db.Seats.Where(s => s.RoomId == roomId)
                       .OrderBy(s => s.RowLabel).ThenBy(s => s.ColNumber)
                       .ToListAsync();

    public async Task<bool> CreateSeatAsync(Seat seat)
    {
        if (await _db.Seats.AnyAsync(s => s.RoomId == seat.RoomId && s.RowLabel == seat.RowLabel && s.ColNumber == seat.ColNumber))
            return false;
        _db.Seats.Add(seat);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSeatAsync(Seat seat)
    {
        var existing = await _db.Seats.FindAsync(seat.Id);
        if (existing == null) return false;
        existing.Type = seat.Type;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSeatAsync(int id)
    {
        var seat = await _db.Seats.FindAsync(id);
        if (seat == null) return false;
        _db.Seats.Remove(seat);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Booking>> GetAllBookingsAsync(string? status = null, int? cinemaId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.BookingDetails).ThenInclude(bd => bd.Seat)
            .AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.Status == status);
        if (cinemaId.HasValue)
            query = query.Where(b => b.Showtime.Room.CinemaId == cinemaId);
        if (from.HasValue)
            query = query.Where(b => b.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(b => b.CreatedAt < to.Value.AddDays(1));
        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    public async Task<List<Cinema>> GetCinemasForFilterAsync() =>
        await _db.Cinemas.OrderBy(c => c.Name).ToListAsync();

    public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking == null) return false;
        booking.Status = status;
        await _db.SaveChangesAsync();

        var contact = _config["AppSettings:SupportContact"] ?? "hotline của chúng tôi";
        string title = status == "confirmed" ? "Vé đã được xác nhận" : "Vé đã bị hủy";
        string message = status == "confirmed"
            ? $"Đơn đặt vé #{bookingId} đã được xác nhận thành công. Chúc bạn xem phim vui vẻ!"
            : $"Đơn đặt vé #{bookingId} đã bị hủy bởi quản trị viên. Mọi thắc mắc vui lòng liên hệ với chúng tôi tại {contact}.";
        await _notifications.CreateBookingNotificationAsync(booking.UserId, bookingId, title, message);
        return true;
    }

    public async Task<List<Cinema>> GetAllCinemasAsync() =>
        await _db.Cinemas.Include(c => c.Rooms).OrderBy(c => c.Name).ToListAsync();

    public async Task<Cinema?> GetCinemaByIdAsync(int id) =>
        await _db.Cinemas.Include(c => c.Rooms).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> CreateCinemaAsync(Cinema cinema)
    {
        _db.Cinemas.Add(cinema);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCinemaAsync(Cinema cinema)
    {
        var existing = await _db.Cinemas.FindAsync(cinema.Id);
        if (existing == null) return false;
        existing.Name = cinema.Name;
        existing.Location = cinema.Location;
        existing.Hotline = cinema.Hotline;
        if (!string.IsNullOrEmpty(cinema.ImageUrl))
            existing.ImageUrl = cinema.ImageUrl;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCinemaAsync(int id)
    {
        var cinema = await _db.Cinemas.FindAsync(id);
        if (cinema == null) return false;
        _db.Cinemas.Remove(cinema);
        await _db.SaveChangesAsync();
        return true;
    }
}
