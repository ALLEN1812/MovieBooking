namespace OnlineMovieBooking.Models;

public class Seat
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RowLabel { get; set; } = string.Empty; // A, B, C, D, E
    public int ColNumber { get; set; }
    public string Type { get; set; } = "standard"; // standard | vip
    public bool IsBooked { get; set; } = false;

    public Room Room { get; set; } = null!;
    public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
}
