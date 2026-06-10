namespace SecureVote.Services;

/// <summary>
/// Mock implementation for development/testing.
/// Always returns true for face verification.
/// Replace with real AI service in production.
/// </summary>
public class MockFaceRecognitionService : IFaceRecognitionService
{
    public Task<bool> VerifyFaceAsync(string storedPhotoPath, byte[] selfieImage)
    {
        // TODO: Replace with real AI service (Face++, Azure, DeepFace)
        // For now, always return true for development
        return Task.FromResult(true);
    }
}
