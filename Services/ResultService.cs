using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public class ResultService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        private const string ResultUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/results";
        private const int MaxRetries = 3;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        public ResultService(AuthService authService)
        {
            _authService = authService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<ResultData> GetLatestResultsAsync()
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

                    var response = await _httpClient.GetAsync(ResultUrl);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Token might be invalid, try to refresh
                        try
                        {
                            await _authService.RefreshTokenAsync();
                            _httpClient.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Bearer", _authService.GetAccessToken());
                            
                            // Retry the request
                            response = await _httpClient.GetAsync(ResultUrl);
                        }
                        catch
                        {
                            throw new Exception("Authentication failed after token refresh");
                        }
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Failed to fetch results: {response.StatusCode} - {errorContent}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ResultData>(json);

                    if (result == null)
                    {
                        throw new Exception("Failed to deserialize result data");
                    }

                    if (string.IsNullOrEmpty(result.ImageId))
                    {
                        throw new Exception("Invalid result data - missing image ID");
                    }

                    return result;
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
                        throw new Exception("Results request timed out after multiple attempts");
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

            throw new Exception("Failed to fetch results after all retry attempts");
        }
    }
}
