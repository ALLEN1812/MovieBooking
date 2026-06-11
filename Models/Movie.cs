namespace OnlineMovieBooking.Models;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public int DurationMin { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string Status { get; set; } = "now_showing"; // now_showing | coming_soon
    public DateOnly? ReleaseDate { get; set; }
    public string? Director { get; set; }
    public string? TrailerUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
