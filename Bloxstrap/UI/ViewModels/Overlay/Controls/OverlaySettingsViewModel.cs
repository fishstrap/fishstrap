using System.Windows.Input;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Enums.Overlay;
using Bloxstrap.UI.Elements.Overlay;

using AppSettings = Bloxstrap.Models.Persistable.Settings;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class OverlaySettingsViewModel : NotifyPropertyChangedViewModel
    {
        private const string FallbackImage = "pack://application:,,,/Bloxstrap.ico";

        private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(600);

        private readonly Integrations.Overlay? _overlay;

        private readonly GameOverlay? _window;

        private bool _rebindQueued;

        private readonly Func<string?> _avatar;

        private readonly DispatcherTimer _saveTimer;

        private bool _unsaved;

        public event EventHandler? AppearanceChanged;

        private static AppSettings Current => App.Settings.Prop;

        public ToastAppearance Appearance => ToastAppearance.Current;

        public IEnumerable<ToastPosition> Positions { get; } = Enum.GetValues<ToastPosition>();

        public double MinCornerRadius => ToastAppearance.MinCornerRadius;

        public double MaxCornerRadius => ToastAppearance.MaxCornerRadius;

        public double MinSize => ToastAppearance.MinScale * 100;

        public double MaxSize => ToastAppearance.MaxScale * 100;

        public double MinBackgroundOpacity => ToastAppearance.MinBackgroundOpacity * 100;

        public double MaxBackgroundOpacity => ToastAppearance.MaxBackgroundOpacity * 100;

        public double MinDuration => ToastAppearance.MinDuration;

        public double MaxDuration => ToastAppearance.MaxDuration;

        public bool IsCorner
        {
            get => Appearance.Style == ToastStyle.Corner;
            set
            {
                if (value)
                    SetStyle(ToastStyle.Corner);
            }
        }

        public bool IsFloating
        {
            get => Appearance.Style == ToastStyle.Floating;
            set
            {
                if (value)
                    SetStyle(ToastStyle.Floating);
            }
        }

        public ToastPosition Position
        {
            get => Appearance.Position;
            set
            {
                Current.OverlayToastPosition = value;
                Changed(nameof(Position));
            }
        }

        public double CornerRadius
        {
            get => Appearance.CornerRadius;
            set
            {
                Current.OverlayToastCornerRadius = Math.Round(value);
                Changed(nameof(CornerRadius), nameof(CornerRadiusText));
            }
        }

        public string CornerRadiusText => String.Format(Strings.Menu_Overlay_Settings_Pixels, Appearance.CornerRadius);

        public double Size
        {
            get => Appearance.Scale * 100;
            set
            {
                Current.OverlayToastScale = Math.Round(value) / 100;
                Changed(nameof(Size), nameof(SizeText));
            }
        }

        public string SizeText => String.Format(Strings.Menu_Overlay_Settings_Percent, Math.Round(Appearance.Scale * 100));

        public double BackgroundOpacity
        {
            get => Appearance.BackgroundOpacity * 100;
            set
            {
                Current.OverlayToastOpacity = Math.Round(value) / 100;
                Changed(nameof(BackgroundOpacity), nameof(BackgroundOpacityText));
            }
        }

        public string BackgroundOpacityText => String.Format(Strings.Menu_Overlay_Settings_Percent, Math.Round(Appearance.BackgroundOpacity * 100));

        public double Duration
        {
            get => Appearance.Duration;
            set
            {
                Current.OverlayToastDuration = (int)Math.Round(value);
                Changed(nameof(Duration), nameof(DurationText));
            }
        }

        public string DurationText => String.Format(Strings.Menu_Overlay_Settings_Seconds, Appearance.Duration);

        public bool HeaderOff
        {
            get => !Current.OverlayToastHeaderServer && !Current.OverlayToastHeaderFriends;
            set
            {
                if (value)
                    SetHeader(false, false);
            }
        }

        public bool HeaderServer
        {
            get => Current.OverlayToastHeaderServer && !Current.OverlayToastHeaderFriends;
            set
            {
                if (value)
                    SetHeader(true, false);
            }
        }

        public bool HeaderFriends
        {
            get => !Current.OverlayToastHeaderServer && Current.OverlayToastHeaderFriends;
            set
            {
                if (value)
                    SetHeader(false, true);
            }
        }

        public bool HeaderBoth
        {
            get => Current.OverlayToastHeaderServer && Current.OverlayToastHeaderFriends;
            set
            {
                if (value)
                    SetHeader(true, true);
            }
        }

        public bool ServerToasts
        {
            get => Current.OverlayServerToasts;
            set
            {
                Current.OverlayServerToasts = value;
                Changed(nameof(ServerToasts));
            }
        }

        public bool FriendNotifications
        {
            get => Current.OverlayFriendNotifications;
            set
            {
                Current.OverlayFriendNotifications = value;
                _overlay?.SetFriendNotifications(value);
                Changed(nameof(FriendNotifications));
            }
        }

        public bool BadgeNotifications
        {
            get => Current.OverlayBadgeNotifications;
            set
            {
                Current.OverlayBadgeNotifications = value;
                Changed(nameof(BadgeNotifications));
            }
        }

        public bool StartHint
        {
            get => Current.OverlayStartHint;
            set
            {
                Current.OverlayStartHint = value;
                Changed(nameof(StartHint));
            }
        }

        public ModifierKeys HotkeyModifiers
        {
            get => Current.OverlayHotkeyModifiers;
            set
            {
                Current.OverlayHotkeyModifiers = value;
                HotkeyChanged(nameof(HotkeyModifiers));
            }
        }

        public Key HotkeyKey
        {
            get => Current.OverlayHotkeyKey;
            set
            {
                Current.OverlayHotkeyKey = value;
                HotkeyChanged(nameof(HotkeyKey));
            }
        }

        public bool HotkeyTaken => _window is not null && !String.IsNullOrEmpty(_window.Shortcut) && !_window.HotkeyRegistered;

        public string HotkeyTakenText => HotkeyTaken ? String.Format(Strings.Menu_Overlay_Settings_ShortcutTaken, _window!.Shortcut) : String.Empty;

        public ICommand ResetHotkeyCommand => new RelayCommand(ResetHotkey);

        public OverlayNotice Sample
        {
            get
            {
                if (Current.OverlayToastHeaderServer && !Current.OverlayToastHeaderFriends)
                {
                    return new OverlayNotice(Strings.ContextMenu_ServerInformation_Notification_Title_Public,
                        String.Format(Strings.ContextMenu_ServerDetails_Notification_Text, Strings.Menu_Overlay_Settings_SampleLocation, Strings.Menu_Overlay_Settings_SampleUptime),
                        Kind: NoticeKind.Server);
                }

                string? avatar = _avatar();
                bool hasAvatar = !String.IsNullOrEmpty(avatar);

                return new OverlayNotice(Strings.Menu_Overlay_Settings_SampleTitle, Strings.Menu_Overlay_Settings_SampleMessage,
                    hasAvatar ? avatar : FallbackImage, hasAvatar, NoticeKind.Friend);
            }
        }

        public ICommand TestCommand => new RelayCommand(() => _overlay?.Preview(Sample));

        public ICommand ResetCommand => new RelayCommand(Reset);

        public OverlaySettingsViewModel(Integrations.Overlay? overlay, GameOverlay? window, Func<string?> avatar)
        {
            _overlay = overlay;
            _window = window;
            _avatar = avatar;

            _saveTimer = new DispatcherTimer { Interval = SaveDelay };
            _saveTimer.Tick += (_, _) => Save();
        }

        private void ResetHotkey()
        {
            Current.OverlayHotkeyModifiers = OverlayHotkey.DefaultModifiers;
            Current.OverlayHotkeyKey = OverlayHotkey.DefaultKey;

            HotkeyChanged(nameof(HotkeyModifiers), nameof(HotkeyKey));
        }

        private void HotkeyChanged(params string[] names)
        {
            foreach (string name in names)
                OnPropertyChanged(name);

            MarkUnsaved();

            if (_rebindQueued)
                return;

            _rebindQueued = true;

            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                _rebindQueued = false;
                RebindHotkey();
            }, DispatcherPriority.Background);
        }

        public void RebindHotkey()
        {
            _window?.RebindHotkey();

            RefreshHotkeyStatus();
        }

        public void RefreshHotkeyStatus()
        {
            OnPropertyChanged(nameof(HotkeyTaken));
            OnPropertyChanged(nameof(HotkeyTakenText));
        }

        private void SetHeader(bool server, bool friends)
        {
            Current.OverlayToastHeaderServer = server;
            Current.OverlayToastHeaderFriends = friends;

            Changed(nameof(HeaderOff), nameof(HeaderServer), nameof(HeaderFriends), nameof(HeaderBoth));
        }

        private void SetStyle(ToastStyle style)
        {
            Current.OverlayToastStyle = style;
            Changed(nameof(IsCorner), nameof(IsFloating));
        }

        private void Reset()
        {
            ToastAppearance defaults = ToastAppearance.Default;

            Current.OverlayToastStyle = defaults.Style;
            Current.OverlayToastPosition = defaults.Position;
            Current.OverlayToastCornerRadius = defaults.CornerRadius;
            Current.OverlayToastScale = defaults.Scale;
            Current.OverlayToastOpacity = defaults.BackgroundOpacity;
            Current.OverlayToastDuration = defaults.Duration;
            Current.OverlayToastHeaderServer = defaults.HeaderServer;
            Current.OverlayToastHeaderFriends = defaults.HeaderFriends;

            Changed(nameof(IsCorner), nameof(IsFloating), nameof(Position), nameof(CornerRadius), nameof(CornerRadiusText),
                nameof(Size), nameof(SizeText), nameof(BackgroundOpacity), nameof(BackgroundOpacityText),
                nameof(Duration), nameof(DurationText), nameof(HeaderOff), nameof(HeaderServer), nameof(HeaderFriends), nameof(HeaderBoth));
        }

        private void Changed(params string[] names)
        {
            foreach (string name in names)
                OnPropertyChanged(name);

            AppearanceChanged?.Invoke(this, EventArgs.Empty);

            MarkUnsaved();
        }

        private void MarkUnsaved()
        {
            _unsaved = true;

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        public void Save()
        {
            const string LOG_IDENT = "OverlaySettingsViewModel::Save";

            _saveTimer.Stop();

            if (!_unsaved)
                return;

            _unsaved = false;

            try
            {
                AppSettings chosen = Current;

                if (File.Exists(App.Settings.FileLocation) && App.Settings.HasFileOnDiskChanged())
                {
                    App.Settings.Load(false);
                    CopyOverlaySettings(chosen, App.Settings.Prop);
                }

                App.Settings.Save();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to save the overlay settings");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private static void CopyOverlaySettings(AppSettings from, AppSettings to)
        {
            to.OverlayHotkeyModifiers = from.OverlayHotkeyModifiers;
            to.OverlayHotkeyKey = from.OverlayHotkeyKey;
            to.OverlayToastStyle = from.OverlayToastStyle;
            to.OverlayToastPosition = from.OverlayToastPosition;
            to.OverlayToastCornerRadius = from.OverlayToastCornerRadius;
            to.OverlayToastScale = from.OverlayToastScale;
            to.OverlayToastOpacity = from.OverlayToastOpacity;
            to.OverlayToastDuration = from.OverlayToastDuration;
            to.OverlayToastHeaderServer = from.OverlayToastHeaderServer;
            to.OverlayToastHeaderFriends = from.OverlayToastHeaderFriends;
            to.OverlayServerToasts = from.OverlayServerToasts;
            to.OverlayFriendNotifications = from.OverlayFriendNotifications;
            to.OverlayBadgeNotifications = from.OverlayBadgeNotifications;
            to.OverlayStartHint = from.OverlayStartHint;
        }
    }
}
