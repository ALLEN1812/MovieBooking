using System.ComponentModel.DataAnnotations;

namespace OnlineMovieBooking.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalMovies { get; set; }
    public int TotalBookings { get; set; }
    public int TotalShowtimes { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<Booking> RecentBookings { get; set; } = new();
}

public class MovieFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phim")]
    public string Title { get; set; } = string.Empty;

    public string? Genre { get; set; }

    [Range(1, 500, ErrorMessage = "Thời lượng không hợp lệ")]
    public int DurationMin { get; set; }

    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public IFormFile? PosterFile { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string Status { get; set; } = "now_showing";

    public DateOnly? ReleaseDate { get; set; }
    public string? Director { get; set; }
    public string? TrailerUrl { get; set; }
}

public class ShowtimeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phim")]
    public int MovieId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phòng chiếu")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giờ chiếu")]
    public DateTime StartTime { get; set; }

    [Range(0, 10000000)]
    public decimal Price { get; set; }

    public string? Subtitle { get; set; }

    public List<Movie> Movies { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
}

public class UserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string Role { get; set; } = "user";

    public string? Password { get; set; } // Only for create
}

public class RoomFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn rạp")]
    public int CinemaId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phòng")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 26, ErrorMessage = "Số hàng từ 1-26")]
    public int TotalRows { get; set; } = 5;

    [Range(1, 30, ErrorMessage = "Số cột từ 1-30")]
    public int TotalCols { get; set; } = 8;

    public List<Cinema> Cinemas { get; set; } = new();
}

public class CinemaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên rạp")]
    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }
    public string? Hotline { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
}

public class SeatFormViewModel
{
    public int Id { get; set; }
    public int RoomId { get; set; }

    [Required]
    public string RowLabel { get; set; } = string.Empty;

    [Range(1, 30)]
    public int ColNumber { get; set; }

    public string Type { get; set; } = "standard";
}
