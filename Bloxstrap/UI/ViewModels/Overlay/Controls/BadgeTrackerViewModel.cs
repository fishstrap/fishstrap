using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private static readonly TimeSpan RemoveGap = TimeSpan.FromMilliseconds(350);

        private readonly ActivityWatcher? _activityWatcher;

        private readonly DispatcherTimer _awardTimer = new() { Interval = AwardCheckInterval };

        private readonly FlashMessage _flash;

        private readonly Dictionary<long, DateTime> _removedAt = new();

        private List<Badge> _pending = new();

        private long _loadedUniverseId;

        private bool _checkingAwards;

        private bool _isRemoving;

        private int _removeProgress;

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

        public bool CanRefresh => !IsBusy && !_isRemoving;

        public bool CanEdit => !_isRemoving;

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

        public string HeaderText => _flash.Text ?? CompletionText;

        public bool AnyRemovable => Badges.Any(x => x.CanRemove);

        public int SelectedCount => Badges.Count(x => x.IsSelected);

        public string SelectionText => _flash.Text ?? String.Format(Strings.Menu_Overlay_Badges_Selected, SelectedCount);

        public bool CanSelectAll => Badges.Any(x => x.CanRemove && !x.IsSelected);

        public bool IsConfirming => _pending.Count > 0 && !_isRemoving;

        public string ConfirmText => _pending.Count == 1
            ? String.Format(Strings.Menu_Overlay_Badges_ConfirmOne, _pending[0].Name)
            : String.Format(Strings.Menu_Overlay_Badges_ConfirmMany, _pending.Count);

        public bool IsRemoving => _isRemoving;

        public string RemovingText => String.Format(Strings.Menu_Overlay_Badges_Removing, _removeProgress, _pending.Count);

        public bool ShowSummary => SelectedCount == 0 && !IsConfirming && !_isRemoving;

        public bool ShowSelectionBar => SelectedCount > 0 && !IsConfirming && !_isRemoving;

        public bool ShowEmptyState => !IsBusy && !Badges.Any();

        public string EmptyText => InGame
            ? Strings.Menu_Overlay_Badges_Empty
            : Strings.Menu_Overlay_Badges_NotInGame;

        private bool InGame => _activityWatcher?.InGame == true && _activityWatcher.Data.UniverseId != 0;

        public ICommand RefreshCommand => new RelayCommand(async () => await LoadAsync(true));

        public ICommand RemoveCommand => new RelayCommand<Badge>(badge =>
        {
            if (badge?.CanRemove == true)
                Ask(new List<Badge> { badge });
        });

        public ICommand RemoveSelectedCommand => new RelayCommand(() => Ask(Badges.Where(x => x.IsSelected && x.CanRemove).ToList()));

        public ICommand ConfirmRemoveCommand => new RelayCommand(async () => await RemovePendingAsync());

        public ICommand CancelRemoveCommand => new RelayCommand(() => Ask(new List<Badge>()));

        public ICommand SelectAllCommand => new RelayCommand(() => Select(x => x.CanRemove));

        public ICommand ClearSelectionCommand => new RelayCommand(() => Select(_ => false));

        public BadgeTrackerViewModel(ActivityWatcher? activityWatcher)
        {
            _activityWatcher = activityWatcher;

            _flash = new FlashMessage(TimeSpan.FromSeconds(4), () =>
            {
                OnPropertyChanged(nameof(HeaderText));
                OnPropertyChanged(nameof(SelectionText));
            });

            if (_activityWatcher is null)
                return;

            _awardTimer.Tick += async (_, _) => await CheckForAwardsAsync();

            _activityWatcher.OnGameJoin += async (_, _) => await App.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
            _activityWatcher.OnGameLeave += (_, _) => App.Current.Dispatcher.Invoke(Clear);
        }

        public async Task LoadAsync(bool force = false)
        {
            const string LOG_IDENT = "BadgeTrackerViewModel::LoadAsync";

            if (IsBusy || _isRemoving)
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

                foreach (Badge badge in badges.Where(x => x.Awarded && !StillEarned(x.Id, x.AwardedDate)))
                {
                    badge.Awarded = false;
                    badge.AwardedDate = null;
                }

                _pending = new List<Badge>();

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

            if (_checkingAwards || IsBusy || _isRemoving || !InGame)
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

                if (universeId != _loadedUniverseId || _isRemoving)
                    return;

                var earned = unearned.Where(x => awarded.TryGetValue(x.Id, out DateTime at) && StillEarned(x.Id, at)).ToList();

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

        private bool StillEarned(long badgeId, DateTime? awardedAt) =>
            !_removedAt.TryGetValue(badgeId, out DateTime removedAt) || (awardedAt is DateTime at && at.ToUniversalTime() > removedAt);

        private void Ask(List<Badge> badges)
        {
            if (_isRemoving)
                return;

            _pending = badges;

            RemovalChanged();
        }

        private void Select(Func<Badge, bool> selected)
        {
            if (_isRemoving)
                return;

            foreach (Badge badge in Badges)
                badge.IsSelected = badge.CanRemove && selected(badge);
        }

        private async Task RemovePendingAsync()
        {
            const string LOG_IDENT = "BadgeTrackerViewModel::RemovePendingAsync";

            if (_isRemoving || _pending.Count == 0)
                return;

            List<Badge> targets = _pending.ToList();
            int failed = 0;

            _isRemoving = true;
            _removeProgress = 0;

            RemovalChanged();

            foreach (Badge badge in targets)
            {
                _removeProgress++;

                OnPropertyChanged(nameof(RemovingText));

                try
                {
                    await BadgesApi.RemoveAsync(badge.Id);

                    _removedAt[badge.Id] = DateTime.UtcNow;

                    badge.Awarded = false;
                    badge.AwardedDate = null;
                    badge.IsSelected = false;
                }
                catch (Exception ex)
                {
                    failed++;

                    App.Logger.WriteLine(LOG_IDENT, $"Failed to remove badge {badge.Id}");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }

                if (badge != targets[^1])
                    await Task.Delay(RemoveGap);
            }

            int removed = targets.Count - failed;

            App.Logger.WriteLine(LOG_IDENT, $"Removed {removed} of {targets.Count} badge(s)");

            _isRemoving = false;
            _pending = new List<Badge>();

            Show(Badges.ToList());

            Refreshed();

            if (_activityWatcher is not null && Badges.Any(x => x.AwardedKnown && !x.Awarded))
                _awardTimer.Start();

            if (failed > 0)
                _flash.Show(String.Format(Strings.Menu_Overlay_Badges_RemoveFailed, failed, targets.Count));
            else if (removed == 1)
                _flash.Show(String.Format(Strings.Menu_Overlay_Badges_RemovedOne, targets[0].Name));
            else
                _flash.Show(String.Format(Strings.Menu_Overlay_Badges_RemovedMany, removed));

            RemovalChanged();
        }

        private void Show(IEnumerable<Badge> badges)
        {
            var ordered = badges.OrderBy(x => x.Awarded).ThenByDescending(x => x.WinRatePercentage).ToList();

            foreach (Badge badge in Badges)
                badge.PropertyChanged -= OnBadgeChanged;

            Badges.Clear();

            foreach (Badge badge in ordered)
            {
                badge.PropertyChanged += OnBadgeChanged;

                Badges.Add(badge);
            }
        }

        private void OnBadgeChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Badge.IsSelected))
                return;

            _flash.Clear();

            SelectionChanged();
        }

        private void Clear()
        {
            _awardTimer.Stop();

            foreach (Badge badge in Badges)
                badge.PropertyChanged -= OnBadgeChanged;

            Badges.Clear();

            _pending = new List<Badge>();
            _loadedUniverseId = 0;

            Refreshed();
        }

        private void SelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectionText));
            OnPropertyChanged(nameof(CanSelectAll));
            OnPropertyChanged(nameof(ShowSummary));
            OnPropertyChanged(nameof(ShowSelectionBar));
        }

        private void RemovalChanged()
        {
            OnPropertyChanged(nameof(IsConfirming));
            OnPropertyChanged(nameof(ConfirmText));
            OnPropertyChanged(nameof(IsRemoving));
            OnPropertyChanged(nameof(RemovingText));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanRefresh));

            SelectionChanged();
        }

        private void Refreshed()
        {
            OnPropertyChanged(nameof(EarnedCount));
            OnPropertyChanged(nameof(CompletionPercentage));
            OnPropertyChanged(nameof(CompletionText));
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(AnyRemovable));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));

            RemovalChanged();
        }
    }
}
