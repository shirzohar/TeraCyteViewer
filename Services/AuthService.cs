using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using TeraCyteViewer.Models;
using System.Collections.Generic;

namespace TeraCyteViewer.Services
{
    public class AuthService
    {
        private readonly HttpClient httpClient;
        private static readonly string loginUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/auth/login";
        private static readonly string refreshUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/auth/refresh";
        private static readonly string meUrl = "https://teracyte-assignment-server-764836180308.us-central1.run.app/api/auth/me";
        private static readonly string tokensFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeraCyteViewer", "tokens.dat");
        private static readonly string historyFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeraCyteViewer", "history.dat");

        public AuthState AuthState { get; private set; } = new AuthState();
        public UserInfo? CurrentUser { get; private set; }

        public AuthService()
        {
            httpClient = new HttpClient();
            LoadStoredTokens();
        }

        private void LoadStoredTokens()
        {
            try
            {
                if (File.Exists(tokensFilePath))
                {
                    var encryptedData = File.ReadAllBytes(tokensFilePath);
                    var decryptedJson = DecryptData(encryptedData);
                    var storedAuthState = JsonConvert.DeserializeObject<AuthState>(decryptedJson);
                    
                    if (storedAuthState != null && !storedAuthState.IsExpired)
                    {
                        AuthState = storedAuthState;
                    }
                }
            }
            catch
            {
                // If loading fails, start with fresh state
                AuthState = new AuthState();
            }
        }

        private void SaveTokens()
        {
            try
            {
                var directory = Path.GetDirectoryName(tokensFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(AuthState);
                var encryptedData = EncryptData(json);
                File.WriteAllBytes(tokensFilePath, encryptedData);
            }
            catch
            {
                // If saving fails, continue without persistence
            }
        }

        private byte[] EncryptData(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        }

        private string DecryptData(byte[] encryptedData)
        {
            var decryptedBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        public void ClearStoredTokens()
        {
            try
            {
                if (File.Exists(tokensFilePath))
                {
                    File.Delete(tokensFilePath);
                }
            }
            catch
            {
                // If deletion fails, continue
            }
            
            AuthState = new AuthState();
            CurrentUser = null;
        }

        public void SaveHistory(List<HistoryItem> history)
        {
            try
            {
                var directory = Path.GetDirectoryName(historyFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(history);
                var encryptedData = EncryptData(json);
                File.WriteAllBytes(historyFilePath, encryptedData);
            }
            catch
            {
                // If saving fails, continue without persistence
            }
        }

        public List<HistoryItem> LoadHistory()
        {
            try
            {
                if (File.Exists(historyFilePath))
                {
                    var encryptedData = File.ReadAllBytes(historyFilePath);
                    var decryptedJson = DecryptData(encryptedData);
                    var history = JsonConvert.DeserializeObject<List<HistoryItem>>(decryptedJson);
                    return history ?? new List<HistoryItem>();
                }
            }
            catch
            {
                // If loading fails, return empty list
            }
            
            return new List<HistoryItem>();
        }

        public void ClearStoredHistory()
        {
            try
            {
                if (File.Exists(historyFilePath))
                {
                    File.Delete(historyFilePath);
                }
            }
            catch
            {
                // If deletion fails, continue
            }
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

                SaveTokens();

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

                SaveTokens();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Token refresh error: {ex.Message}");
            }
        }

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AuthState.AccessToken))
                {
                    throw new Exception("No access token available.");
                }

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthState.AccessToken);
                var response = await httpClient.GetAsync(meUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to get current user: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var userInfo = JsonConvert.DeserializeObject<UserInfo>(responseContent);

                if (userInfo == null)
                {
                    throw new Exception("Failed to deserialize user info");
                }

                CurrentUser = userInfo;
                return userInfo;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting current user: {ex.Message}");
            }
        }

        public async Task<bool> ValidateTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AuthState.AccessToken))
                {
                    return false;
                }

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthState.AccessToken);
                var response = await httpClient.GetAsync(meUrl);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
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

        public async Task LogoutAsync()
        {
            try
            {
                ClearStoredTokens();
                httpClient.DefaultRequestHeaders.Authorization = null;
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new Exception($"Logout error: {ex.Message}");
            }
        }
    }
}
