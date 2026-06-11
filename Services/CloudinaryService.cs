using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace OnlineMovieBooking.Services;

public interface ICloudinaryService
{
    Task<string?> UploadImageAsync(IFormFile file, string folder = "movies");
    Task<bool> DeleteImageAsync(string publicId);
}

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string?> UploadImageAsync(IFormFile file, string folder = "movies")
    {
        if (file == null || file.Length == 0) return null;

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"moviebooking/{folder}",
            Transformation = new Transformation()
                .Width(400).Height(600).Crop("fill").Quality("auto").FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        return result.Error == null ? result.SecureUrl.ToString() : null;
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }
}
