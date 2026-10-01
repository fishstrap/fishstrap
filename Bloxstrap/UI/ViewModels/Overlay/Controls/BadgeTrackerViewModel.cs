using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Integrations;
using BadgesApi = Bloxstrap.RobloxInterfaces.Badges;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class BadgeTrackerViewModel : NotifyPropertyChangedViewModel
    {
        private static readonly TimeSpan AwardCheckInterval = TimeSpan.FromSeconds(60);

        private readonly ActivityWatcher? _activityWatcher;

        private readonly DispatcherTimer _awardTimer = new() { Interval = AwardCheckInterval };

        private long _loadedUniverseId;

        private bool _checkingAwards;

        public event EventHandler<Badge>? BadgeEarned;

        public ObservableCollection<Badge> Badges { get; } = new();

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;

                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(CanRefresh));
            }
        }

        public bool CanRefresh => !IsBusy;

        public int EarnedCount => Badges.Count(x => x.Awarded);

        private bool ProgressKnown => Badges.Any(x => x.AwardedKnown);

        public double CompletionPercentage => Badges.Any() && ProgressKnown ? (double)EarnedCount / Badges.Count * 100 : 0;

        public string CompletionText
        {
            get
            {
                if (!Badges.Any())
                    return String.Empty;

                return ProgressKnown
                    ? String.Format(Strings.Menu_Overlay_Badges_Progress, EarnedCount, Badges.Count)
                    : String.Format(Strings.Menu_Overlay_Badges_ProgressUnknown, Badges.Count);
            }
        }

        public bool ShowEmptyState => !IsBusy && !Badges.Any();

        public string EmptyText => InGame
            ? Strings.Menu_Overlay_Badges_Empty
            : Strings.Menu_Overlay_Badges_NotInGame;

        private bool InGame => _activityWatcher?.InGame == true && _activityWatcher.Data.UniverseId != 0;

        public ICommand RefreshCommand => new RelayCommand(async () => await LoadAsync(true));

        public BadgeTrackerViewModel(ActivityWatcher? activityWatcher)
        {
            _activityWatcher = activityWatcher;

            if (_activityWatcher is null)
                return;

            _awardTimer.Tick += async (_, _) => await CheckForAwardsAsync();

            _activityWatcher.OnGameJoin += async (_, _) => await App.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
            _activityWatcher.OnGameLeave += (_, _) => App.Current.Dispatcher.Invoke(Clear);
        }

        public async Task LoadAsync(bool force = false)
        {
            const string LOG_IDENT = "BadgeTrackerViewModel::LoadAsync";

            if (IsBusy)
                return;

            if (!InGame)
            {
                Clear();
                return;
            }

            long universeId = _activityWatcher!.Data.UniverseId;

            if (!force && universeId == _loadedUniverseId && Badges.Any())
                return;

            IsBusy = true;

            try
            {
                var badges = await BadgesApi.FetchAsync(universeId, _activityWatcher.Data.UserId);

                Show(badges);

                _loadedUniverseId = universeId;

                if (Badges.Any(x => x.AwardedKnown && !x.Awarded))
                    _awardTimer.Start();
                else
                    _awardTimer.Stop();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load badges");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                IsBusy = false;

                Refreshed();
            }
        }

        public async Task CheckForAwardsAsync()
        {
            const string LOG_IDENT = "BadgeTrackerViewModel::CheckForAwardsAsync";

            if (_checkingAwards || IsBusy || !InGame)
                return;

            var unearned = Badges.Where(x => x.AwardedKnown && !x.Awarded).ToList();

            if (!unearned.Any())
            {
                _awardTimer.Stop();
                return;
            }

            long universeId = _loadedUniverseId;

            _checkingAwards = true;

            try
            {
                var awarded = await BadgesApi.AwardedDatesAsync(_activityWatcher!.Data.UserId, unearned.Select(x => x.Id));

                if (universeId != _loadedUniverseId)
                    return;

                var earned = unearned.Where(x => awarded.ContainsKey(x.Id)).ToList();

                if (!earned.Any())
                    return;

                foreach (Badge badge in earned)
                {
                    badge.Awarded = true;
                    badge.AwardedDate = awarded[badge.Id];
                }

                App.Logger.WriteLine(LOG_IDENT, $"Earned {earned.Count} badge(s) since the last check");

                Show(Badges.ToList());

                Refreshed();

                foreach (Badge badge in earned)
                    BadgeEarned?.Invoke(this, badge);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to check for new badges");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                _checkingAwards = false;
            }
        }

        private void Show(IEnumerable<Badge> badges)
        {
            var ordered = badges.OrderBy(x => x.Awarded).ThenByDescending(x => x.WinRatePercentage).ToList();

            Badges.Clear();

            foreach (Badge badge in ordered)
                Badges.Add(badge);
        }

        private void Clear()
        {
            _awardTimer.Stop();

            Badges.Clear();

            _loadedUniverseId = 0;

            Refreshed();
        }

        private void Refreshed()
        {
            OnPropertyChanged(nameof(EarnedCount));
            OnPropertyChanged(nameof(CompletionPercentage));
            OnPropertyChanged(nameof(CompletionText));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));
        }
    }
}
