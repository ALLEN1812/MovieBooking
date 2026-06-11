namespace OnlineMovieBooking.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // user | admin
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
