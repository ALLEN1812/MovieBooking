using Microsoft.EntityFrameworkCore;
using OnlineMovieBooking.Data;
using OnlineMovieBooking.Models;

namespace OnlineMovieBooking.Services;

public interface IMovieService
{
    Task<List<Movie>> GetAllMoviesAsync(string? status = null, string? search = null, bool includeHidden = false);
    Task<Movie?> GetMovieByIdAsync(int id);
    Task<List<Showtime>> GetShowtimesByMovieAsync(int movieId);
    Task<Showtime?> GetShowtimeByIdAsync(int id);
    Task<List<Seat>> GetSeatsByShowtimeAsync(int showtimeId);
    Task<Movie> CreateMovieAsync(Movie movie);
    Task<bool> UpdateMovieAsync(Movie movie);
    Task<bool> DeleteMovieAsync(int id);
    Task<List<Cinema>> GetAllCinemasAsync();
    Task<Cinema?> GetCinemaByIdAsync(int id);
    Task<List<Showtime>> GetCinemaScheduleAsync(int cinemaId, DateTime date);
    Task<int> GetMovieBookingCountAsync(int movieId);
    Task<Dictionary<int, int>> GetAllMovieBookingCountsAsync();
    Task<List<int>> GetAffectedUserIdsByMovieAsync(int movieId);
    Task<List<Showtime>> GetAllShowtimesAsync();
    Task<Showtime> CreateShowtimeAsync(Showtime showtime);
    Task<bool> UpdateShowtimeAsync(Showtime showtime);
    Task<bool> DeleteShowtimeAsync(int id);
}

public class MovieService : IMovieService
{
    private readonly ApplicationDbContext _db;

    public MovieService(ApplicationDbContext db) => _db = db;

    public async Task<List<Movie>> GetAllMoviesAsync(string? status = null, string? search = null, bool includeHidden = false)
    {
        var query = _db.Movies.AsQueryable();
        if (!includeHidden)
            query = query.Where(m => m.Status != "hidden");
        if (!string.IsNullOrEmpty(status))
            query = query.Where(m => m.Status == status);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(m => m.Title.Contains(search) || (m.Genre != null && m.Genre.Contains(search)));
        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<Movie?> GetMovieByIdAsync(int id) =>
        await _db.Movies.Include(m => m.Showtimes).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                        .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<List<Showtime>> GetShowtimesByMovieAsync(int movieId) =>
        await _db.Showtimes
                 .Include(s => s.Room).ThenInclude(r => r.Cinema)
                 .Where(s => s.MovieId == movieId && s.StartTime > DateTime.Now)
                 .OrderBy(s => s.StartTime)
                 .ToListAsync();

    public async Task<Showtime?> GetShowtimeByIdAsync(int id) =>
        await _db.Showtimes
                 .Include(s => s.Movie)
                 .Include(s => s.Room).ThenInclude(r => r.Cinema)
                 .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<Seat>> GetSeatsByShowtimeAsync(int showtimeId)
    {
        var showtime = await _db.Showtimes.Include(s => s.Room).FirstOrDefaultAsync(s => s.Id == showtimeId);
        if (showtime == null) return new();

        var bookedSeatIds = await _db.BookingDetails
            .Include(bd => bd.Booking)
            .Where(bd => bd.Booking.ShowtimeId == showtimeId && bd.Booking.Status != "cancelled")
            .Select(bd => bd.SeatId)
            .ToListAsync();

        var seats = await _db.Seats
            .Where(s => s.RoomId == showtime.RoomId)
            .OrderBy(s => s.RowLabel).ThenBy(s => s.ColNumber)
            .ToListAsync();

        foreach (var seat in seats)
            seat.IsBooked = bookedSeatIds.Contains(seat.Id);

        return seats;
    }

    public async Task<Movie> CreateMovieAsync(Movie movie)
    {
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();
        return movie;
    }

    public async Task<bool> UpdateMovieAsync(Movie movie)
    {
        var existing = await _db.Movies.FindAsync(movie.Id);
        if (existing == null) return false;
        existing.Title = movie.Title;
        existing.Genre = movie.Genre;
        existing.DurationMin = movie.DurationMin;
        existing.Description = movie.Description;
        existing.Status = movie.Status;
        existing.ReleaseDate = movie.ReleaseDate;
        existing.Director = movie.Director;
        existing.TrailerUrl = movie.TrailerUrl;
        if (!string.IsNullOrEmpty(movie.PosterUrl))
            existing.PosterUrl = movie.PosterUrl;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMovieAsync(int id)
    {
        var movie = await _db.Movies
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.Bookings)
                    .ThenInclude(b => b.Notifications)
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.Bookings)
                    .ThenInclude(b => b.BookingDetails)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (movie == null) return false;

        foreach (var showtime in movie.Showtimes)
        {
            foreach (var booking in showtime.Bookings)
            {
                _db.Notifications.RemoveRange(booking.Notifications);
                _db.BookingDetails.RemoveRange(booking.BookingDetails);
            }
            _db.Bookings.RemoveRange(showtime.Bookings);
        }
        _db.Showtimes.RemoveRange(movie.Showtimes);
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Cinema>> GetAllCinemasAsync() =>
        await _db.Cinemas.OrderBy(c => c.Name).ToListAsync();

    public async Task<Cinema?> GetCinemaByIdAsync(int id) =>
        await _db.Cinemas.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Showtime>> GetCinemaScheduleAsync(int cinemaId, DateTime date) =>
        await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .Where(s => s.Room.CinemaId == cinemaId && s.StartTime.Date == date.Date)
            .OrderBy(s => s.Movie.Title)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

    public async Task<int> GetMovieBookingCountAsync(int movieId) =>
        await _db.Bookings.CountAsync(b => b.Showtime.MovieId == movieId && b.Status != "cancelled");

    public async Task<Dictionary<int, int>> GetAllMovieBookingCountsAsync() =>
        await _db.Bookings
            .Where(b => b.Status != "cancelled")
            .GroupBy(b => b.Showtime.MovieId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

    public async Task<List<int>> GetAffectedUserIdsByMovieAsync(int movieId) =>
        await _db.Bookings
            .Where(b => b.Showtime.MovieId == movieId && b.Status != "cancelled")
            .Select(b => b.UserId)
            .Distinct()
            .ToListAsync();

    public async Task<List<Showtime>> GetAllShowtimesAsync() =>
        await _db.Showtimes.Include(s => s.Movie).Include(s => s.Room).ThenInclude(r => r.Cinema)
                           .OrderByDescending(s => s.StartTime).ToListAsync();

    public async Task<Showtime> CreateShowtimeAsync(Showtime showtime)
    {
        _db.Showtimes.Add(showtime);
        await _db.SaveChangesAsync();
        return showtime;
    }

    public async Task<bool> UpdateShowtimeAsync(Showtime showtime)
    {
        var existing = await _db.Showtimes.FindAsync(showtime.Id);
        if (existing == null) return false;
        existing.MovieId = showtime.MovieId;
        existing.RoomId = showtime.RoomId;
        existing.StartTime = showtime.StartTime;
        existing.Price = showtime.Price;
        existing.Subtitle = showtime.Subtitle;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteShowtimeAsync(int id)
    {
        var showtime = await _db.Showtimes
            .Include(s => s.Bookings)
                .ThenInclude(b => b.Notifications)
            .Include(s => s.Bookings)
                .ThenInclude(b => b.BookingDetails)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (showtime == null) return false;

        foreach (var booking in showtime.Bookings)
        {
            _db.Notifications.RemoveRange(booking.Notifications);
            _db.BookingDetails.RemoveRange(booking.BookingDetails);
        }
        _db.Bookings.RemoveRange(showtime.Bookings);
        _db.Showtimes.Remove(showtime);
        await _db.SaveChangesAsync();
        return true;
    }
}
