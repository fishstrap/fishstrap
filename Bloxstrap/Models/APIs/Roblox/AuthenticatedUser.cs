using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bloxstrap.Models.APIs.Roblox
{
    public class AuthenticatedUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("name")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("displayname")]
        public string Displayname { get; set; } = string.Empty;
    }
}
