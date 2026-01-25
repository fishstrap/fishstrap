using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bloxstrap.Models.APIs.Roblox
{
    public class UserChannel
    {
        [JsonPropertyName("channelName")]
        public string Channel { get; set; } = "production";

        [JsonPropertyName("channelAssignmentType")]
        public int? AssignmentType { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
