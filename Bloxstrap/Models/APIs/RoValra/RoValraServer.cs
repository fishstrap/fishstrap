using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bloxstrap.Models.APIs.RoValra
{
    public class RoValraServer
    {
        [JsonPropertyName("server_id")]
        public string ServerId { get; set; } = string.Empty;
    }
}
