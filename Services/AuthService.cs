using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public class AuthService
    {
        private static readonly string loginUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/auth/login";
        private static readonly string refreshUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/auth/refresh";
        private static readonly HttpClient httpClient = new HttpClient();

        public AuthState AuthState { get; private set; } = new AuthState();

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var json = JsonConvert.SerializeObject(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(loginUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Login failed: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                if (loginResponse == null)
                {
                    throw new Exception("Failed to deserialize login response");
                }

                if (string.IsNullOrEmpty(loginResponse.AccessToken) || string.IsNullOrEmpty(loginResponse.RefreshToken))
                {
                    throw new Exception("Invalid tokens received from server");
                }

                AuthState = new AuthState
                {
                    AccessToken = loginResponse.AccessToken,
                    RefreshToken = loginResponse.RefreshToken,
                    Expiration = DateTime.UtcNow.AddSeconds(loginResponse.ExpiresIn - 60) // Refresh 1 minute before expiry
                };

                return loginResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error during login: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Login request timed out");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid response format: {ex.Message}");
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AuthState.RefreshToken))
                {
                    throw new Exception("No refresh token available.");
                }

                var refreshRequest = new
                {
                    refresh_token = AuthState.RefreshToken
                };

                var json = JsonConvert.SerializeObject(refreshRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(refreshUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Token refresh failed: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                if (loginResponse == null)
                {
                    throw new Exception("Failed to deserialize refresh response");
                }

                if (string.IsNullOrEmpty(loginResponse.AccessToken) || string.IsNullOrEmpty(loginResponse.RefreshToken))
                {
                    throw new Exception("Invalid tokens received during refresh");
                }

                AuthState = new AuthState
                {
                    AccessToken = loginResponse.AccessToken,
                    RefreshToken = loginResponse.RefreshToken,
                    Expiration = DateTime.UtcNow.AddSeconds(loginResponse.ExpiresIn - 60)
                };

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error during token refresh: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Token refresh request timed out");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid refresh response format: {ex.Message}");
            }
        }

        public string GetAccessToken()
        {
            if (string.IsNullOrEmpty(AuthState.AccessToken))
            {
                throw new Exception("No access token available");
            }
            return AuthState.AccessToken;
        }

        public bool IsTokenExpired()
        {
            return AuthState.IsExpired;
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(AuthState.AccessToken) && !AuthState.IsExpired;
        }

        public void ClearAuthState()
        {
            AuthState = new AuthState();
        }
    }
}
