using System.ComponentModel.DataAnnotations;

namespace OnlineMovieBooking.Models.ViewModels;

public class SeatMapViewModel
{
    public Showtime Showtime { get; set; } = null!;
    public List<Seat> Seats { get; set; } = new();
    public string RoomName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalCols { get; set; }
}

public class BookingConfirmViewModel
{
    public int ShowtimeId { get; set; }
    public List<int> SeatIds { get; set; } = new();
    public List<SelectedSeatInfo> SelectedSeats { get; set; } = new();
    public Showtime? Showtime { get; set; }
    public decimal TotalPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)[0-9]{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";

    public string? Note { get; set; }
}

public class SelectedSeatInfo
{
    public int SeatId { get; set; }
    public string Label { get; set; } = string.Empty; // e.g. A1, B3
    public string Type { get; set; } = "standard";
    public decimal Price { get; set; }
}

public class BookingSuccessViewModel
{
    public int BookingId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public List<string> SeatLabels { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
