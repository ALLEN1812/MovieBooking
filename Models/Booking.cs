namespace OnlineMovieBooking.Models;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ShowtimeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD"; // COD | MoMo | VNPay
    public string? CardNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "pending"; // pending | confirmed | cancelled
    public string? TicketDelivery { get; set; }
    public string? Note { get; set; }
    public string? BillImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
    public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
