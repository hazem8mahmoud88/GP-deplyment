using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace SecureVote.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"]!;
        var apiKey = configuration["Cloudinary:ApiKey"]!;
        var apiSecret = configuration["Cloudinary:ApiSecret"]!;

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, imageStream),
            Folder = $"securevote/{folder}",
            PublicId = Path.GetFileNameWithoutExtension(fileName),
            Overwrite = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }

    public async Task DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrEmpty(publicId)) return;
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }
}
