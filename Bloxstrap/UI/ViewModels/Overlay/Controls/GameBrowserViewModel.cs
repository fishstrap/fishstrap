using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Integrations;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class GameBrowserViewModel : NotifyPropertyChangedViewModel
    {
        private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(400);

        private readonly ActivityWatcher? _activityWatcher;

        private readonly DispatcherTimer _searchTimer;

        private readonly FlashMessage _flash;

        private int _searchGeneration;

        private bool _favoritesLoaded;

        private bool _favoritesFailed;

        private bool _searchFailed;

        public ObservableCollection<GameTile> SearchResults { get; } = new();

        public ObservableCollection<GameTile> Favorites { get; } = new();

        public ObservableCollection<GameTile> ActiveTiles => ShowingFavorites ? Favorites : SearchResults;

        private bool _showingFavorites;

        public bool ShowingFavorites
        {
            get => _showingFavorites;
            set
            {
                if (_showingFavorites == value)
                    return;

                _showingFavorites = value;

                OnPropertyChanged(nameof(ShowingFavorites));
                OnPropertyChanged(nameof(ShowingSearch));
                OnPropertyChanged(nameof(ActiveTiles));

                Refreshed();

                if (value && !_favoritesLoaded)
                    _ = LoadFavoritesAsync();
            }
        }

        public bool ShowingSearch
        {
            get => !ShowingFavorites;
            set => ShowingFavorites = !value;
        }

        private string _query = String.Empty;

        public string Query
        {
            get => _query;
            set
            {
                if (_query == value)
                    return;

                _query = value;

                OnPropertyChanged(nameof(Query));

                _searchTimer.Stop();

                if (String.IsNullOrWhiteSpace(value))
                {
                    _searchGeneration++;
                    _searchFailed = false;

                    SearchResults.Clear();

                    IsBusy = false;
                    Refreshed();

                    return;
                }

                _searchTimer.Start();
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;

                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public string StatusText => _flash.Text ?? String.Empty;

        public bool HasStatus => _flash.Text is not null;

        public bool ShowEmptyState => !IsBusy && !ActiveTiles.Any();

        public string EmptyText
        {
            get
            {
                if (ShowingFavorites)
                {
                    if (_favoritesFailed)
                        return Strings.Menu_Overlay_Games_Failed;

                    return UserId == 0 ? Strings.Menu_Overlay_Games_NoAccount : Strings.Menu_Overlay_Games_NoFavorites;
                }

                if (String.IsNullOrWhiteSpace(Query))
                    return Strings.Menu_Overlay_Games_SearchPrompt;

                return _searchFailed
                    ? Strings.Menu_Overlay_Games_Failed
                    : String.Format(Strings.Menu_Overlay_Games_NoResults, Query.Trim());
            }
        }

        private long UserId
        {
            get
            {
                if (_activityWatcher?.Data.UserId is long playing and > 0)
                    return playing;

                return App.Cookies.CurrentUser?.Id ?? 0;
            }
        }

        public ICommand JoinCommand => new RelayCommand<GameTile>(Join);

        public GameBrowserViewModel(ActivityWatcher? activityWatcher)
        {
            _activityWatcher = activityWatcher;

            _flash = new FlashMessage(TimeSpan.FromSeconds(4), () =>
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(HasStatus));
            });

            _searchTimer = new DispatcherTimer { Interval = SearchDelay };
            _searchTimer.Tick += async (_, _) =>
            {
                _searchTimer.Stop();

                await SearchAsync(Query);
            };
        }

        private async Task SearchAsync(string query)
        {
            const string LOG_IDENT = "GameBrowserViewModel::SearchAsync";

            int generation = ++_searchGeneration;

            IsBusy = true;
            _searchFailed = false;

            Refreshed();

            try
            {
                List<GameTile> results = await Experiences.SearchAsync(query.Trim());

                if (generation != _searchGeneration)
                    return;

                SearchResults.Clear();

                foreach (GameTile tile in results)
                    SearchResults.Add(tile);
            }
            catch (Exception ex)
            {
                if (generation != _searchGeneration)
                    return;

                App.Logger.WriteLine(LOG_IDENT, "Search failed");
                App.Logger.WriteException(LOG_IDENT, ex);

                SearchResults.Clear();

                _searchFailed = true;
            }
            finally
            {
                if (generation == _searchGeneration)
                {
                    IsBusy = false;
                    Refreshed();
                }
            }
        }

        public async Task LoadFavoritesAsync()
        {
            const string LOG_IDENT = "GameBrowserViewModel::LoadFavoritesAsync";

            long userId = UserId;

            if (userId == 0)
            {
                Refreshed();
                return;
            }

            IsBusy = true;
            _favoritesFailed = false;

            Refreshed();

            try
            {
                List<GameTile> favorites = await Experiences.FavoritesAsync(userId);

                Favorites.Clear();

                foreach (GameTile tile in favorites)
                    Favorites.Add(tile);

                _favoritesLoaded = true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load favorites");
                App.Logger.WriteException(LOG_IDENT, ex);

                _favoritesFailed = true;
            }
            finally
            {
                IsBusy = false;

                Refreshed();
            }
        }

        private void Join(GameTile? tile)
        {
            const string LOG_IDENT = "GameBrowserViewModel::Join";

            if (tile is null)
                return;

            try
            {
                Experiences.Join(tile);

                Flash(String.Format(Strings.Menu_Overlay_Games_Joining, tile.Name));
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to join {tile.PlaceId}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void Flash(string message) => _flash.Show(message);

        private void Refreshed()
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));
        }
    }
}
