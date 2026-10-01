using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class OnlineStatusOption
    {
        public string Value { get; init; } = String.Empty;

        public string Label { get; init; } = String.Empty;

        public bool IsCurrent { get; init; }
    }

    public class OnlineStatusViewModel : NotifyPropertyChangedViewModel
    {
        private string? _online;

        private string? _join;

        private bool _busy;

        private string? _status;

        private bool _isOpen;

        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen == value)
                    return;

                _isOpen = value;

                OnPropertyChanged(nameof(IsOpen));

                if (value)
                    _ = LoadAsync();
            }
        }

        public IReadOnlyList<OnlineStatusOption> Options => PrivacySettings.OnlineLevels
            .Select(x => new OnlineStatusOption { Value = x, Label = LabelFor(x), IsCurrent = x == _online })
            .ToList();

        public bool CanChange => !_busy && _online is not null;

        public string StatusText => _status ?? String.Empty;

        public bool HasStatus => _status is not null;

        public ICommand OpenCommand => new RelayCommand(() => IsOpen = !IsOpen);

        public ICommand SetCommand => new RelayCommand<string>(async value => await SetAsync(value));

        private async Task LoadAsync()
        {
            const string LOG_IDENT = "OnlineStatusViewModel::LoadAsync";

            if (_busy)
                return;

            if (!await App.Cookies.EnsureLoadedAsync())
            {
                _online = null;
                Show(Strings.Menu_Overlay_Privacy_NeedsCookies);
                Refreshed();
                return;
            }

            _busy = true;
            Show(Strings.Menu_Overlay_Privacy_Loading);

            try
            {
                (_online, _join) = await PrivacySettings.FetchAsync();

                App.Logger.WriteLine(LOG_IDENT, $"Online visibility is {_online ?? "unreported"}, joining is {_join ?? "unreported"}");

                Show(null);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to read the privacy settings");
                App.Logger.WriteException(LOG_IDENT, ex);

                _online = null;
                Show(Strings.Menu_Overlay_Privacy_LoadFailed);
            }
            finally
            {
                _busy = false;

                Refreshed();
            }
        }

        private async Task SetAsync(string? value)
        {
            const string LOG_IDENT = "OnlineStatusViewModel::SetAsync";

            if (value is null || !CanChange || value == _online)
                return;

            _busy = true;
            Show(Strings.Menu_Overlay_Privacy_Saving);
            Refreshed();

            string message;

            try
            {
                bool narrowed = await PrivacySettings.SetOnlineVisibilityAsync(value, _join);

                App.Logger.WriteLine(LOG_IDENT, $"Online visibility is now {value}{(narrowed ? ", with joining narrowed to match" : "")}");

                message = narrowed ? Strings.Menu_Overlay_Privacy_JoinNarrowed : Strings.Menu_Overlay_Privacy_Saved;
            }
            catch (SettingRejectedException ex) when (!String.IsNullOrWhiteSpace(ex.Reason))
            {
                App.Logger.WriteLine(LOG_IDENT, $"Roblox refused it: {ex.Message}");

                message = String.Format(Strings.Menu_Overlay_Privacy_Rejected, ex.Reason);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to set online visibility to {value}");
                App.Logger.WriteException(LOG_IDENT, ex);

                message = Strings.Menu_Overlay_Privacy_SaveFailed;
            }

            try
            {
                (_online, _join) = await PrivacySettings.FetchAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to read the privacy settings back");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            _busy = false;
            Show(message);
            Refreshed();
        }

        private static string LabelFor(string value) => value switch
        {
            "AllUsers" => Strings.Menu_Overlay_Privacy_Everyone,
            "FriendsFollowingAndFollowers" => Strings.Menu_Overlay_Privacy_FriendsFollowingAndFollowers,
            "FriendsAndFollowing" => Strings.Menu_Overlay_Privacy_FriendsAndFollowing,
            "Friends" => Strings.Menu_Overlay_Privacy_Friends,
            "TrustedFriends" => Strings.Menu_Overlay_Privacy_TrustedFriends,
            "NoOne" => Strings.Menu_Overlay_Privacy_NoOne,
            _ => value
        };

        private void Show(string? status)
        {
            _status = status;

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(HasStatus));
        }

        private void Refreshed()
        {
            OnPropertyChanged(nameof(Options));
            OnPropertyChanged(nameof(CanChange));
        }
    }
}
