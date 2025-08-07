using Bloxstrap.RobloxInterfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Bloxstrap.UI.ViewModels.Dialogs
{
    public class ChannelListsViewModel : NotifyPropertyChangedViewModel
    {
        private const string ChannelsJsonUrl = "https://raw.githubusercontent.com/SCR00M/gsagsssssssagdsgadgsgds/refs/heads/main/Channels.json";

        private readonly Dictionary<string, ClientVersion> _channelInfoCache = new();

        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Froststrap",
            "channelCache.json");

        private static readonly string CacheMetaFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Froststrap",
            "channelCacheMeta.json");

        private const int CacheExpiryHours = 24;

        private DateTime _lastCacheUpdate = DateTime.MinValue;

        public ObservableCollection<DeployInfoDisplay> Channels { get; } = new();

        public ICommand RefreshCommand { get; }

        public ChannelListsViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await ForceRefreshAsync());

            _ = InitializeAsync();
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        private async Task InitializeAsync()
        {
            await LoadCacheMetaAsync();
            await LoadCacheFromDiskAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Channels.Clear();
                foreach (var kvp in _channelInfoCache.OrderBy(k => k.Key))
                {
                    Channels.Add(new DeployInfoDisplay
                    {
                        ChannelName = kvp.Key,
                        Version = kvp.Value.Version,
                        VersionGuid = kvp.Value.VersionGuid
                    });
                }
            });

            if (DateTime.UtcNow - _lastCacheUpdate > TimeSpan.FromHours(CacheExpiryHours))
            {
                await RefreshAsync();
            }
        }

        private async Task ForceRefreshAsync()
        {
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                using var client = new HttpClient();

                var json = await client.GetStringAsync(ChannelsJsonUrl);
                var channelNames = JsonSerializer.Deserialize<string[]>(json);

                if (channelNames == null)
                    return;

                var semaphore = new SemaphoreSlim(20);
                var tempCache = new Dictionary<string, ClientVersion>();
                var tasks = new List<Task>();

                foreach (var channel in channelNames)
                {
                    await semaphore.WaitAsync();

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var info = await Deployment.GetInfo(channel);

                            lock (tempCache)
                                tempCache[channel] = info;
                        }
                        catch (InvalidChannelException) { }
                        catch { }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                lock (_channelInfoCache)
                {
                    _channelInfoCache.Clear();
                    foreach (var kvp in tempCache)
                        _channelInfoCache[kvp.Key] = kvp.Value;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Channels.Clear();
                    foreach (var kvp in _channelInfoCache.OrderBy(k => k.Key))
                    {
                        Channels.Add(new DeployInfoDisplay
                        {
                            ChannelName = kvp.Key,
                            Version = kvp.Value.Version,
                            VersionGuid = kvp.Value.VersionGuid
                        });
                    }
                });

                _lastCacheUpdate = DateTime.UtcNow;

                await SaveCacheToDiskAsync();
                await SaveCacheMetaAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ChannelListsViewModel::RefreshAsync", $"Failed to refresh channels: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveCacheToDiskAsync()
        {
            try
            {
                var folder = Path.GetDirectoryName(CacheFilePath);
                if (!Directory.Exists(folder!))
                    Directory.CreateDirectory(folder!);

                string json;
                lock (_channelInfoCache)
                {
                    json = JsonSerializer.Serialize(_channelInfoCache);
                }
                await File.WriteAllTextAsync(CacheFilePath, json);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ChannelListsViewModel::SaveCacheToDisk", $"Failed to save cache: {ex.Message}");
            }
        }

        private async Task LoadCacheFromDiskAsync()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                    return;

                var json = await File.ReadAllTextAsync(CacheFilePath);
                var cache = JsonSerializer.Deserialize<Dictionary<string, ClientVersion>>(json);
                if (cache != null)
                {
                    lock (_channelInfoCache)
                    {
                        _channelInfoCache.Clear();
                        foreach (var kvp in cache)
                            _channelInfoCache[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ChannelListsViewModel::LoadCacheFromDisk", $"Failed to load cache: {ex.Message}");
            }
        }

        private async Task SaveCacheMetaAsync()
        {
            try
            {
                var folder = Path.GetDirectoryName(CacheMetaFilePath);
                if (!Directory.Exists(folder!))
                    Directory.CreateDirectory(folder!);

                var json = JsonSerializer.Serialize(new CacheMeta { LastUpdatedUtc = _lastCacheUpdate });
                await File.WriteAllTextAsync(CacheMetaFilePath, json);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ChannelListsViewModel::SaveCacheMeta", $"Failed to save cache meta: {ex.Message}");
            }
        }

        private async Task LoadCacheMetaAsync()
        {
            try
            {
                if (!File.Exists(CacheMetaFilePath))
                    return;

                var json = await File.ReadAllTextAsync(CacheMetaFilePath);
                var meta = JsonSerializer.Deserialize<CacheMeta>(json);
                if (meta != null)
                {
                    _lastCacheUpdate = meta.LastUpdatedUtc;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ChannelListsViewModel::LoadCacheMeta", $"Failed to load cache meta: {ex.Message}");
            }
        }

        private class CacheMeta
        {
            public DateTime LastUpdatedUtc { get; set; }
        }
    }

    public class DeployInfoDisplay
    {
        public string ChannelName { get; set; } = null!;
        public string Version { get; set; } = null!;
        public string VersionGuid { get; set; } = null!;
    }
}