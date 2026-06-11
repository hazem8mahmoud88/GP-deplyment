using System.Text.Json;

namespace SecureVote.Services;

/// <summary>
/// Face recognition service using a custom model hosted on Railway.
/// Sends both images as multipart/form-data and expects { "is_verified": true/false }.
/// </summary>
public class RailwayFaceRecognitionService : IFaceRecognitionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly ILogger<RailwayFaceRecognitionService> _logger;

    public RailwayFaceRecognitionService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RailwayFaceRecognitionService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("FaceRecognition");
        _apiUrl = configuration["FaceRecognition:RailwayUrl"]
            ?? "https://web-production-0502c.up.railway.app/verify_images";
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

            // 3. Send to Railway API
            _logger.LogInformation("Sending face verification request to Railway API at {Url}...", _apiUrl);
            var response = await _httpClient.PostAsync(_apiUrl, form);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Railway API returned status {StatusCode}: {Response}", response.StatusCode, errorBody);
                return false;
            }

            // 4. Parse response: { "is_verified": true/false }
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Railway API response: {Response}", json);

            var result = JsonSerializer.Deserialize<RailwayResponse>(json);
            return result?.IsVerified ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Railway face verification failed due to an error");
            return false;
        }
    }

    private class RailwayResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("is_verified")]
        public bool IsVerified { get; set; }
    }
}
