using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;
using SharpDX.DXGI;

using Bloxstrap.Enums.FlagPresets;
using System.Windows;
using Bloxstrap.UI.Elements.Settings.Pages;
using Wpf.Ui.Mvvm.Contracts;
using System.Windows.Documents;
using System.Runtime.InteropServices;

using System;

public static class SystemInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    public static int GetLogicalProcessorCount()
    {
        GetSystemInfo(out SYSTEM_INFO sysInfo);
        return (int)sysInfo.dwNumberOfProcessors;
    }

    public enum LOGICAL_PROCESSOR_RELATIONSHIP : uint
    {
        ProcessorCore = 0,
        NumaNode = 1,
        Cache = 2,
        ProcessorPackage = 3,
        Group = 4,
        All = 0xffff
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
    {
        public UIntPtr ProcessorMask;
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public ProcessorInfoUnion ProcessorInformation;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ProcessorInfoUnion
    {
        [FieldOffset(0)]
        public ProcessorCore ProcessorCore;

        [FieldOffset(0)]
        public NumaNode NumaNode;

        [FieldOffset(0)]
        public CacheDescriptor Cache;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessorCore
    {
        public byte Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NumaNode
    {
        public uint NodeNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CacheDescriptor
    {
        public byte Level;
        public byte Associativity;
        public ushort LineSize;
        public uint Size;
        public uint Type;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformation(IntPtr Buffer, ref uint ReturnLength);

    public static int GetPhysicalCoreCount()
    {
        uint returnLength = 0;
        GetLogicalProcessorInformation(IntPtr.Zero, ref returnLength);

        IntPtr ptr = Marshal.AllocHGlobal((int)returnLength);
        try
        {
            if (!GetLogicalProcessorInformation(ptr, ref returnLength))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            int size = Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
            int count = (int)returnLength / size;

            int coreCount = 0;
            for (int i = 0; i < count; i++)
            {
                IntPtr current = IntPtr.Add(ptr, i * size);
                var info = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>(current);

                if (info.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.ProcessorCore)
                {
                    coreCount++;
                }
            }

            return coreCount;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}


namespace Bloxstrap.UI.ViewModels.Settings
{
    public class FastFlagsViewModel : NotifyPropertyChangedViewModel
    {
        private Dictionary<string, object>? _preResetFlags;

        public event EventHandler? RequestPageReloadEvent;
        
        public event EventHandler? OpenFlagEditorEvent;

        private void OpenFastFlagEditor() => OpenFlagEditorEvent?.Invoke(this, EventArgs.Empty);

        public ICommand OpenFastFlagEditorCommand => new RelayCommand(OpenFastFlagEditor);

        public bool DisableTelemetry
        {
            get => App.FastFlags?.GetPreset("Telemetry.Urlv2") == "0.0.0.0";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Urlv2", value ? "0.0.0.0" : null);
                App.FastFlags.SetPreset("Telemetry.Protocol", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Service", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Enabled2", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.RobloxTelemetry", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.MemoryTracking", value ? "True" : null);
                App.FastFlags.SetPreset("Telemetry.Reliability", value ? "null" : null);
                App.FastFlags.SetPreset("Telemetry.Physic", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Tickrate", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Telemetry.Schedule", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Telemetry.AltHttp", value ? "null" : null);
                App.FastFlags.SetPreset("Telemetry.Http", value ? "null" : null);
            }
        }

        public bool DisableVoiceChatTelemetry
        {
            get => App.FastFlags?.GetPreset("Telemetry.Voicechat3") == "False";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Voicechat3", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat4", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat5", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat6", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat7", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat8", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat9", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat10", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat11", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat15", value ? "True" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat16", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat17", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat18", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat19", value ? "0" : null);
            }
        }

        public bool DisableWebview2Telemetry
        {
            get => App.FastFlags?.GetPreset("Telemetry.Webview1") == "False";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Webview1", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Webview2", value ? "0" : null);
            }
        }

        public bool BlockTencent
        {
            get => App.FastFlags?.GetPreset("Telemetry.Tencent1") == "null";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Tencent1", value ? "null" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent2", value ? "null" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent4", value ? "null" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent5", value ? "True" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent7", value ? "10000" : null);

            }
        }

        public bool BlockVng
        {
            get => App.FastFlags.GetPreset("Telemetry.VNG1") == "False";
            set
            {
                App.FastFlags.SetPreset("Telemetry.VNG1", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.VNG2", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.VNG3", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.VNG4", value ? "null" : null);
            }
        }

        public bool NetworkStream
        {
            get => App.FastFlags?.GetPreset("Network.Stream1") == "0";
            set
            {
                App.FastFlags.SetPreset("Network.Stream1", value ? "0" : null);
                App.FastFlags.SetPreset("Network.Stream2", value ? "0" : null);
                App.FastFlags.SetPreset("Network.Stream3", value ? "0" : null);
                App.FastFlags.SetPreset("Network.Stream4", value ? "0" : null);
                App.FastFlags.SetPreset("Network.Stream5", value ? "0" : null);
                App.FastFlags.SetPreset("Network.Stream6", value ? "0" : null);
                App.FastFlags.SetPreset("Network.Stream7", value ? "0" : null);
            }
        }

        public bool RemoveGrass
        {
            get => App.FastFlags?.GetPreset("Rendering.RemoveGrass1") == "0";
            set
            {
                App.FastFlags.SetPreset("Rendering.RemoveGrass1", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.RemoveGrass2", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.RemoveGrass3", value ? "0" : null);
            }
        }

        public bool LightCulling
        {
            get => App.FastFlags.GetPreset("System.GpuCulling") == "True";
            set
            {
                App.FastFlags.SetPreset("System.GpuCulling", value ? "True" : null);
                App.FastFlags.SetPreset("System.CpuCulling", value ? "True" : null);
            }
        }

        public bool WhiteSky
        {
            get => App.FastFlags.GetPreset("Graphic.RGBEEEncoding") == "True";
            set
            {
                App.FastFlags.SetPreset("Graphic.RGBEEEncoding", value ? "True" : null);
                App.FastFlags.SetPreset("Graphic.GraySky", value ? "True" : null);
            }
        }

        public bool BlackSky
        {
            get => App.FastFlags.GetPreset("Graphic.VertexSmoothing") == "10000";
            set
            {
                App.FastFlags.SetPreset("Graphic.VertexSmoothing", value ? "10000" : null);
                App.FastFlags.SetPreset("Rendering.TextureSkipping.Skips", value ? "8" : null);
                App.FastFlags.SetPreset("Rendering.TextureQuality.Level", value ? "0" : null);
            }
        }

        public bool RainbowSky
        {
            get => App.FastFlags.GetPreset("Rendering.Mode.Vulkan") == "True";
            set
            {
                App.FastFlags.SetPreset("Rendering.TextureSkipping.Skips", value ? "8" : null);
                App.FastFlags.SetPreset("Graphic.VertexSmoothing", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.MSAA2", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.MSAA1", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.TextureQuality.Level", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.TextureQuality.OverrideEnabled", value ? "True" : null);
                App.FastFlags.SetPreset("Rendering.Mode.Vulkan", value ? "True" : null);
            }
        }

        public bool GrayAvatars
        {
            get => App.FastFlags.GetPreset("Rendering.GrayAvatars") == "0";
            set => App.FastFlags.SetPreset("Rendering.GrayAvatars", value ? "0" : null);
        }

        public bool ShowChunks
        {
            get => App.FastFlags.GetPreset("Debug.Chunks") == "True";
            set => App.FastFlags.SetPreset("Debug.Chunks", value ? "True" : null);
        }

        public bool MemoryProbing
        {
            get => App.FastFlags.GetPreset("Memory.Probe") == "True";
            set => App.FastFlags.SetPreset("Memory.Probe", value ? "True" : null);
        }

        public bool NewFpsSystem
        {
            get => App.FastFlags.GetPreset("Rendering.NewFpsSystem") == "True";
            set => App.FastFlags.SetPreset("Rendering.NewFpsSystem", value ? "True" : null);
        }

        public bool FasterLoading
        {
            get => App.FastFlags.GetPreset("Network.MaxAssetPreload") == "2147483647";
            set
            {
                App.FastFlags.SetPreset("Network.MaxAssetPreload", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.PlayerImageDefault", value ? "1" : null);
                App.FastFlags.SetPreset("Network.MeshPreloadding", value ? "True" : null);
            }
        }

        public bool LowPolyMeshes
        {
            get => App.FastFlags.GetPreset("Rendering.LowPolyMeshes1") == "0";
            set
            {
                App.FastFlags.SetPreset("Rendering.LowPolyMeshes1", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.LowPolyMeshes2", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.LowPolyMeshes3", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.LowPolyMeshes4", value ? "0" : null);
            }
        }

        public bool ReduceLagSpikes
        {
            get => App.FastFlags.GetPreset("Network.DefaultBps") == "1024000";
            set
            {
                App.FastFlags.SetPreset("Network.DefaultBps", value ? "1024000" : null);
                App.FastFlags.SetPreset("Network.MaxWorkCatchupMs", value ? "8" : null);
            }
        }

        public bool RobloxCore
        {
            get => App.FastFlags.GetPreset("Network.RCore1") == "3000";
            set
            {
                App.FastFlags.SetPreset("Network.RCore1", value ? "3000" : null);
                App.FastFlags.SetPreset("Network.RCore2", value ? "10" : null);
                App.FastFlags.SetPreset("Network.RCore3", value ? "5000" : null);
                App.FastFlags.SetPreset("Network.RCore4", value ? "250" : null);
                App.FastFlags.SetPreset("Network.RCore5", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.RCore6", value ? "1100" : null);
                App.FastFlags.SetPreset("Network.RCore7", value ? "50" : null);
                App.FastFlags.SetPreset("Network.RCore8", value ? "100" : null);
                App.FastFlags.SetPreset("Network.RCore9", value ? "1000" : null);
            }
        }

        public bool NoPayloadLimit
        {
            get => App.FastFlags.GetPreset("Network.Payload1") == "2147483647";
            set
            {
                App.FastFlags.SetPreset("Network.Payload1", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload2", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload3", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload4", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload5", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload6", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload7", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload8", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload9", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload10", value ? "2147483647" : null);
                App.FastFlags.SetPreset("Network.Payload11", value ? "2147483647" : null);
            }
        }

        public bool IncreaseCache
        {
            get => App.FastFlags.GetPreset("Cache.Increase1") == "134217728 ";
            set
            {
                App.FastFlags.SetPreset("Cache.Increase1", value ? "134217728 " : null);
                App.FastFlags.SetPreset("Cache.Increase2", value ? "2147483647 " : null);
            }
        }

        public bool LargeReplicator
        {
            get => App.FastFlags.GetPreset("Network.EnableLargeReplicator") == "True";
            set
            {
                App.FastFlags.SetPreset("Network.EnableLargeReplicator", value ? "True" : null);
                App.FastFlags.SetPreset("Network.LargeReplicatorWrite", value ? "True" : null);
                App.FastFlags.SetPreset("Network.LargeReplicatorRead", value ? "True" : null);
                App.FastFlags.SetPreset("Network.EngineModule3", value ? "False" : null);
                App.FastFlags.SetPreset("Network.SerializeRead", value ? "True" : null);
                App.FastFlags.SetPreset("Network.SerializeWrite", value ? "True" : null);
            }
        }

        public bool DisableAds
        {
            get => App.FastFlags.GetPreset("UI.DisableAds1") == "False";
            set
            {
                App.FastFlags.SetPreset("UI.DisableAds1", value ? "False" : null);
                App.FastFlags.SetPreset("UI.DisableAds2", value ? "False" : null);
                App.FastFlags.SetPreset("UI.DisableAds3", value ? "0" : null);
            }
        }

        public bool DraggableCapture
        {
            get => App.FastFlags.GetPreset("UI.DraggableCapture") == "True";
            set => App.FastFlags.SetPreset("UI.DraggableCapture", value ? "True" : null);
        }

        public bool KTX
        {
            get => App.FastFlags.GetPreset("Texture.KTX") == "True";
            set => App.FastFlags.SetPreset("Texture.KTX", value ? "True" : null);
        }

        public bool Prerender
        {
            get => App.FastFlags.GetPreset("Rendering.Prerender") == "True";
            set => App.FastFlags.SetPreset("Rendering.Prerender", value ? "True" : null);
        }

        public bool GraySky
        {
            get => App.FastFlags.GetPreset("Graphic.GraySky") == "True";
            set => App.FastFlags.SetPreset("Graphic.GraySky", value ? "True" : null);
        }

        public bool PingBreakdown
        {
            get => App.FastFlags.GetPreset("Debug.PingBreakdown") == "True";
            set => App.FastFlags.SetPreset("Debug.PingBreakdown", value ? "True" : null);
        }

        public bool Pseudolocalization
        {
            get => App.FastFlags.GetPreset("UI.Pseudolocalization") == "True";
            set => App.FastFlags.SetPreset("UI.Pseudolocalization", value ? "True" : null);
        }

        public bool UseFastFlagManager
        {
            get => App.Settings.Prop.UseFastFlagManager;
            set => App.Settings.Prop.UseFastFlagManager = value;
        }

        public int FramerateLimit
        {
            get => int.TryParse(App.FastFlags.GetPreset("Rendering.Framerate"), out int x) ? x : 0;
            set
            {
                App.FastFlags.SetPreset("Rendering.Framerate", value == 0 ? null : value);
                if (value > 240)
                {
                    App.FastFlags.SetPreset("Rendering.LimitFramerate", "False");
                }
                else if (value <= 240)
                {
                    App.FastFlags.SetPreset("Rendering.LimitFramerate", null);
                }
            }
        }

        public int BufferArrayLength
        {
            get => int.TryParse(App.FastFlags.GetPreset("Recommended.Buffer"), out int x) ? x : 0;
            set => App.FastFlags.SetPreset("Recommended.Buffer", value == 0 ? null : value);
        }

        public int HideGUI
        {
            get => int.TryParse(App.FastFlags.GetPreset("UI.Hide"), out int x) ? x : 0;
            set
            {
                App.FastFlags.SetPreset("UI.Hide", value > 0 ? value.ToString() : null);
                App.FastFlags.SetPreset("UI.Hide.Toggles", value > 0 ? "True" : null);
            }
        }

        public IReadOnlyDictionary<MSAAMode, string?> MSAALevels => FastFlagManager.MSAAModes;

        public MSAAMode SelectedMSAALevel
        {
            get => MSAALevels.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.MSAA1")).Key;
            set
            {
                App.FastFlags.SetPreset("Rendering.MSAA1", MSAALevels[value]);
                App.FastFlags.SetPreset("Rendering.MSAA2", MSAALevels[value]);

            }
        }

        public IReadOnlyDictionary<TextureQuality, string?> TextureQualities => FastFlagManager.TextureQualityLevels;

        public TextureQuality SelectedTextureQuality
        {
            get => TextureQualities.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.TextureQuality.Level")).Key;
            set
            {
                if (value == TextureQuality.Default)
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality.OverrideEnabled", "True");
                    App.FastFlags.SetPreset("Rendering.TextureQuality.Level", TextureQualities[value]);
                }
            }
        }

        public IReadOnlyDictionary<RenderingMode, string> RenderingModes => FastFlagManager.RenderingModes;

        public RenderingMode SelectedRenderingMode
        {
            get => App.FastFlags.GetPresetEnum(RenderingModes, "Rendering.Mode", "True");
            set
            {
                if (value == RenderingMode.D3D10)
                {
                    App.FastFlags.SetPreset("Rendering.Mode.D3D10", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D10Compute", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D10GlobalInstancing", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", "True");
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Mode.D3D10", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D10Compute", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D10GlobalInstancing", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", null);
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", null);
                }

                if (value == RenderingMode.D3D11)
                {
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11ExtraLog", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.DisableHQShadersLowEndDx11", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.Dx11ShaderAnalytics", "0");
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", "False");
                    try
                    {
                        int coreCount = SystemInfo.GetPhysicalCoreCount();
                        App.FastFlags.SetPreset("Rendering.Mode.Dx11LowEndCoreCount", coreCount.ToString());
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine("SelectedRenderingMode", $"Failed to get CPU core count: {ex.Message}");
                        App.FastFlags.SetPreset("Rendering.Mode.Dx11LowEndCoreCount", "4");
                    }
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11ExtraLog", null);
                    App.FastFlags.SetPreset("Rendering.Mode.DisableHQShadersLowEndDx11", null);
                    App.FastFlags.SetPreset("Rendering.Mode.Dx11ShaderAnalytics", null);
                    App.FastFlags.SetPreset("Rendering.Mode.Dx11LowEndCoreCount", null);
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", null);
                }

                if (value == RenderingMode.Vulkan)
                {
                    App.FastFlags.SetPreset("Rendering.Mode.Vulkan", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.DisableVulkan1", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanDisablePreRotate", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanBonuxMemory", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanGlobalInstancing", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanAnalytics", "0");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanLogLayers", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanHeadless", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanARMVaryingBufferMb", "1024");
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanVaryingBufferLimit", "0x13B5:.+:.+=1024;0x5143:.+:.+=1024");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", "False");
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Mode.Vulkan", null);
                    App.FastFlags.SetPreset("Rendering.Mode.DisableVulkan1", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanDisablePreRotate", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanBonuxMemory", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanGlobalInstancing", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanAnalytics", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanLogLayers", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanHeadless", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanARMVaryingBufferMb", null);
                    App.FastFlags.SetPreset("Rendering.Mode.VulkanVaryingBufferLimit", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", null);
                }

                if (value == RenderingMode.OpenGL)
                {
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL.HQShadersExclusion", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL.SuperHQShadersExclusion", "True");
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", null);
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL", null);
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL.HQShadersExclusion", null);
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL.SuperHQShadersExclusion", null);
                }

                if (value == RenderingMode.Metal)
                {
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.DisableVulkan1", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.DisableVulkan2", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.Vulkan", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL", "False");
                    App.FastFlags.SetPreset("Rendering.Mode.Metal", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.MetalAnalytics", "0");
                    App.FastFlags.SetPreset("Rendering.Mode.MetalShaderCookie1", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.MetalShaderCookie2", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.MetalGlobalInstancing", "True");
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", "False");
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", null);
                    App.FastFlags.SetPreset("Rendering.Mode.DisableVulkan1", null);
                    App.FastFlags.SetPreset("Rendering.Mode.DisableVulkan2", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11", null);
                    App.FastFlags.SetPreset("Rendering.Mode.Vulkan", null);
                    App.FastFlags.SetPreset("Rendering.Mode.OpenGL", null);
                    App.FastFlags.SetPreset("Rendering.Mode.Metal", null);
                    App.FastFlags.SetPreset("Rendering.Mode.MetalAnalytics", null);
                    App.FastFlags.SetPreset("Rendering.Mode.MetalShaderCookie1", null);
                    App.FastFlags.SetPreset("Rendering.Mode.MetalShaderCookie2", null);
                    App.FastFlags.SetPreset("Rendering.Mode.MetalGlobalInstancing", null);
                    App.FastFlags.SetPreset("Rendering.Mode.D3D11GlobalInstancing", null);
                }
            }
        }

        public bool FixDisplayScaling
        {
            get => App.FastFlags.GetPreset("Rendering.DisableScaling") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisableScaling", value ? "True" : null);
        }

        public string? FlagState
        {
            get => App.FastFlags.GetPreset("Debug.FlagState");
            set => App.FastFlags.SetPreset("Debug.FlagState", value);
        }

        public IReadOnlyDictionary<LightingMode, string> LightingModes => FastFlagManager.LightingModes;

        public LightingMode SelectedLightingMode
        {
            get => App.FastFlags.GetPresetEnum(LightingModes, "Rendering.Lighting", "True");
            set => App.FastFlags.SetPresetEnum("Rendering.Lighting", LightingModes[value], "True");
        }

        public bool FullscreenTitlebarDisabled
        {
            get => int.TryParse(App.FastFlags.GetPreset("UI.FullscreenTitlebarDelay"), out int x) && x > 5000;
            set => App.FastFlags.SetPreset("UI.FullscreenTitlebarDelay", value ? "3600000" : null);
        }

        public IReadOnlyDictionary<TextureSkipping, string?> TextureSkippings => FastFlagManager.TextureSkippingSkips;

        public TextureSkipping SelectedTextureSkipping
        {
            get => TextureSkippings.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.TextureSkipping.Skips")).Key;
            set
            {
                if (value == TextureSkipping.Noskip)
                {
                    App.FastFlags.SetPreset("Rendering.TextureSkipping", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.TextureSkipping.Skips", TextureSkippings[value]);
                }
            }
        }

        public IReadOnlyDictionary<RomarkStart, string?> RomarkStartMappings => FastFlagManager.RomarkStartMappings;

        public RomarkStart SelectedRomarkStart
        {
            get => FastFlagManager.RomarkStartMappings.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.Start.Graphic")).Key;
            set
            {
                if (value == RomarkStart.Disabled)
                {
                    App.FastFlags.SetPreset("Rendering.Start.Graphic", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Start.Graphic", FastFlagManager.RomarkStartMappings[value]);
                }
            }
        }

        public IReadOnlyDictionary<QualityLevel, string?> QualityLevels => FastFlagManager.QualityLevels;

        public QualityLevel SelectedQualityLevel
        {
            get => FastFlagManager.QualityLevels.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.FrmQuality")).Key;
            set
            {
                if (value == QualityLevel.Disabled)
                {
                    App.FastFlags.SetPreset("Rendering.FrmQuality", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.FrmQuality", FastFlagManager.QualityLevels[value]);
                }
            }
        }

        public bool DisablePostFX
        {
            get => App.FastFlags.GetPreset("Rendering.DisablePostFX") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisablePostFX", value ? "True" : null);
        }

        public bool MinimalRendering
        {
            get => App.FastFlags.GetPreset("Rendering.MinimalRendering") == "True";
            set => App.FastFlags.SetPreset("Rendering.MinimalRendering", value ? "True" : null);
        }

        public bool DisableSky
        {
            get => App.FastFlags.GetPreset("Rendering.NoFrmBloom") == "False";
            set => App.FastFlags.SetPreset("Rendering.NoFrmBloom", value ? "False" : null);
        }

        public bool UnthemedInstances
        {
            get => App.FastFlags.GetPreset("UI.UnthemedInstances") == "True";
            set => App.FastFlags.SetPreset("UI.UnthemedInstances", value ? "True" : null);
        }

        public bool RemoveBuyGui
        {
            get => App.FastFlags.GetPreset("UI.RemoveBuyGui") == "True";
            set => App.FastFlags.SetPreset("UI.RemoveBuyGui", value ? "True" : null);
        }

        public bool NoDisconnectMessage
        {
            get => App.FastFlags.GetPreset("UI.NoDisconnectMsg") == "2147483647";
            set => App.FastFlags.SetPreset("UI.NoDisconnectMsg", value ? "2147483647" : null);
        }

        public bool RedFont
        {
            get => App.FastFlags.GetPreset("UI.RedFont") == "rbxasset://fonts/families/BuilderSans.json";
            set => App.FastFlags.SetPreset("UI.RedFont", value ? "rbxasset://fonts/families/BuilderSans.json" : null);
        }

        public bool DisableLayeredClothing
        {
            get => App.FastFlags.GetPreset("UI.DisableLayeredClothing") == "-1";
            set => App.FastFlags.SetPreset("UI.DisableLayeredClothing", value ? "-1" : null);
        }

        public int? TextElongation
        {
            get => int.TryParse(App.FastFlags.GetPreset("UI.TextElongation"), out int x) ? x : 1;
            set => App.FastFlags.SetPreset("UI.TextElongation", value == 1 ? null : value);
        }

        public bool DisablePlayerShadows
        {
            get => App.FastFlags.GetPreset("Rendering.ShadowIntensity") == "0";
            set
            {
                App.FastFlags.SetPreset("Rendering.ShadowIntensity", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.Pause.Voxelizer", value ? "True" : null);
            }
        }

        public int? FontSize
        {
            get => int.TryParse(App.FastFlags.GetPreset("UI.FontSize"), out int x) ? x : 1;
            set => App.FastFlags.SetPreset("UI.FontSize", value == 1 ? null : value);
        }

        public bool DisableTerrainTextures
        {
            get => App.FastFlags.GetPreset("Rendering.TerrainTextureQuality") == "0";
            set => App.FastFlags.SetPreset("Rendering.TerrainTextureQuality", value ? "0" : null);
        }

        public int MtuSize
        {
            get => int.TryParse(App.FastFlags.GetPreset("Network.Mtusize"), out int x) ? x : 0;
            set
            {
                int clamped = Math.Max(0, Math.Min(1498, value));
                App.FastFlags.SetPreset(
                    "Network.Mtusize",
                    clamped >= 576 ? clamped.ToString() : null
                );
            }
        }

        public int PhysicSender
        {
            get
            {
                return int.TryParse(App.FastFlags.GetPreset("Network.Phyics1"), out var value) ? value : 0;
            }
            set
            {
                if (value < 60)
                {
                    App.FastFlags.SetPreset("Network.Phyics1", null);
                    App.FastFlags.SetPreset("Network.Phyics2", null);
                }
                else
                {
                    int clamped = Math.Min(38760, value);
                    string strValue = clamped.ToString();
                    App.FastFlags.SetPreset("Network.Phyics1", strValue);
                    App.FastFlags.SetPreset("Network.Phyics2", strValue);
                }
            }
        }

        public int FPSBufferPercentage
        {
            get => int.TryParse(App.FastFlags.GetPreset("Rendering.FrameRateBufferPercentage"), out int x) ? x : 0;
            set
            {
                int clamped = Math.Max(0, Math.Min(100, value));
                App.FastFlags.SetPreset(
                    "Rendering.FrameRateBufferPercentage",
                    clamped >= 1 ? clamped.ToString() : null
                );
            }
        }

        public IReadOnlyDictionary<string, string?>? GPUs => GetGPUs();

        public string SelectedGPU
        {
            get => App.FastFlags.GetPreset("System.PreferredGPU") ?? "Automatic";
            set
            {
                App.FastFlags.SetPreset("System.PreferredGPU", value == "Automatic" ? null : value);
                App.FastFlags.SetPreset("System.DXT", value == "Automatic" ? null : value);

            }
        }

        public string BypassVulkan
        {
            get => App.FastFlags.GetPreset("System.BypassVulkan") ?? "Automatic";
            set => App.FastFlags.SetPreset("System.BypassVulkan", value == "Automatic" ? null : value);
        }

        public bool GetFlagAsBool(string flagKey, string falseValue = "False")
        {
            return App.FastFlags.GetPreset(flagKey) != falseValue;
        }

        public void SetFlagFromBool(string flagKey, bool value, string falseValue = "False")
        {
            App.FastFlags.SetPreset(flagKey, value ? null : falseValue);
        }

        public bool VRToggle
        {
            get => GetFlagAsBool("Menu.VRToggles");
            set => SetFlagFromBool("Menu.VRToggles", value);
        }

        public bool SoothsayerCheck
        {
            get => GetFlagAsBool("Menu.Feedback");
            set => SetFlagFromBool("Menu.Feedback", value);
        }

        public bool LanguageSelector
        {
            get => App.FastFlags.GetPreset("Menu.LanguageSelector") != "0";
            set => SetFlagFromBool("Menu.LanguageSelector", value, "0");
        }

        public bool Framerate
        {
            get => GetFlagAsBool("Menu.Framerate");
            set => SetFlagFromBool("Menu.Framerate", value);
        }

        public bool ChatTranslation
        {
            get => GetFlagAsBool("Menu.ChatTranslation");
            set => SetFlagFromBool("Menu.ChatTranslation", value);
        }

        public IReadOnlyDictionary<DynamicResolution, string?> DynamicResolutions => FastFlagManager.DynamicResolutions;

        public IEnumerable<DynamicResolution> DynamicResolutionOptions
        {
            get
            {
                var currentValue = App.FastFlags.GetPreset("Rendering.Dynamic.Resolution");
                bool isKnown = DynamicResolutions.Values.Contains(currentValue);

                return isKnown
                    ? DynamicResolutions.Keys
                    : DynamicResolutions.Keys.Concat(new[] { DynamicResolution.CustomValue });
            }
        }

        public DynamicResolution SelectedDynamicResolution
        {
            get
            {
                string? currentValue = App.FastFlags.GetPreset("Rendering.Dynamic.Resolution");

                var match = DynamicResolutions.FirstOrDefault(x => x.Value == currentValue);

                if (match.Value == currentValue)
                    return match.Key;

                return DynamicResolution.CustomValue;
            }
            set
            {
                if (value == DynamicResolution.Default)
                {
                    App.FastFlags.SetPreset("Rendering.Dynamic.Resolution", null);
                }
                else if (value == DynamicResolution.CustomValue)
                {

                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Dynamic.Resolution", DynamicResolutions[value]);
                }
            }
        }

        public IReadOnlyDictionary<RefreshRate, string?> RefreshRates => FastFlagManager.RefreshRates;

        public IEnumerable<RefreshRate> RefreshRateOptions
        {
            get
            {
                string? currentValue = App.FastFlags.GetPreset("System.TargetRefreshRate1");
                bool isKnown = RefreshRates.Values.Contains(currentValue);

                return isKnown
                    ? RefreshRates.Keys
                    : RefreshRates.Keys.Concat(new[] { RefreshRate.CustomValue });
            }
        }

        public RefreshRate SelectedRefreshRate
        {
            get
            {
                string? currentValue = App.FastFlags.GetPreset("System.TargetRefreshRate1");
                var match = RefreshRates.FirstOrDefault(x => x.Value == currentValue);

                if (match.Value == currentValue)
                    return match.Key;

                return RefreshRate.CustomValue;
            }
            set
            {
                if (value == RefreshRate.Default)
                {
                    App.FastFlags.SetPreset("System.TargetRefreshRate1", null);
                }
                else if (value == RefreshRate.CustomValue)
                {
                    // Handle custom value if needed, or leave empty to not change preset
                }
                else
                {
                    var presetValue = RefreshRates[value];
                    App.FastFlags.SetPreset("System.TargetRefreshRate1", presetValue);
                }

                OnPropertyChanged(nameof(SelectedRefreshRate));
                OnPropertyChanged(nameof(RefreshRateOptions));
            }
        }


        public bool ResetConfiguration
        {
            get => _preResetFlags is not null;
            set
            {
                if (value)
                {
                    _preResetFlags = new(App.FastFlags.Prop);
                    App.FastFlags.Prop.Clear();
                }
                else
                {
                    App.FastFlags.Prop = _preResetFlags!;
                    _preResetFlags = null;
                }

                RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public static IReadOnlyDictionary<string, string?> GetGPUs()
        {
            const string LOG_IDENT = "FFlagPresets::GetGPUs";
            Dictionary<string, string?> GPUs = new();

            GPUs.Add("Automatic", null);

            try
            {
                using (var factory = new Factory1())
                {
                    for (int i = 0; i < factory.GetAdapterCount1(); i++)
                    {
                        var GPU = factory.GetAdapter1(i);
                        var Name = GPU.Description;
                        GPUs.Add(Name.Description, Name.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get GPU names: {ex.Message}");
            }

            return GPUs;
        }

        public static IReadOnlyDictionary<string, string?> GetCpuThreads()
        {
            const string LOG_IDENT = "FFlagPresets::GetCpuThreads";
            Dictionary<string, string?> cpuThreads = new();

            // Add the "Automatic" option
            cpuThreads.Add("Automatic", null);

            try
            {
                // Get the number of logical processors
                int logicalProcessorCount = SystemInfo.GetLogicalProcessorCount();

                // Add options for 1, 2, 3, ..., up to the number of logical processors
                for (int i = 1; i <= logicalProcessorCount; i++)
                {
                    cpuThreads.Add(i.ToString(), i.ToString());
                }
            }
            catch (Exception ex)
            {
                // Log the error if something goes wrong
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get CPU thread count: {ex.Message}");
            }

            return cpuThreads;
        }

        public IReadOnlyDictionary<string, string?>? CpuThreads => GetCpuThreads();
        public KeyValuePair<string, string?> SelectedCpuThreads
        {
            get
            {
                string currentValue = App.FastFlags.GetPreset("System.CpuCore1") ?? "Automatic";
                return CpuThreads?.FirstOrDefault(kvp => kvp.Key == currentValue) ?? default;
            }
            set
            {
                App.FastFlags.SetPreset("System.CpuCore1", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
                App.FastFlags.SetPreset("System.CpuCore2", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
                App.FastFlags.SetPreset("System.CpuCore3", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
                App.FastFlags.SetPreset("System.CpuCore4", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
                App.FastFlags.SetPreset("System.CpuCore5", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
                App.FastFlags.SetPreset("System.CpuCore6", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
                App.FastFlags.SetPreset("System.CpuCore7", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
                App.FastFlags.SetPreset("System.CpuCore9", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));

                if (value.Value != null && int.TryParse(value.Value, out int parsedValue))
                {
                    int maxValue = CpuThreads!
                        .Where(kvp => kvp.Key != "Automatic" && int.TryParse(kvp.Key, out _))
                        .Select(kvp => int.Parse(kvp.Key))
                        .DefaultIfEmpty(1)
                        .Max();

                    if (parsedValue == maxValue)
                    {
                        int adjustedValue = Math.Max(parsedValue - 1, 1);
                        App.FastFlags.SetPreset("System.CpuThreads", adjustedValue.ToString());
                        OnPropertyChanged(nameof(SelectedCpuThreads));
                        App.FastFlags.SetPreset("System.CpuCore8", adjustedValue.ToString());
                        OnPropertyChanged(nameof(SelectedCpuThreads));
                    }
                    else
                    {
                        App.FastFlags.SetPreset("System.CpuThreads", parsedValue.ToString());
                        OnPropertyChanged(nameof(SelectedCpuThreads));
                        App.FastFlags.SetPreset("System.CpuCore8", parsedValue.ToString());
                        OnPropertyChanged(nameof(SelectedCpuThreads));
                    }
                }
                else
                {
                    App.FastFlags.SetPreset("System.CpuThreads", null);
                    OnPropertyChanged(nameof(SelectedCpuThreads));
                    App.FastFlags.SetPreset("System.CpuCore8", null);
                    OnPropertyChanged(nameof(SelectedCpuThreads));
                }
            }
        }

        public static IReadOnlyDictionary<string, string?> GetCpuCoreMinThreadCount()
        {
            const string LOG_IDENT = "FFlagPresets::GetCpuCoreMinThreadCount";
            Dictionary<string, string?> cpuThreads = new();

            // Add the "Automatic" option
            cpuThreads.Add("Automatic", null);

            try
            {
                // Use physical core count or logical, whichever you want:
                int coreCount = SystemInfo.GetPhysicalCoreCount(); // or GetLogicalProcessorCount()

                // Add options for 1, 2, ..., coreCount
                for (int i = 1; i <= coreCount; i++)
                {
                    cpuThreads.Add(i.ToString(), i.ToString());
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get CPU thread count: {ex.Message}");
            }

            return cpuThreads;
        }

        public IReadOnlyDictionary<string, string?>? CpuCoreMinThreadCount => GetCpuCoreMinThreadCount();

        public KeyValuePair<string, string?> SelectedCpuCoreMinThreadCount
        {
            get
            {
                string currentValue = App.FastFlags.GetPreset("System.CpuCoreMinThreadCount") ?? "Automatic";
                return CpuThreads?.FirstOrDefault(kvp => kvp.Key == currentValue) ?? default;
            }
            set
            {
                // Save selected value as-is
                App.FastFlags.SetPreset("System.CpuCoreMinThreadCount", value.Value);
                OnPropertyChanged(nameof(SelectedCpuThreads));
            }
        }
    }
}