namespace SecureVote.Services;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder);
    Task DeleteImageAsync(string publicId);
}
