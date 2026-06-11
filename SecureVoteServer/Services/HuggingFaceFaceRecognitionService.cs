using System.Text.Json;

namespace SecureVote.Services;

/// <summary>
/// Face recognition service using the HuggingFace-hosted model.
/// Sends both images as multipart/form-data to the external API.
/// </summary>
public class HuggingFaceFaceRecognitionService : IFaceRecognitionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly ILogger<HuggingFaceFaceRecognitionService> _logger;

    public HuggingFaceFaceRecognitionService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HuggingFaceFaceRecognitionService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("FaceRecognition");
        _apiUrl = configuration["FaceRecognition:HuggingFaceUrl"]
            ?? "https://abdelrahman1111121-face-recognition.hf.space/verify_images";
        _logger = logger;
    }

    public async Task<bool> VerifyFaceAsync(byte[] storedPhotoBytes, byte[] selfieImage)
    {
        try
        {
            using var form = new MultipartFormDataContent();

            // 1. Add the stored reference photo (already as bytes)
            var referenceContent = new ByteArrayContent(storedPhotoBytes);
            referenceContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(referenceContent, "reference_image", "reference.jpg");

            // 2. Add the selfie (test image)
            var selfieContent = new ByteArrayContent(selfieImage);
            selfieContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(selfieContent, "test_image", "selfie.jpg");

            // 3. Send to HuggingFace API
            _logger.LogInformation("Sending face verification request to HuggingFace API...");
            var response = await _httpClient.PostAsync(_apiUrl, form);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HuggingFace API returned status {StatusCode}", response.StatusCode);
                return false;
            }

            // 4. Parse response: { "is_verified": true/false }
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("HuggingFace API response: {Response}", json);

            var result = JsonSerializer.Deserialize<HuggingFaceResponse>(json);
            return result?.IsVerified ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face verification failed due to an error");
            return false;
        }
    }

    private class HuggingFaceResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("is_verified")]
        public bool IsVerified { get; set; }
    }
}
