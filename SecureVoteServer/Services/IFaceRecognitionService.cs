namespace SecureVote.Services;

public interface IFaceRecognitionService
{
    /// <summary>
    /// Compares a stored voter photo with a live selfie.
    /// Returns true if similarity is above threshold (80%).
    /// </summary>
    Task<bool> VerifyFaceAsync(string storedPhotoPath, byte[] selfieImage);
}
