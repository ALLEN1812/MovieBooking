using Microsoft.EntityFrameworkCore;
using OnlineMovieBooking.Models;

namespace OnlineMovieBooking.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Cinema> Cinemas { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingDetail> BookingDetails { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("user");
        });

        modelBuilder.Entity<Seat>(e =>
        {
            e.HasIndex(s => new { s.RoomId, s.RowLabel, s.ColNumber }).IsUnique();
        });

        modelBuilder.Entity<Showtime>(e =>
        {
            e.Property(s => s.Price).HasColumnType("decimal(18,2)");
        });

        // SQL Server không cho phép nhiều cascade path đến cùng 1 bảng
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(n => n.Booking)
             .WithMany(b => b.Notifications)
             .HasForeignKey(n => n.BookingId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
            e.Property(b => b.Status).HasDefaultValue("pending");

            e.HasOne(b => b.User)
             .WithMany(u => u.Bookings)
             .HasForeignKey(b => b.UserId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BookingDetail>(e =>
        {
            e.Property(d => d.Price).HasColumnType("decimal(18,2)");

            e.HasOne(d => d.Booking)
             .WithMany(b => b.BookingDetails)
             .HasForeignKey(d => d.BookingId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.Seat)
             .WithMany(s => s.BookingDetails)
             .HasForeignKey(d => d.SeatId)
             .OnDelete(DeleteBehavior.Restrict);
        });

    }
}
