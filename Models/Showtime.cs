namespace OnlineMovieBooking.Models;

public class Showtime
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
    public string? Subtitle { get; set; } // Vietsub | Thuyết minh | Phụ đề Anh

    public Movie Movie { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
