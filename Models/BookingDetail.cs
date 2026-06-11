namespace OnlineMovieBooking.Models;

public class BookingDetail
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int SeatId { get; set; }
    public decimal Price { get; set; }

    public Booking Booking { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}
