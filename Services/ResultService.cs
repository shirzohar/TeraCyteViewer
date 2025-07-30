using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public class ResultService : IResultService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        private const string ResultUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/results";
        private const int MaxRetries = 3;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        public ResultService(IAuthService authService)
        {
            _authService = authService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<ResultData?> GetLatestResultsAsync()
        {
            int retryCount = 0;

            while (retryCount < MaxRetries)
            {
                try
                {
                    if (_authService.IsTokenExpired())
                    {
                        await _authService.RefreshTokenAsync();
                    }

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _authService.GetAccessToken());

                    var response = await _httpClient.GetAsync(ResultUrl);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        try
                        {
                            await _authService.RefreshTokenAsync();
                            _httpClient.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Bearer", _authService.GetAccessToken());
                            
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
                    
                    // Handle Infinity values in JSON
                    json = json.Replace("Infinity", "\"Infinity\"").Replace("-Infinity", "\"-Infinity\"");
                    
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
                    throw;
                }
            }

            throw new Exception("Failed to fetch results after all retry attempts");
        }
    }
}
