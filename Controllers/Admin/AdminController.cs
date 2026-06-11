using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Models;
using OnlineMovieBooking.Models.ViewModels;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers.Admin;

[Authorize(Roles = "admin")]
[Route("Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _admin;
    private readonly IAuthService _auth;
    private readonly IMovieService _movies;
    private readonly ICloudinaryService _cloudinary;
    private readonly INotificationService _notifications;

    public AdminController(IAdminService admin, IAuthService auth, IMovieService movies, ICloudinaryService cloudinary, INotificationService notifications)
    {
        _admin = admin;
        _auth = auth;
        _movies = movies;
        _cloudinary = cloudinary;
        _notifications = notifications;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        var dashboard = await _admin.GetDashboardAsync();
        return View(dashboard);
    }

    // ─── Users ─────────────────────────────────────────────────────────
    [Route("Users")]
    public async Task<IActionResult> Users(string? search)
    {
        ViewBag.Search = search;
        var users = await _admin.GetAllUsersAsync(search);
        return View(users);
    }

    [HttpGet, Route("Users/Create")]
    public IActionResult CreateUser() => View(new UserFormViewModel());

    [HttpPost, Route("Users/Create")]
    public async Task<IActionResult> CreateUser(UserFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (string.IsNullOrEmpty(model.Password))
        {
            ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu");
            return View(model);
        }
        var user = new User { Name = model.Name, Email = model.Email, Phone = model.Phone, Address = model.Address, Role = model.Role };
        var ok = await _admin.CreateUserAsync(user, model.Password);
        if (!ok) { ModelState.AddModelError("Email", "Email đã tồn tại"); return View(model); }
        TempData["Success"] = "Tạo người dùng thành công!";
        return RedirectToAction("Users");
    }

    [HttpGet, Route("Users/Edit/{id}")]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _admin.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        var vm = new UserFormViewModel { Id = user.Id, Name = user.Name, Email = user.Email, Phone = user.Phone, Address = user.Address, Role = user.Role };
        return View(vm);
    }

    [HttpPost, Route("Users/Edit/{id}")]
    public async Task<IActionResult> EditUser(UserFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new User { Id = model.Id, Name = model.Name, Email = model.Email, Phone = model.Phone, Address = model.Address, Role = model.Role };
        await _admin.UpdateUserAsync(user);
        TempData["Success"] = "Cập nhật người dùng thành công!";
        return RedirectToAction("Users");
    }

    [HttpPost, Route("Users/Delete/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id == UserId) { TempData["Error"] = "Không thể xóa tài khoản của chính mình!"; return RedirectToAction("Users"); }
        await _admin.DeleteUserAsync(id);
        TempData["Success"] = "Đã xóa người dùng!";
        return RedirectToAction("Users");
    }

    // ─── Movies ────────────────────────────────────────────────────────
    [Route("Movies")]
    public async Task<IActionResult> Movies(string? search)
    {
        ViewBag.Search = search;
        var movies = await _movies.GetAllMoviesAsync(null, search, includeHidden: true);
        ViewBag.BookingCounts = await _movies.GetAllMovieBookingCountsAsync();
        return View(movies);
    }

    [HttpGet, Route("Movies/Create")]
    public IActionResult CreateMovie() => View(new MovieFormViewModel());

    [HttpPost, Route("Movies/Create")]
    public async Task<IActionResult> CreateMovie(MovieFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var posterUrl = await SavePosterAsync(model.PosterFile);
        var movie = new Movie
        {
            Title = model.Title, Genre = model.Genre, DurationMin = model.DurationMin,
            Description = model.Description, Status = model.Status,
            ReleaseDate = model.ReleaseDate, Director = model.Director,
            TrailerUrl = model.TrailerUrl,
            PosterUrl = posterUrl ?? model.PosterUrl
        };
        await _movies.CreateMovieAsync(movie);
        TempData["Success"] = "Thêm phim thành công!";
        return RedirectToAction("Movies");
    }

    [HttpGet, Route("Movies/Edit/{id}")]
    public async Task<IActionResult> EditMovie(int id)
    {
        var movie = await _movies.GetMovieByIdAsync(id);
        if (movie == null) return NotFound();
        return View(new MovieFormViewModel
        {
            Id = movie.Id, Title = movie.Title, Genre = movie.Genre, DurationMin = movie.DurationMin,
            Description = movie.Description, Status = movie.Status, PosterUrl = movie.PosterUrl,
            ReleaseDate = movie.ReleaseDate, Director = movie.Director, TrailerUrl = movie.TrailerUrl
        });
    }

    [HttpPost, Route("Movies/Edit/{id}")]
    public async Task<IActionResult> EditMovie(MovieFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var posterUrl = await SavePosterAsync(model.PosterFile);
        var movie = new Movie
        {
            Id = model.Id, Title = model.Title, Genre = model.Genre, DurationMin = model.DurationMin,
            Description = model.Description, Status = model.Status,
            ReleaseDate = model.ReleaseDate, Director = model.Director,
            TrailerUrl = model.TrailerUrl,
            PosterUrl = posterUrl ?? model.PosterUrl
        };
        await _movies.UpdateMovieAsync(movie);
        TempData["Success"] = "Cập nhật phim thành công!";
        return RedirectToAction("Movies");
    }

    [HttpPost, Route("Movies/Delete/{id}")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await _movies.GetMovieByIdAsync(id);
        if (movie == null) return RedirectToAction("Movies");

        var bookingCount = await _movies.GetMovieBookingCountAsync(id);
        var affectedUserIds = bookingCount > 0
            ? await _movies.GetAffectedUserIdsByMovieAsync(id)
            : [];

        await _movies.DeleteMovieAsync(id);

        foreach (var userId in affectedUserIds)
        {
            await _notifications.CreateNotificationAsync(
                userId,
                "Vé đặt đã bị hủy",
                $"Phim \"{movie.Title}\" đã bị xóa khỏi hệ thống. Vé đặt của bạn cho phim này đã được hủy tự động. Xin lỗi vì sự bất tiện này."
            );
        }

        TempData["Success"] = "Đã xóa phim!";
        if (bookingCount > 0)
            TempData["Warning"] = $"Đã hủy {bookingCount} lượt đặt vé và gửi thông báo đến {affectedUserIds.Count} khách hàng.";

        return RedirectToAction("Movies");
    }

    // ─── Showtimes ─────────────────────────────────────────────────────
    [Route("Showtimes")]
    public async Task<IActionResult> Showtimes()
    {
        var showtimes = await _movies.GetAllShowtimesAsync();
        return View(showtimes);
    }

    [HttpGet, Route("Showtimes/Create")]
    public async Task<IActionResult> CreateShowtime()
    {
        var vm = new ShowtimeFormViewModel
        {
            Movies = await _movies.GetAllMoviesAsync(includeHidden: true),
            Rooms = await _admin.GetAllRoomsAsync()
        };
        return View(vm);
    }

    [HttpPost, Route("Showtimes/Create")]
    public async Task<IActionResult> CreateShowtime(ShowtimeFormViewModel model)
    {
        model.Movies = await _movies.GetAllMoviesAsync(includeHidden: true);
        model.Rooms = await _admin.GetAllRoomsAsync();
        if (!ModelState.IsValid) return View(model);
        var showtime = new Showtime { MovieId = model.MovieId, RoomId = model.RoomId, StartTime = model.StartTime, Price = model.Price, Subtitle = model.Subtitle };
        await _movies.CreateShowtimeAsync(showtime);
        TempData["Success"] = "Thêm suất chiếu thành công!";
        return RedirectToAction("Showtimes");
    }

    [HttpGet, Route("Showtimes/Edit/{id}")]
    public async Task<IActionResult> EditShowtime(int id)
    {
        var s = await _movies.GetShowtimeByIdAsync(id);
        if (s == null) return NotFound();
        return View(new ShowtimeFormViewModel
        {
            Id = s.Id, MovieId = s.MovieId, RoomId = s.RoomId, StartTime = s.StartTime, Price = s.Price, Subtitle = s.Subtitle,
            Movies = await _movies.GetAllMoviesAsync(includeHidden: true), Rooms = await _admin.GetAllRoomsAsync()
        });
    }

    [HttpPost, Route("Showtimes/Edit/{id}")]
    public async Task<IActionResult> EditShowtime(ShowtimeFormViewModel model)
    {
        model.Movies = await _movies.GetAllMoviesAsync(includeHidden: true);
        model.Rooms = await _admin.GetAllRoomsAsync();
        if (!ModelState.IsValid) return View(model);
        var showtime = new Showtime { Id = model.Id, MovieId = model.MovieId, RoomId = model.RoomId, StartTime = model.StartTime, Price = model.Price, Subtitle = model.Subtitle };
        await _movies.UpdateShowtimeAsync(showtime);
        TempData["Success"] = "Cập nhật suất chiếu thành công!";
        return RedirectToAction("Showtimes");
    }

    [HttpPost, Route("Showtimes/Delete/{id}")]
    public async Task<IActionResult> DeleteShowtime(int id)
    {
        await _movies.DeleteShowtimeAsync(id);
        TempData["Success"] = "Đã xóa suất chiếu!";
        return RedirectToAction("Showtimes");
    }

    // ─── Rooms ─────────────────────────────────────────────────────────
    [Route("Rooms")]
    public async Task<IActionResult> Rooms(int? cinemaId)
    {
        ViewBag.CinemaId = cinemaId;
        ViewBag.Cinemas  = await _admin.GetCinemasForFilterAsync();
        var rooms = await _admin.GetRoomsByCinemaAsync(cinemaId);
        return View(rooms);
    }

    [HttpGet, Route("Rooms/Create")]
    public async Task<IActionResult> CreateRoom()
    {
        return View(new RoomFormViewModel { Cinemas = await _admin.GetAllCinemasAsync() });
    }

    [HttpPost, Route("Rooms/Create")]
    public async Task<IActionResult> CreateRoom(RoomFormViewModel model)
    {
        model.Cinemas = await _admin.GetAllCinemasAsync();
        if (!ModelState.IsValid) return View(model);
        var room = new Room { CinemaId = model.CinemaId, Name = model.Name, TotalRows = model.TotalRows, TotalCols = model.TotalCols };
        await _admin.CreateRoomAsync(room);
        TempData["Success"] = "Tạo phòng chiếu thành công!";
        return RedirectToAction("Rooms");
    }

    [HttpPost, Route("Rooms/Delete/{id}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        await _admin.DeleteRoomAsync(id);
        TempData["Success"] = "Đã xóa phòng chiếu!";
        return RedirectToAction("Rooms");
    }

    // ─── Seats ─────────────────────────────────────────────────────────
    [Route("Rooms/{roomId}/Seats")]
    public async Task<IActionResult> Seats(int roomId)
    {
        var room = await _admin.GetRoomByIdAsync(roomId);
        if (room == null) return NotFound();
        var seats = await _admin.GetSeatsByRoomAsync(roomId);
        ViewBag.Room = room;
        return View(seats);
    }

    [HttpPost, Route("Seats/UpdateType")]
    public async Task<IActionResult> UpdateSeatType(int id, string type, int roomId)
    {
        await _admin.UpdateSeatAsync(new Models.Seat { Id = id, Type = type });
        return RedirectToAction("Seats", new { roomId });
    }

    // ─── Bookings ──────────────────────────────────────────────────────
    [Route("Bookings")]
    public async Task<IActionResult> Bookings(string? status, int? cinemaId, string? from, string? to)
    {
        DateTime? fromDate = DateTime.TryParse(from, out var fd) ? fd : null;
        DateTime? toDate   = DateTime.TryParse(to,   out var td) ? td : null;
        ViewBag.Status   = status;
        ViewBag.CinemaId = cinemaId;
        ViewBag.From     = from;
        ViewBag.To       = to;
        ViewBag.Cinemas  = await _admin.GetCinemasForFilterAsync();
        var bookings = await _admin.GetAllBookingsAsync(status, cinemaId, fromDate, toDate);
        return View(bookings);
    }

    [HttpPost, Route("Bookings/UpdateStatus")]
    public async Task<IActionResult> UpdateBookingStatus(int id, string status)
    {
        await _admin.UpdateBookingStatusAsync(id, status);
        TempData["Success"] = "Cập nhật trạng thái thành công!";
        return RedirectToAction("Bookings");
    }

    // ─── Cinemas ───────────────────────────────────────────────────────
    [Route("Cinemas")]
    public async Task<IActionResult> Cinemas()
    {
        var cinemas = await _admin.GetAllCinemasAsync();
        return View(cinemas);
    }

    [HttpGet, Route("Cinemas/Create")]
    public IActionResult CreateCinema() => View(new CinemaFormViewModel());

    [HttpPost, Route("Cinemas/Create")]
    public async Task<IActionResult> CreateCinema(CinemaFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var imageUrl = await SavePosterAsync(model.ImageFile);
        var cinema = new Cinema { Name = model.Name, Location = model.Location, Hotline = model.Hotline, ImageUrl = imageUrl ?? model.ImageUrl };
        await _admin.CreateCinemaAsync(cinema);
        TempData["Success"] = "Thêm rạp chiếu thành công!";
        return RedirectToAction("Cinemas");
    }

    [HttpGet, Route("Cinemas/Edit/{id}")]
    public async Task<IActionResult> EditCinema(int id)
    {
        var cinema = await _admin.GetCinemaByIdAsync(id);
        if (cinema == null) return NotFound();
        return View(new CinemaFormViewModel { Id = cinema.Id, Name = cinema.Name, Location = cinema.Location, Hotline = cinema.Hotline, ImageUrl = cinema.ImageUrl });
    }

    [HttpPost, Route("Cinemas/Edit/{id}")]
    public async Task<IActionResult> EditCinema(CinemaFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var imageUrl = await SavePosterAsync(model.ImageFile);
        var cinema = new Cinema { Id = model.Id, Name = model.Name, Location = model.Location, Hotline = model.Hotline, ImageUrl = imageUrl ?? model.ImageUrl };
        await _admin.UpdateCinemaAsync(cinema);
        TempData["Success"] = "Cập nhật rạp chiếu thành công!";
        return RedirectToAction("Cinemas");
    }

    [HttpPost, Route("Cinemas/Delete/{id}")]
    public async Task<IActionResult> DeleteCinema(int id)
    {
        await _admin.DeleteCinemaAsync(id);
        TempData["Success"] = "Đã xóa rạp chiếu!";
        return RedirectToAction("Cinemas");
    }

    // ─── Profile ───────────────────────────────────────────────────────
    [Route("Profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _auth.GetUserByIdAsync(UserId);
        if (user == null) return NotFound();
        var vm = new UpdateProfileViewModel { Name = user.Name, Phone = user.Phone, Address = user.Address };
        ViewBag.Email = user.Email;
        return View(vm);
    }

    [HttpPost, Route("Profile")]
    public async Task<IActionResult> Profile(UpdateProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        await _auth.UpdateProfileAsync(UserId, model.Name, model.Phone, model.Address);
        TempData["Success"] = "Cập nhật thông tin thành công!";
        return RedirectToAction("Profile");
    }

    [HttpGet, Route("ChangePassword")]
    public IActionResult ChangePassword() => View();

    [HttpPost, Route("ChangePassword")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var success = await _auth.ChangePasswordAsync(UserId, model.CurrentPassword, model.NewPassword);
        if (!success) { ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác"); return View(model); }
        TempData["Success"] = "Đổi mật khẩu thành công!";
        return RedirectToAction("ChangePassword");
    }

    // ─── Helper ────────────────────────────────────────────────────────
    private async Task<string?> SavePosterAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        return await _cloudinary.UploadImageAsync(file, "movies");
    }
}
