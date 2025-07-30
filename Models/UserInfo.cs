using Newtonsoft.Json;

namespace TeraCyteViewer.Models
{
    public class UserInfo
    {
        [JsonProperty("user_id")]
        public string? UserId { get; set; }

        [JsonProperty("username")]
        public string? Username { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("role")]
        public string? Role { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("last_login")]
        public DateTime? LastLogin { get; set; }
    }
} 