using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraCyteViewer.Services
{
    public class AuthState
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expiration;
        public bool IsRefreshTokenExpired => DateTime.UtcNow >= RefreshTokenExpiration;
    }
}
