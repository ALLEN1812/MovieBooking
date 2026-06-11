namespace OnlineMovieBooking.Models;

public class Cinema
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Hotline { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
