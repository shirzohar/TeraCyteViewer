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
        private readonly HttpClient httpClient;
        private static readonly string loginUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/auth/login";
        private static readonly string refreshUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/auth/refresh";

        public AuthState AuthState { get; private set; } = new AuthState();

        public AuthService()
        {
            httpClient = new HttpClient();
        }

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

                AuthState = new AuthState
                {
                    AccessToken = loginResponse.AccessToken,
                    RefreshToken = loginResponse.RefreshToken,
                    Expiration = DateTime.UtcNow.AddSeconds(loginResponse.ExpiresIn - 60),
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(30)
                };

                return loginResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Login error: {ex.Message}");
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

                var refreshRequest = new { refresh_token = AuthState.RefreshToken };
                var json = JsonConvert.SerializeObject(refreshRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(refreshUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Token refresh failed: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var refreshResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                if (refreshResponse == null)
                {
                    throw new Exception("Failed to deserialize refresh response");
                }

                AuthState.AccessToken = refreshResponse.AccessToken;
                AuthState.RefreshToken = refreshResponse.RefreshToken;
                AuthState.Expiration = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn - 60);
                AuthState.RefreshTokenExpiration = DateTime.UtcNow.AddDays(30);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Token refresh error: {ex.Message}");
            }
        }

        public string GetAccessToken()
        {
            return AuthState.AccessToken ?? string.Empty;
        }

        public bool IsTokenExpired()
        {
            return AuthState.IsExpired;
        }

        public bool IsRefreshTokenExpired()
        {
            return AuthState.IsRefreshTokenExpired;
        }
    }
}
