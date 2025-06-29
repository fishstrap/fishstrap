using Bloxstrap.Enums.FlagPresets;
using System.Security.Policy;
using System.Windows;

namespace Bloxstrap
{
    public class FastFlagManager : JsonManager<Dictionary<string, object>>
    {
        public override string ClassName => nameof(FastFlagManager);

        public override string LOG_IDENT_CLASS => ClassName;

        public override string ProfilesLocation => Path.Combine(Paths.Base, "Profiles");

        public override string FileLocation => Path.Combine(Paths.Modifications, "ClientSettings\\ClientAppSettings.json");

        public bool Changed => !OriginalProp.SequenceEqual(Prop);

        public static IReadOnlyDictionary<string, string> PresetFlags = new Dictionary<string, string>
        {
            // Activity Watcher
            { "Players.LogLevel", "FStringDebugLuaLogLevel" },
            { "Players.LogPattern", "FStringDebugLuaLogPattern" },
            { "Instances.WndCheck", "FLogWndProcessCheck" },

            // Preset Flags
            { "Rendering.ManualFullscreen", "FFlagHandleAltEnterFullscreenManually" },
            { "Flog.Network", "FLogNetwork" },

            // Recommended Buffering
            { "Recommended.Buffer", "FIntRakNetResendBufferArrayLength" },
     
            // Memory Probing
            { "Memory.Probe", "DFFlagPerformanceControlEnableMemoryProbing3" },

            // Skys
            { "Graphic.GraySky", "FFlagDebugSkyGray" },
            { "Graphic.RGBEEEncoding", "FFlagSkyUseRGBEEncoding" },
            { "Graphic.VertexSmoothing", "FIntVertexSmoothingGroupTolerance" },

            // Low Poly Meshes
            { "Rendering.LowPolyMeshes1", "DFIntCSGLevelOfDetailSwitchingDistance" },
            { "Rendering.LowPolyMeshes2", "DFIntCSGLevelOfDetailSwitchingDistanceL12" },
            { "Rendering.LowPolyMeshes3", "DFIntCSGLevelOfDetailSwitchingDistanceL23" },
            { "Rendering.LowPolyMeshes4", "DFIntCSGLevelOfDetailSwitchingDistanceL34" },

            // Frm Quality Level
            { "Rendering.FrmQuality", "DFIntDebugFRMQualityLevelOverride" },

            // Less lag spikes
            { "Network.DefaultBps", "DFIntBandwidthManagerApplicationDefaultBps" },
            { "Network.MaxWorkCatchupMs", "DFIntBandwidthManagerDataSenderMaxWorkCatchupMs" },

            // Load Faster
            { "Network.MeshPreloadding", "DFFlagEnableMeshPreloading2" },
            { "Network.MaxAssetPreload", "DFIntNumAssetsMaxToPreload" },
            { "Network.PlayerImageDefault", "FStringGetPlayerImageDefaultTimeout" },

            // Physic
            { "Network.Phyics1", "DFIntS2PhysicsSenderRate" },
            { "Network.Phyics2", "DFIntDataSenderRate" },

            // Payload Limit
            { "Network.Payload1", "DFIntRccMaxPayloadSnd" },
            { "Network.Payload2", "DFIntCliMaxPayloadRcv" },
            { "Network.Payload3", "DFIntCliMaxPayloadSnd" },
            { "Network.Payload4", "DFIntRccMaxPayloadRcv" },
            { "Network.Payload5", "DFIntCliTcMaxPayloadRcv" },
            { "Network.Payload6", "DFIntRccTcMaxPayloadRcv" },
            { "Network.Payload7", "DFIntCliTcMaxPayloadSnd" },
            { "Network.Payload8", "DFIntRccTcMaxPayloadSnd" },
            { "Network.Payload9", "DFIntMaxDataPayloadSize" },
            { "Network.Payload10", "DFIntMaxUREPayloadSingleLimit" },
            { "Network.Payload11", "DFIntTotalRepPayloadLimit" },

            // Disable Ads
            { "UI.DisableAds1", "FFlagAdServiceEnabled" },
            { "UI.DisableAds2", "FFlagEnableRewardedVideoInAdService15" },
            { "UI.DisableAds3", "FLogAdService" },


            // Pseudolocalization
            { "UI.Pseudolocalization", "FFlagDebugEnablePseudolocalization" },

            // Disable Ads
            { "Network.Stream1", "DFIntNetworkStreamInitSize" },
            { "Network.Stream2", "DFIntNetworkStreamMinGrowSize" },
            { "Network.Stream3", "DFIntNetworkStreamingGCMaxMicroSecondLimit" },
            { "Network.Stream4", "DFIntNetworkStreamingGCMaxMicroSecondLimitPartsModels" },
            { "Network.Stream5", "DFIntNetworkStreamingGCUrgentMaxMicroSecondLimit" },
            { "Network.Stream6", "DFIntNetworkStreamingGCUrgentMaxMicroSecondLimitPartsModels" },
            { "Network.Stream7", "DFIntSchemaNetworkStreamSize" },

            // Remove Grass
            { "Rendering.RemoveGrass1", "FIntFRMMinGrassDistance" },
            { "Rendering.RemoveGrass2", "FIntFRMMaxGrassDistance" },
            { "Rendering.RemoveGrass3", "FIntRenderGrassDetailStrands" },

            // Other FFlags
            { "Rendering.LimitFramerate", "FFlagTaskSchedulerLimitTargetFpsTo2402" },
            { "Rendering.Framerate", "DFIntTaskSchedulerTargetFps" },
            { "Rendering.DisableScaling", "DFFlagDisableDPIScale" },
            { "Rendering.MSAA1", "FIntDebugForceMSAASamples" },
            { "Rendering.MSAA2", "FIntDebugFRMOptionalMSAALevelOverride" },
            { "Rendering.DisablePostFX", "FFlagDisablePostFx" },

            // Debug
            { "Debug.FlagState", "FStringDebugShowFlagState" },
            { "Debug.PingBreakdown", "DFFlagDebugPrintDataPingBreakDown" },
            { "Debug.Chunks", "FFlagDebugLightGridShowChunks" },

            // Increase Cache Size
            { "Cache.Increase1", "DFFlagAlwaysSkipDiskCache" },
            { "Cache.Increase3", "DFIntCachedPatchLoadDelayMilliseconds" },
            { "Cache.Increase4", "DFIntHttpCacheCleanScheduleAfterMs" },
            { "Cache.Increase5", "DFIntHttpCacheCleanUpToAvailableSpaceMiB" },
            { "Cache.Increase6", "DFIntHttpCacheAsyncWriterMaxPendingSize" },
            { "Cache.Increase7", "DFIntHttpCacheEvictionExemptionMapMaxSize" },
            { "Cache.Increase8", "DFIntMemCacheMaxCapacityMB" },
            { "Cache.Increase9", "DFIntFileCacheReserveSize" },
            { "Cache.Increase10", "DFIntThirdPartyInMemoryCacheCapacity" },
            { "Cache.Increase12", "DFIntUserIdPlayerNameCacheLifetimeSeconds" },

            // Force Logical Processors
            { "System.CpuCore1", "DFIntInterpolationNumParallelTasks" },
            { "System.CpuCore2", "DFIntMegaReplicatorNumParallelTasks" },
            { "System.CpuCore3", "DFIntNetworkClusterPacketCacheNumParallelTasks" },
            { "System.CpuCore4", "DFIntReplicationDataCacheNumParallelTasks" },
            { "System.CpuCore5", "FIntLuaGcParallelMinMultiTasks" },
            { "System.CpuCore6", "FIntSmoothClusterTaskQueueMaxParallelTasks" },
            { "System.CpuCore7", "DFIntPhysicsReceiveNumParallelTasks" },
            { "System.CpuCore8", "FIntTaskSchedulerAutoThreadLimit" },
            { "System.CpuCore9", "FIntSimWorldTaskQueueParallelTasks" },
            { "System.CpuThreads", "DFIntRuntimeConcurrency"},

            // Telemetry
            { "Telemetry.GraphicsQualityUsage", "DFFlagGraphicsQualityUsageTelemetry" },
            { "Telemetry.GpuVsCpuBound", "DFFlagGpuVsCpuBoundTelemetry" },
            { "Telemetry.RenderDistance", "DFFlagReportRenderDistanceTelemetry" },
            { "Telemetry.AudioPlugin", "DFFlagCollectAudioPluginTelemetry" },
            { "Telemetry.SoundLength", "DFFlagRccLoadSoundLengthTelemetryEnabled" },
            { "Telemetry.AssetRequestV1", "DFFlagReportAssetRequestV1Telemetry" },
            { "Telemetry.DeviceRAM", "DFFlagRobloxTelemetryAddDeviceRAMPointsV2" },
            { "Telemetry.V2FrameRateMetrics", "DFFlagEnableTelemetryV2FRMStats" },
            { "Telemetry.GlobalSkipUpdating", "DFFlagEnableSkipUpdatingGlobalTelemetryInfo5" },
            { "Telemetry.CallbackSafety", "DFFlagEmitSafetyTelemetryInCallbackEnable" },
            { "Telemetry.Protocol", "FFlagEnableTelemetryProtocol" },
            { "Telemetry.TelemetryService", "FFlagEnableTelemetryService1" },
            { "Telemetry.PropertiesTelemetry", "FFlagPropertiesEnableTelemetry" },
            { "Telemetry.OpenTelemetry", "FFlagOpenTelemetryEnabled2" },
            { "Telemetry.FLogTelemetry", "FLogRobloxTelemetry" },
            { "Telemetry.Reliability", "DFStringRobloxTelemetryReliabilityCountAllowList" },
            { "Telemetry.MemoryTracking", "FFlagDisableMemoryTracking" },

            // Voicechat Telemetry
            { "Telemetry.Voicechat3", "DFFlagVoiceChatPossibleDuplicateSubscriptionsTelemetry" },
            { "Telemetry.Voicechat4", "DFIntVoiceChatTaskStatsTelemetryThrottleHundrethsPercent" },
            { "Telemetry.Voicechat5", "FFlagEnableLuaVoiceChatAnalyticsV2" },
            { "Telemetry.Voicechat6", "FFlagLuaVoiceChatAnalyticsBanMessage" },
            { "Telemetry.Voicechat7", "FFlagLuaVoiceChatAnalyticsUseCounterV2" },
            { "Telemetry.Voicechat8", "FFlagLuaVoiceChatAnalyticsUseEventsV2" },
            { "Telemetry.Voicechat9", "FFlagLuaVoiceChatAnalyticsUsePointsV2" },
            { "Telemetry.Voicechat10", "FFlagVoiceChatCullingEnableMutedSubsTelemetry" },
            { "Telemetry.Voicechat11", "FFlagVoiceChatCullingEnableStaleSubsTelemetry" },
            { "Telemetry.Voicechat15", "FFlagVoiceChatDontSendTelemetryForPubIceTrickle" },
            { "Telemetry.Voicechat16", "FFlagVoiceChatPeerConnectionTelemetryDetails" },
            { "Telemetry.Voicechat17", "FFlagVoiceChatRobloxAudioDeviceUpdateRecordedBufferTelemetryEnabled" },
            { "Telemetry.Voicechat18", "FFlagVoiceChatSubscriptionsDroppedTelemetry" },
            { "Telemetry.Voicechat19", "FIntLuaVoiceChatAnalyticsPointsThrottle" },

            // Webview2 telemetry
            { "Telemetry.Webview1", "DFFlagWindowsWebViewTelemetryEnabled" },
            { "Telemetry.Webview3", "DFIntMacWebViewTelemetryThrottleHundredthsPercent" },
            { "Telemetry.Webview4", "DFIntWindowsWebViewTelemetryThrottleHundredthsPercent" },
            { "Telemetry.Webview5", "FIntStudioWebView2TelemetryHundredthsPercent" },
            { "Telemetry.Webview6", "FFlagSyncWebViewCookieToEngine2" },
            { "Telemetry.Webview7", "FFlagUpdateHTTPCookieStorageFromWKWebView" },

            // Block Tencent
            { "Telemetry.Tencent1", "FStringTencentAuthPath" },
            { "Telemetry.Tencent2", "FLogTencentAuthPath" },
            { "Telemetry.Tencent4", "FStringExperienceGuidelinesExplainedPageUrl" },
            { "Telemetry.Tencent5", "DFFlagPolicyServiceReportIsNotSubjectToChinaPolicies" },
            { "Telemetry.Tencent7", "DFIntPolicyServiceReportDetailIsNotSubjectToChinaPoliciesHundredthsPercentage" },

            // Block VNG (Vietnamese goverment)
            { "Telemetry.VNG1", "FFlagAllowVngWebshopDomainInBrowserService" },
            { "Telemetry.VNG2", "FFlagEnableHidePremiumForVngUsers" },
            { "Telemetry.VNG3", "FFlagEnablePreSignedVngShopRedirectUrl" },
            { "Telemetry.VNG4", "FFlagEnableVNGNewAppAvailableModal" },
            { "Telemetry.VNG5", "FFlagLuaAppHomeVngAppUpsell" },
            { "Telemetry.VNG6", "FFlagVngLogoutGlobalAppSessionsOnConversion" },
            { "Telemetry.VNG7", "FFlagVngTOSRevisedEnabled" },
            { "Telemetry.VNG8", "FStringVngAppUpsellUrl" },
            { "Telemetry.VNG9", "FStringVNGWebshopUrl" },

            
            // Minimal Rendering
            { "Rendering.MinimalRendering", "FFlagDebugRenderingSetDeterministic"},

            // Remove Sky/Clouds
            { "Rendering.NoFrmBloom", "FFlagRenderNoLowFrmBloom"},

            // Unthemed Instances
            { "UI.UnthemedInstances", "FFlagDebugDisplayUnthemedInstances" },

            // Disable Layered Clothing
            { "UI.DisableLayeredClothing", "DFIntLCCageDeformLimit" },

            // Remove Buy Gui
            { "UI.RemoveBuyGui", "DFFlagOrder66" },

            // More characters in text
            { "UI.TextElongation", "FIntDebugTextElongationFactor" },

            // No Disconnect Message
            { "UI.NoDisconnectMsg", "DFIntDefaultTimeoutTimeMs" },

            // Gray Avatars
            { "Rendering.GrayAvatars", "DFIntTextureCompositorActiveJobs" },

            // Cpu cores
            { "System.CpuCoreMinThreadCount", "FIntTaskSchedulerAsyncTasksMinimumThreadCount"},

            // New Fps System
            { "Rendering.NewFpsSystem", "FFlagEnableFPSAndFrameTime"},
            { "Rendering.FrameRateBufferPercentage", "FIntMaquettesFrameRateBufferPercentage"},

            // Light Cullings
            { "System.GpuCulling", "FFlagFastGPULightCulling3" },
            { "System.CpuCulling", "FFlagDebugForceFSMCPULightCulling" },
            
            // Prerender (Prerenderv2 is enabled by default so there is no need to add it here)
            { "Rendering.Prerender", "FFlagMovePrerender" },

            // Unlimited Camera Distance
            { "Rendering.Camerazoom","FIntCameraMaxZoomDistance" },

            // Rendering engines
            { "Rendering.Mode.DisableD3D11", "FFlagDebugGraphicsDisableDirect3D11" },
            { "Rendering.Mode.D3D11", "FFlagDebugGraphicsPreferD3D11" },
            { "Rendering.Mode.Metal", "FFlagDebugGraphicsPreferMetal" },
            { "Rendering.Mode.Vulkan", "FFlagDebugGraphicsPreferVulkan" },
            { "Rendering.Mode.OpenGL", "FFlagDebugGraphicsPreferOpenGL" },
            { "Rendering.Mode.D3D10", "FFlagDebugGraphicsPreferD3D11FL10" },

            // Better DX10
            { "Rendering.Mode.D3D10Compute", "FFlagGraphicsEnableD3D10Compute"},
            { "Rendering.Mode.D3D10GlobalInstancing", "FFlagRenderEnableGlobalInstancingD3D10"},
            { "Rendering.Mode.D3D11GlobalInstancing", "FFlagRenderEnableGlobalInstancingD3D11"},

            // Better Vulkan
            { "Rendering.Mode.DisableVulkan1", "FFlagDebugGraphicsDisableVulkan"},
            { "Rendering.Mode.DisableVulkan2", "FFlagDebugGraphicsDisableVulkan11"},
            { "Rendering.Mode.VulkanDisablePreRotate", "FFlagDebugVulkanDisablePreRotate"},
            { "Rendering.Mode.VulkanBonuxMemory", "FFlagGraphicsVulkanBonusMemory"},
            { "Rendering.Mode.VulkanGlobalInstancing", "FFlagRenderEnableGlobalInstancingVulkan"},

            // Better Metal
            { "Rendering.Mode.MetalAnalytics", "FIntGraphicsMetalAnalyticsHundredthPercent"},
            { "Rendering.Mode.MetalShaderCookie1", "FFlagGraphicsMetalShaderCookie"},
            { "Rendering.Mode.MetalShaderCookie2", "FFlagGraphicsMetalShaderCookie16"},
            { "Rendering.Mode.MetalGlobalInstancing", "FFlagRenderEnableGlobalInstancingMetal"},

            // Task Scheduler Avoid sleep
            { "Rendering.AvoidSleep", "DFFlagTaskSchedulerAvoidSleep" },

            // Lighting technology
            { "Rendering.Lighting.Voxel", "DFFlagDebugRenderForceTechnologyVoxel" },
            { "Rendering.Lighting.ShadowMap", "FFlagDebugForceFutureIsBrightPhase2" },
            { "Rendering.Lighting.Future", "FFlagDebugForceFutureIsBrightPhase3" },
            { "Rendering.Lighting.Unified", "FFlagRenderUnifiedLighting13"},

            // Texture quality
            { "Rendering.TerrainTextureQuality", "FIntTerrainArraySliceSize" },
            { "Rendering.TextureSkipping.Skips", "FIntDebugTextureManagerSkipMips" },
            { "Rendering.TextureQuality.Level", "DFIntTextureQualityOverride" },
            { "Rendering.TextureQuality.OverrideEnabled", "DFFlagTextureQualityOverrideEnabled" },

            // Guis
            { "UI.Hide", "DFIntCanHideGuiGroupId" },
            { "UI.Hide.Toggles", "FFlagUserShowGuiHideToggles" },

            // Fonts
            { "UI.FontSize", "FIntFontSizePadding" },
            { "UI.RedFont", "FStringDebugHighlightSpecificFont" },

            // RCore
            { "Network.RCore1", "DFIntSignalRCoreServerTimeoutMs"},
            { "Network.RCore2", "DFIntSignalRCoreRpcQueueSize"},
            { "Network.RCore3", "DFIntSignalRCoreHubBaseRetryMs"},
            { "Network.RCore4", "DFIntSignalRCoreHandshakeTimeoutMs"},
            { "Network.RCore5", "DFIntSignalRCoreKeepAlivePingPeriodMs"},
            { "Network.RCore6", "DFIntSignalRCoreHubMaxBackoffMs"},

            // Large Replicator
            { "Network.EnableLargeReplicator", "FFlagLargeReplicatorEnabled7"},
            { "Network.LargeReplicatorWrite", "FFlagLargeReplicatorWrite5"},
            { "Network.LargeReplicatorRead", "FFlagLargeReplicatorRead5"},
            { "Network.SerializeRead", "FFlagLargeReplicatorSerializeRead3"},
            { "Network.SerializeWrite", "FFlagLargeReplicatorSerializeWrite3"},
            { "Network.EngineModule1", "FFlagGlobalSettingsEngineModule3"},
            { "Network.EngineModule2", "DFFlagLargeReplicatorEngineModule"},

            // MTU Size
            { "Network.Mtusize","DFIntConnectionMTUSize" },

            // Dynamic Render Resolution
            { "Rendering.Dynamic.Resolution","DFIntDebugDynamicRenderKiloPixels"},

            // Fullscreen bar
            { "UI.FullscreenTitlebarDelay", "FIntFullscreenTitleBarTriggerDelayMillis" },

            // No Shadows
            { "Rendering.Pause.Voxelizer", "DFFlagDebugPauseVoxelizer" },
            { "Rendering.ShadowIntensity", "FIntRenderShadowIntensity" },
            { "Rendering.ShadowMapBias", "FIntRenderShadowmapBias" },

            // Romark
            { "Rendering.Start.Graphic", "FIntRomarkStartWithGraphicQualityLevel" },

            // Refresh Rate
            { "System.TargetRefreshRate1", "DFIntGraphicsOptimizationModeFRMFrameRateTarget" },
    
            // GPU
            { "System.PreferredGPU", "FStringDebugGraphicsPreferredGPUName"},
            { "System.DXT", "FStringGraphicsDisableUnalignedDxtGPUNameBlacklist"},
            { "System.BypassVulkan", "FStringVulkanBuggyRenderpassList2"},

            // Menu stuff
            { "Menu.VRToggles", "FFlagAlwaysShowVRToggleV3" },
            { "Menu.Feedback", "FFlagDisableFeedbackSoothsayerCheck" },
            { "Menu.LanguageSelector", "FIntV1MenuLanguageSelectionFeaturePerMillageRollout" },
            { "Menu.Framerate", "FFlagGameBasicSettingsFramerateCap5"},
            { "Menu.ChatTranslation", "FFlagChatTranslationSettingEnabled3" },
        };

        public static IReadOnlyDictionary<RenderingMode, string> RenderingModes => new Dictionary<RenderingMode, string>
        {
            { RenderingMode.Default, "None" },
            { RenderingMode.D3D11, "D3D11" },
            { RenderingMode.D3D10, "D3D10" },
            { RenderingMode.Metal, "Metal" },
            { RenderingMode.Vulkan, "Vulkan" },
            { RenderingMode.OpenGL, "OpenGL" },

        };

        public static IReadOnlyDictionary<LightingMode, string> LightingModes => new Dictionary<LightingMode, string>
        {
            { LightingMode.Default, "None" },
            { LightingMode.Voxel, "Voxel" },
            { LightingMode.ShadowMap, "ShadowMap" },
            { LightingMode.Future, "Future" },
            { LightingMode.Unified, "Unified" },
        };

        public static IReadOnlyDictionary<ProfileMode, string> ProfileModes => new Dictionary<ProfileMode, string>
        {
            { ProfileMode.Default, "None" },
            { ProfileMode.Yourmom, "Your Mom" },
            { ProfileMode.SoFatlol, "Is So Fat" },

        };

        public static IReadOnlyDictionary<MSAAMode, string?> MSAAModes => new Dictionary<MSAAMode, string?>
        {
            { MSAAMode.Default, null },
            { MSAAMode.x0, "0" },
            { MSAAMode.x1, "1" },
            { MSAAMode.x2, "2" },
            { MSAAMode.x4, "4" },
            { MSAAMode.x8, "8" }
        };

        public static IReadOnlyDictionary<TextureSkipping, string?> TextureSkippingSkips => new Dictionary<TextureSkipping, string?>
        {
            { TextureSkipping.Noskip, null },
            { TextureSkipping.Skip1x, "1" },
            { TextureSkipping.Skip2x, "2" },
            { TextureSkipping.Skip3x, "3" },
            { TextureSkipping.Skip4x, "4" },
            { TextureSkipping.Skip5x, "5" },
            { TextureSkipping.Skip6x, "6" },
            { TextureSkipping.Skip7x, "7" },
            { TextureSkipping.Skip8x, "8" },
            { TextureSkipping.Skip9x, "9" },
            { TextureSkipping.Skip10x, "10" },
        };
        public static IReadOnlyDictionary<TextureQuality, string?> TextureQualityLevels => new Dictionary<TextureQuality, string?>
        {
            { TextureQuality.Default, null },
            { TextureQuality.Lowest, "0" },
            { TextureQuality.Low, "1" },
            { TextureQuality.Medium, "2" },
            { TextureQuality.High, "3" },
        };

        public static IReadOnlyDictionary<DynamicResolution, string?> DynamicResolutions => new Dictionary<DynamicResolution, string?>
        {
            { DynamicResolution.Default, null },
            { DynamicResolution.Resolution1, "37" },
            { DynamicResolution.Resolution2, "77" },
            { DynamicResolution.Resolution3, "230" },
            { DynamicResolution.Resolution4, "410" },
            { DynamicResolution.Resolution5, "922" },
            { DynamicResolution.Resolution6, "2074" },
            { DynamicResolution.Resolution7, "3686" },
            { DynamicResolution.Resolution8, "8294" },
            { DynamicResolution.Resolution9, "33178" },
        };

        public static IReadOnlyDictionary<RefreshRate, string?> RefreshRates => new Dictionary<RefreshRate, string?>
        {
            { RefreshRate.Default, null },
            { RefreshRate.RefreshRate1, "75" },
            { RefreshRate.RefreshRate2, "80" },
            { RefreshRate.RefreshRate3, "90" },
            { RefreshRate.RefreshRate4, "120" },
            { RefreshRate.RefreshRate5, "144" },
            { RefreshRate.RefreshRate6, "165" },
            { RefreshRate.RefreshRate7, "180" },
            { RefreshRate.RefreshRate8, "240" },
            { RefreshRate.RefreshRate9, "360" },

        };

        public static IReadOnlyDictionary<RomarkStart, string?> RomarkStartMappings => new Dictionary<RomarkStart, string?>
        {
            { RomarkStart.Disabled, null },
            { RomarkStart.Bar1, "1" },
            { RomarkStart.Bar2, "2" },
            { RomarkStart.Bar3, "3" },
            { RomarkStart.Bar4, "4" },
            { RomarkStart.Bar5, "5" },
            { RomarkStart.Bar6, "6" },
            { RomarkStart.Bar7, "7" },
            { RomarkStart.Bar8, "8" },
            { RomarkStart.Bar9, "9" },
            { RomarkStart.Bar10, "10" }
        };

        public static IReadOnlyDictionary<QualityLevel, string?> QualityLevels => new Dictionary<QualityLevel, string?>
        {
            { QualityLevel.Disabled, null },
            { QualityLevel.Level1, "1" },
            { QualityLevel.Level2, "2" },
            { QualityLevel.Level3, "3" },
            { QualityLevel.Level4, "4" },
            { QualityLevel.Level5, "5" },
            { QualityLevel.Level6, "6" },
            { QualityLevel.Level7, "7" },
            { QualityLevel.Level8, "8" },
            { QualityLevel.Level9, "9" },
            { QualityLevel.Level10, "10" },
            { QualityLevel.Level11, "11" },
            { QualityLevel.Level12, "12" },
            { QualityLevel.Level13, "13" },
            { QualityLevel.Level14, "14" },
            { QualityLevel.Level15, "15" },
            { QualityLevel.Level16, "16" },
            { QualityLevel.Level17, "17" },
            { QualityLevel.Level18, "18" },
            { QualityLevel.Level19, "19" },
            { QualityLevel.Level20, "20" },
            { QualityLevel.Level21, "21" }
        };

        public bool suspendUndoSnapshot = false;

        // to delete a flag, set the value as null
        public void SetValue(string key, object? value)
        {
            const string LOG_IDENT = "FastFlagManager::SetValue";

            if (!suspendUndoSnapshot)
                SaveUndoSnapshot();

            if (value is null)
            {
                if (Prop.ContainsKey(key))
                    App.Logger.WriteLine(LOG_IDENT, $"Deletion of '{key}' is pending");

                Prop.Remove(key);
            }
            else
            {
                if (Prop.ContainsKey(key))
                {
                    if (key == Prop[key]!.ToString())
                        return;

                    App.Logger.WriteLine(LOG_IDENT, $"Changing of '{key}' from '{Prop[key]}' to '{value}' is pending");
                }
                else
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Setting of '{key}' to '{value}' is pending");
                }

                Prop[key] = value.ToString()!;
            }
        }

        // this returns null if the fflag doesn't exist
        public string? GetValue(string key)
        {
            // check if we have an updated change for it pushed first
            if (Prop.TryGetValue(key, out object? value) && value is not null)
                return value.ToString();

            return null;
        }

        public void SetPreset(string prefix, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
                SetValue(pair.Value, value);
        }

        public void SetPresetEnum(string prefix, string target, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
            {
                if (pair.Key.StartsWith($"{prefix}.{target}"))
                    SetValue(pair.Value, value);
                else
                    SetValue(pair.Value, null);
            }
        }

        public string? GetPreset(string name)
        {
            if (!PresetFlags.ContainsKey(name))
            {
                App.Logger.WriteLine("FastFlagManager::GetPreset", $"Could not find preset {name}");
                Debug.Assert(false, $"Could not find preset {name}");
                return null;
            }

            return GetValue(PresetFlags[name]);
        }

        public T GetPresetEnum<T>(IReadOnlyDictionary<T, string> mapping, string prefix, string value) where T : Enum
        {
            foreach (var pair in mapping)
            {
                if (pair.Value == "None")
                    continue;

                if (GetPreset($"{prefix}.{pair.Value}") == value)
                    return pair.Key;
            }

            return mapping.First().Key;
        }

        public bool IsPreset(string Flag) => PresetFlags.Values.Any(v => v.ToLower() == Flag.ToLower());

        public override void Save()
        {
            // convert all flag values to strings before saving

            foreach (var pair in Prop)
                Prop[pair.Key] = pair.Value!.ToString()!;

            base.Save();

            // clone the dictionary
            OriginalProp = new(Prop);
        }

        public override void Load(bool alertFailure = true)
        {
            base.Load(alertFailure);

            // clone the dictionary
            OriginalProp = new(Prop);

            if (GetPreset("Rendering.ManualFullscreen") != "False")
                SetPreset("Rendering.ManualFullscreen", "False");
        }

        public void DeleteProfile(string Profile)
        {
            try
            {
                string profilesDirectory = Path.Combine(Paths.Base, Paths.SavedFlagProfiles);

                if (!Directory.Exists(profilesDirectory))
                    Directory.CreateDirectory(profilesDirectory);

                if (String.IsNullOrEmpty(Profile))
                    return;

                File.Delete(Path.Combine(profilesDirectory, Profile));
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(ex.Message, MessageBoxImage.Error);
            }
        }

        public IEnumerable<FastFlag> GetAllFlags()
        {
            foreach (var kvp in Prop)
            {
                yield return new FastFlag
                {
                    Name = kvp.Key,
                    Value = kvp.Value?.ToString() ?? "",
                    Preset = "" // optional
                };
            }
        }

        private readonly Stack<Dictionary<string, object?>> undoStack = new();
        private readonly Stack<Dictionary<string, object?>> redoStack = new();

        public void SaveUndoSnapshot()
        {
            // Avoid pushing if last snapshot is identical (optional but nice)
            if (undoStack.Count > 0 && DictionaryEquals(undoStack.Peek(), Prop!))
                return;

            undoStack.Push(new Dictionary<string, object?>(Prop!));
            redoStack.Clear();
        }

        private bool DictionaryEquals(Dictionary<string, object?> a, Dictionary<string, object?> b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var pair in a)
            {
                if (!b.TryGetValue(pair.Key, out var bValue))
                    return false;

                if (!Equals(pair.Value, bValue))
                    return false;
            }

            return true;
        }

        public void Undo()
        {
            if (undoStack.Count == 0)
                return;

            redoStack.Push(new Dictionary<string, object?>(Prop!));

            var previous = undoStack.Pop();

            Prop.Clear();
            foreach (var kvp in previous)
                Prop[kvp.Key] = kvp.Value!;
        }

        public void Redo()
        {
            if (redoStack.Count == 0)
                return;

            undoStack.Push(new Dictionary<string, object?>(Prop!));

            var next = redoStack.Pop();

            Prop.Clear();
            foreach (var kvp in next)
                Prop[kvp.Key] = kvp.Value!;
        }

    }
}
