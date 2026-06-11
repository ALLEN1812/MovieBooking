using Microsoft.EntityFrameworkCore;
using OnlineMovieBooking.Data;
using OnlineMovieBooking.Models;

namespace OnlineMovieBooking.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string email, string password);
    Task<(bool Success, string Message)> RegisterAsync(string name, string email, string? phone, string password);
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> UpdateProfileAsync(int userId, string name, string? phone, string? address);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<string?> GenerateResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;

    public AuthService(ApplicationDbContext db) => _db = db;

    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;
        return user;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string name, string email, string? phone, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return (false, "Email đã được sử dụng");

        var user = new User
        {
            Name = name,
            Email = email,
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "user"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (true, "Đăng ký thành công");
    }

    public async Task<User?> GetUserByIdAsync(int id) =>
        await _db.Users.FindAsync(id);

    public async Task<User?> GetUserByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> UpdateProfileAsync(int userId, string name, string? phone, string? address)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        user.Name = name;
        user.Phone = phone;
        user.Address = address;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GenerateResetTokenAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null;
        var token = Guid.NewGuid().ToString("N");
        user.ResetPasswordToken = token;
        user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.ResetPasswordToken == token && u.ResetPasswordExpiry > DateTime.UtcNow);
        if (user == null) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ResetPasswordToken = null;
        user.ResetPasswordExpiry = null;
        await _db.SaveChangesAsync();
        return true;
    }
}
