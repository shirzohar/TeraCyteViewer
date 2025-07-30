using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public interface IAuthService
    {
        AuthState AuthState { get; }
        UserInfo? CurrentUser { get; }
        
        Task<LoginResponse> LoginAsync(string username, string password);
        Task<bool> RefreshTokenAsync();
        Task<UserInfo?> GetCurrentUserAsync();
        Task<bool> ValidateTokenAsync();
        Task LogoutAsync();
        
        string GetAccessToken();
        bool IsTokenExpired();
        bool IsRefreshTokenExpired();
        
        void ClearStoredTokens();
        void SaveHistory(List<HistoryItem> history);
        List<HistoryItem> LoadHistory();
        void ClearStoredHistory();
    }
} 