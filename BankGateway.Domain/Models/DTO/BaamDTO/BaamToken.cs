using System;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    public class BaamToken
    {
        [JsonProperty(PropertyName = "last_logins")]
        public string LastLogins { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        public DateTime ExpiresAt { get; set; }

        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        public bool IsValidAndNotExpiring => !string.IsNullOrEmpty(AccessToken) && ExpiresAt > DateTime.UtcNow.AddSeconds(30);
    }
}
