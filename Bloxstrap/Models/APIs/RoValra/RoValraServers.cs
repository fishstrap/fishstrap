using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bloxstrap.Models.APIs.RoValra
{
    public class RoValraServers
    {
        [JsonPropertyName("servers")]
        public List<RoValraServer> Servers { get; set; } = new();
    }
}
