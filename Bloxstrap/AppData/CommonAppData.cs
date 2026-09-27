using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bloxstrap.AppData
{
    public abstract class CommonAppData
    {
        public virtual string ExecutableName { get; } = null!;

        public virtual string BinaryType { get; } = null!;

        public virtual string AppDataDirectory { get; } = Path.Combine(Paths.LocalAppData, "Roblox");

        public string StaticDirectory => Path.Combine(Paths.Versions, BinaryType);
        public string DynamicDirectory => Path.Combine(Paths.Versions, State.VersionGuid);

        public string Directory => App.Settings.Prop.StaticDirectory ? StaticDirectory : DynamicDirectory;

        public string ExecutablePath => Path.Combine(Directory, ExecutableName);

        public virtual string CdnExtension { get; } = string.Empty;

        public virtual bool SupportsCustomDeployments { get; } = true;

        public virtual AppState State { get; } = null!;
    }
}
