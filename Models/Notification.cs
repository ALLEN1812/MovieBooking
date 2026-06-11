namespace OnlineMovieBooking.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? BookingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "booking";
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Booking? Booking { get; set; }
}
