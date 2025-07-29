using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public class ImageService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        private const string ImageUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/image";
        private const int MaxRetries = 3;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        public ImageService(AuthService authService)
        {
            _authService = authService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<ImageData> GetLatestImageAsync()
        {
            int retryCount = 0;

            while (retryCount < MaxRetries)
            {
                try
                {
                    // Ensure we have a valid token
                    if (_authService.IsTokenExpired())
                    {
                        await _authService.RefreshTokenAsync();
                    }

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _authService.GetAccessToken());

                    var response = await _httpClient.GetAsync(ImageUrl);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Token might be invalid, try to refresh
                        try
                        {
                            await _authService.RefreshTokenAsync();
                            _httpClient.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Bearer", _authService.GetAccessToken());
                            
                            // Retry the request
                            response = await _httpClient.GetAsync(ImageUrl);
                        }
                        catch
                        {
                            throw new Exception("Authentication failed after token refresh");
                        }
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Failed to fetch image: {response.StatusCode} - {errorContent}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var parsed = JsonConvert.DeserializeObject<ImageApiResponse>(json);
                    
                    if (parsed == null)
                    {
                        throw new Exception("Failed to deserialize image response");
                    }
                    
                    if (string.IsNullOrEmpty(parsed.ImageDataBase64))
                    {
                        throw new Exception("Empty image data received");
                    }

                    return new ImageData
                    {
                        ImageId = parsed.ImageId,
                        Timestamp = parsed.Timestamp,
                        Image = Base64ToBitmap(parsed.ImageDataBase64)
                    };
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("401"))
                {
                    throw new Exception("Authentication failed - please re-authenticate");
                }
                catch (TaskCanceledException)
                {
                    retryCount++;
                    if (retryCount >= MaxRetries)
                    {
                        throw new Exception("Image request timed out after multiple attempts");
                    }
                    await Task.Delay(RetryDelay);
                }
                catch (Exception) when (retryCount < MaxRetries - 1)
                {
                    retryCount++;
                    await Task.Delay(RetryDelay);
                }
                catch (Exception)
                {
                    throw; // Re-throw if we've exhausted retries
                }
            }

            throw new Exception("Failed to fetch image after all retry attempts");
        }

        private BitmapImage Base64ToBitmap(string base64)
        {
            try
            {
                byte[] binaryData = Convert.FromBase64String(base64);
                var image = new BitmapImage();

                using (var stream = new System.IO.MemoryStream(binaryData))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                }

                return image;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decode image data: {ex.Message}");
            }
        }

        private class ImageApiResponse
        {
            [JsonProperty("image_id")]
            public string? ImageId { get; set; }

            [JsonProperty("timestamp")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("image_data_base64")]
            public string? ImageDataBase64 { get; set; }
        }
    }
}
