namespace OnlineMovieBooking.Models;

public class Room
{
    public int Id { get; set; }
    public int CinemaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalCols { get; set; }

    public Cinema Cinema { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
