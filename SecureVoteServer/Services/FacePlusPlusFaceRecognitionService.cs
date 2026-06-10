using System.Text.Json;

namespace SecureVote.Services;

/// <summary>
/// Face recognition service using Face++ (Megvii) Compare API.
/// Sends both images as multipart/form-data and compares the confidence
/// score against a configurable threshold.
/// </summary>
public class FacePlusPlusFaceRecognitionService : IFaceRecognitionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _apiUrl;
    private readonly double _threshold;
    private readonly ILogger<FacePlusPlusFaceRecognitionService> _logger;

    public FacePlusPlusFaceRecognitionService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FacePlusPlusFaceRecognitionService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("FaceRecognition");
        _apiKey = configuration["FaceRecognition:FacePlusPlus:ApiKey"] ?? "";
        _apiSecret = configuration["FaceRecognition:FacePlusPlus:ApiSecret"] ?? "";
        _apiUrl = configuration["FaceRecognition:FacePlusPlus:ApiUrl"]
            ?? "https://api-us.faceplusplus.com/facepp/v3/compare";
        _threshold = double.TryParse(configuration["FaceRecognition:FacePlusPlus:Threshold"], out var t) ? t : 70.0;
        _logger = logger;
    }

    public async Task<bool> VerifyFaceAsync(string storedPhotoPath, byte[] selfieImage)
    {
        try
        {
            using var form = new MultipartFormDataContent();

            // 1. Add the stored reference photo
            var referenceBytes = await File.ReadAllBytesAsync(storedPhotoPath);
            var referenceContent = new ByteArrayContent(referenceBytes);
            referenceContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(referenceContent, "image_file1", "reference.jpg");

            // 2. Add the selfie (test image)
            var selfieContent = new ByteArrayContent(selfieImage);
            selfieContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(selfieContent, "image_file2", "selfie.jpg");

            // 3. Send to Face++ API (credentials as query params)
            var url = $"{_apiUrl}?api_key={_apiKey}&api_secret={_apiSecret}";
            _logger.LogInformation("Sending face verification request to Face++ API...");
            var response = await _httpClient.PostAsync(url, form);

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Face++ API response: {Response}", json);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Face++ API returned status {StatusCode}: {Response}", response.StatusCode, json);
                return false;
            }

            // 5. Parse response and check confidence against threshold
            // Face++ returns: { "confidence": 95.234, "thresholds": { "1e-3": 62.327, ... } }
            var result = JsonSerializer.Deserialize<FacePlusPlusResponse>(json);

            if (result?.Confidence == null)
            {
                _logger.LogWarning("Face++ API returned no confidence score");
                return false;
            }

            var isVerified = result.Confidence >= _threshold;
            _logger.LogInformation(
                "Face++ verification: confidence={Confidence}, threshold={Threshold}, verified={IsVerified}",
                result.Confidence, _threshold, isVerified);

            return isVerified;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face++ verification failed due to an error");
            return false;
        }
    }

    private class FacePlusPlusResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("thresholds")]
        public FacePlusPlusThresholds? Thresholds { get; set; }
    }

    private class FacePlusPlusThresholds
    {
        [System.Text.Json.Serialization.JsonPropertyName("1e-3")]
        public double? Threshold1e3 { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("1e-4")]
        public double? Threshold1e4 { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("1e-5")]
        public double? Threshold1e5 { get; set; }
    }
}
