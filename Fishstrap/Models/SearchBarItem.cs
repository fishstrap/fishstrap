using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishstrap.Models
{
    public class SearchBarItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public Type PageType { get; set; } = null!;

        public override string ToString() => DisplayName;
    }
}
