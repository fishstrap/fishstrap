using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Enums.Overlay;
using Bloxstrap.Integrations.OverlayModules;
using Bloxstrap.Models.APIs.RobloxParty;
using Bloxstrap.Models.APIs.RobloxParty.Events;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class FriendActivityViewModel : NotifyPropertyChangedViewModel
    {
        private static readonly TimeSpan GroupGap = TimeSpan.FromMinutes(5);

        private const int DetailsBatch = 100;

        private readonly Integrations.Overlay? _overlay;

        private readonly RobloxParty? _party;

        private readonly FriendPresence? _presence;

        private readonly Dictionary<long, ChatFriend> _people = new();

        private readonly List<ChatFriend> _groups = new();

        private readonly Dictionary<FriendSection, FriendListHeader> _headers;

        private AuthenticatedUser? _me;

        private string? _myAvatar;

        private bool _loading;

        private bool _loaded;

        private string? _listStatus;

        public ObservableCollection<object> Rows { get; } = new();

        public ObservableCollection<ChatTab> Tabs { get; } = new();

        public event EventHandler? MessagesAdded;

        private ChatTab? _selectedTab;

        public ChatTab? SelectedTab
        {
            get => _selectedTab;
            private set
            {
                if (_selectedTab is not null)
                    _selectedTab.IsSelected = false;

                _selectedTab = value;

                if (_selectedTab is not null)
                {
                    _selectedTab.IsSelected = true;
                    _selectedTab.Friend.Unread = 0;
                }

                OnPropertyChanged(nameof(SelectedTab));
                OnPropertyChanged(nameof(HasTab));
                OnPropertyChanged(nameof(ShowPlaceholder));
            }
        }

        public bool HasTab => _selectedTab is not null;

        public bool ShowPlaceholder => _selectedTab is null;

        private string _searchText = String.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;

                OnPropertyChanged(nameof(SearchText));

                Rebuild();
            }
        }

        public string MyName { get; private set; } = String.Empty;

        public string MyHandle { get; private set; } = String.Empty;

        public string? MyAvatar => _myAvatar;

        public string ListStatus => _listStatus ?? String.Empty;

        public bool ShowListStatus => !String.IsNullOrEmpty(_listStatus);

        public ICommand OpenChatCommand => new RelayCommand<ChatFriend>(friend => _ = OpenChatAsync(friend));

        public ICommand SelectTabCommand => new RelayCommand<ChatTab>(tab => SelectedTab = tab);

        public ICommand CloseTabCommand => new RelayCommand<ChatTab>(CloseTab);

        public ICommand ToggleSectionCommand => new RelayCommand<FriendListHeader>(ToggleSection);

        public ICommand SendCommand => new RelayCommand(() => _ = SendAsync());

        public ICommand JoinCommand => new RelayCommand<ChatFriend>(Join);

        public FriendActivityViewModel(Integrations.Overlay? overlay)
        {
            _overlay = overlay;
            _party = overlay?.Messaging.Party;
            _presence = overlay?.Friends;

            _headers = new Dictionary<FriendSection, FriendListHeader>
            {
                [FriendSection.InGame] = new FriendListHeader { Title = Strings.Menu_Overlay_Messages_SectionInGame },
                [FriendSection.Online] = new FriendListHeader { Title = Strings.Menu_Overlay_Messages_SectionOnline },
                [FriendSection.Offline] = new FriendListHeader { Title = Strings.Menu_Overlay_Messages_SectionOffline },
                [FriendSection.Groups] = new FriendListHeader { Title = Strings.Menu_Overlay_Messages_SectionGroups }
            };

            if (_party is not null)
                _party.IncomingMessage += OnIncomingMessage;

            if (_presence is not null)
                _presence.Updated += (_, _) => App.Current.Dispatcher.InvokeAsync(ApplyPresence);
        }

        public async Task LoadAsync(bool force = false)
        {
            const string LOG_IDENT = "FriendActivityViewModel::LoadAsync";

            if (_loading || (_loaded && !force))
                return;

            if (!App.Settings.Prop.AllowCookieAccess)
            {
                SetListStatus(Strings.Menu_Overlay_Messages_NeedsCookies);
                return;
            }

            _loading = true;

            if (!_people.Any())
                SetListStatus(Strings.Menu_Overlay_Messages_Loading);

            try
            {
                await App.Cookies.EnsureLoadedAsync();

                _me = App.Cookies.CurrentUser;

                if (_me is null)
                {
                    SetListStatus(Strings.Menu_Overlay_Messages_NeedsCookies);
                    return;
                }

                _overlay?.WatchFriendsForPanel();

                if (_presence is not null && !_presence.HasLooked)
                    await _presence.PollAsync();

                List<Conversation> conversations = _party is null ? new() : await _party.GetAllConversations();

                var ids = new HashSet<long>(_presence?.FriendIds ?? Array.Empty<long>());

                foreach (Conversation conversation in conversations.Where(IsOneToOne))
                {
                    long other = conversation.Participants.FirstOrDefault(x => x != _me.Id);

                    if (other > 0)
                        ids.Add(other);
                }

                ids.Add(_me.Id);

                var details = await FetchDetailsAsync(ids.ToList());

                _myAvatar = details.TryGetValue(_me.Id, out UserDetails? mine) ? mine.Thumbnail?.ImageUrl : null;

                MyName = mine?.Data.DisplayName ?? mine?.Data.Name ?? String.Empty;
                MyHandle = String.IsNullOrEmpty(mine?.Data.Name) ? String.Empty : $"@{mine!.Data.Name}";

                OnPropertyChanged(nameof(MyName));
                OnPropertyChanged(nameof(MyHandle));
                OnPropertyChanged(nameof(MyAvatar));

                ids.Remove(_me.Id);

                foreach (long id in ids)
                {
                    if (_people.ContainsKey(id))
                        continue;

                    details.TryGetValue(id, out UserDetails? person);

                    _people[id] = new ChatFriend
                    {
                        UserId = id,
                        DisplayName = person?.Data.DisplayName ?? person?.Data.Name ?? id.ToString(),
                        Username = person?.Data.Name ?? String.Empty,
                        Headshot = person?.Thumbnail?.ImageUrl
                    };
                }

                _groups.Clear();

                foreach (Conversation conversation in conversations)
                {
                    if (IsOneToOne(conversation))
                    {
                        long other = conversation.Participants.FirstOrDefault(x => x != _me.Id);

                        if (_people.TryGetValue(other, out ChatFriend? friend))
                        {
                            friend.ConversationId = conversation.Id;
                            friend.Unread = conversation.UnreadMessagesCount;
                        }

                        continue;
                    }

                    _groups.Add(new ChatFriend
                    {
                        IsGroup = true,
                        DisplayName = String.IsNullOrWhiteSpace(conversation.Name) ? Strings.Menu_Overlay_Messages_UnnamedGroup : conversation.Name,
                        MemberCount = conversation.Participants.Length,
                        ConversationId = conversation.Id,
                        Unread = conversation.UnreadMessagesCount
                    });
                }

                _loaded = true;

                SetListStatus(null);

                ApplyPresence();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load friends and conversations");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (!_people.Any())
                    SetListStatus(Strings.Menu_Overlay_Messages_LoadFailed);
            }
            finally
            {
                _loading = false;
            }
        }

        private static bool IsOneToOne(Conversation conversation) =>
            conversation.Type == "one_to_one" || conversation.Participants.Length <= 2;

        private static async Task<Dictionary<long, UserDetails>> FetchDetailsAsync(List<long> ids)
        {
            var details = new Dictionary<long, UserDetails>();

            foreach (long[] batch in ids.Chunk(DetailsBatch))
            {
                foreach ((long id, UserDetails user) in await UserDetails.FetchBatch(batch.ToList()))
                    details[id] = user;
            }

            return details;
        }

        private void ApplyPresence()
        {
            IReadOnlyDictionary<long, UserPresence> presences = _presence?.Presences ?? new Dictionary<long, UserPresence>();

            foreach ((long id, ChatFriend friend) in _people)
                friend.Update(presences.TryGetValue(id, out UserPresence? presence) ? presence : null);

            Rebuild();
        }

        private void Rebuild()
        {
            string search = _searchText.Trim();

            bool Shown(ChatFriend friend) => search.Length == 0 || friend.Matches(search);

            var sections = new List<(FriendSection Section, List<ChatFriend> People)>
            {
                (FriendSection.InGame, _people.Values.Where(x => x.Status == FriendStatus.InGame && Shown(x)).ToList()),
                (FriendSection.Online, _people.Values.Where(x => (x.Status is FriendStatus.Online or FriendStatus.InStudio) && Shown(x)).ToList()),
                (FriendSection.Offline, _people.Values.Where(x => x.Status == FriendStatus.Offline && Shown(x)).ToList()),
                (FriendSection.Groups, _groups.Where(Shown).ToList())
            };

            var rows = new List<object>();

            foreach ((FriendSection section, List<ChatFriend> people) in sections)
            {
                if (!people.Any())
                    continue;

                FriendListHeader header = _headers[section];
                header.Count = people.Count;

                rows.Add(header);

                if (header.IsExpanded)
                    rows.AddRange(people.OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase));
            }

            if (!rows.SequenceEqual(Rows))
            {
                Rows.Clear();

                foreach (object row in rows)
                    Rows.Add(row);
            }

            if (_loaded)
            {
                if (!rows.Any() && search.Length > 0)
                    SetListStatus(String.Format(Strings.Menu_Overlay_Messages_NoMatches, search));
                else if (!rows.Any())
                    SetListStatus(Strings.Menu_Overlay_Messages_Empty);
                else
                    SetListStatus(null);
            }
        }

        private void ToggleSection(FriendListHeader? header)
        {
            if (header is null)
                return;

            header.IsExpanded = !header.IsExpanded;

            Rebuild();
        }

        private void SetListStatus(string? status)
        {
            _listStatus = status;

            OnPropertyChanged(nameof(ListStatus));
            OnPropertyChanged(nameof(ShowListStatus));
        }

        public async Task OpenChatAsync(ChatFriend? friend)
        {
            if (friend is null)
                return;

            ChatTab? tab = Tabs.FirstOrDefault(x => x.Friend == friend);

            if (tab is null)
            {
                tab = new ChatTab { Friend = friend };
                Tabs.Add(tab);
            }

            SelectedTab = tab;

            if (!tab.Loaded)
                await RefreshTabAsync(tab);
        }

        private void CloseTab(ChatTab? tab)
        {
            if (tab is null)
                return;

            int index = Tabs.IndexOf(tab);

            Tabs.Remove(tab);

            if (_selectedTab == tab)
                SelectedTab = Tabs.Count == 0 ? null : Tabs[Math.Clamp(index, 0, Tabs.Count - 1)];
        }

        private async Task RefreshTabAsync(ChatTab tab)
        {
            const string LOG_IDENT = "FriendActivityViewModel::RefreshTabAsync";

            if (_party is null || String.IsNullOrEmpty(tab.Friend.ConversationId))
            {
                tab.Loaded = true;
                tab.RowsChanged();
                return;
            }

            tab.IsLoading = !tab.Loaded;

            try
            {
                UserMessagesPage? page = await _party.GetMessages(new Conversation { Id = tab.Friend.ConversationId });

                if (page?.Messages is null)
                    return;

                bool added = false;

                foreach (UserMessage message in Enumerable.Reverse(page.Messages))
                {
                    if (String.IsNullOrEmpty(message.Id) || !tab.KnownMessages.Add(message.Id))
                        continue;

                    if (message.Visibility != "visible" && !String.IsNullOrEmpty(message.Visibility))
                        continue;

                    long sender = message.Sender ?? UserMessage.SystemSenderId;

                    if (sender == UserMessage.SystemSenderId)
                        AddSystemLine(tab, message.Content, message.CreatedAt);
                    else
                        AddLine(tab, sender, message.Content, message.CreatedAt);

                    added = true;
                }

                tab.Loaded = true;

                if (added)
                    MessagesAdded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load a conversation");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                tab.IsLoading = false;
                tab.RowsChanged();
            }
        }

        private void AddSystemLine(ChatTab tab, string text, DateTime? at)
        {
            AddDivider(tab, at);

            tab.Rows.Add(new ChatSystemLine(text));
        }

        public ChatLine AddLine(ChatTab tab, long sender, string text, DateTime? at)
        {
            bool mine = _me is not null && sender == _me.Id;

            AddDivider(tab, at);

            ChatMessageGroup? group = tab.Rows.LastOrDefault() as ChatMessageGroup;

            bool continues = group is not null && group.SenderId == sender
                && (at is null || group.LastAt is null || at.Value - group.LastAt.Value <= GroupGap);

            if (!continues)
            {
                ChatFriend? who = mine ? null : _people.TryGetValue(sender, out ChatFriend? friend) ? friend : null;

                group = new ChatMessageGroup
                {
                    SenderId = sender,
                    IsMine = mine,
                    Sender = mine ? Strings.Menu_Overlay_Messages_You : who?.DisplayName ?? tab.Friend.DisplayName,
                    Avatar = mine ? _myAvatar : who?.Headshot ?? (tab.Friend.IsGroup ? null : tab.Friend.Headshot),
                    StartedAt = at
                };

                tab.Rows.Add(group);
            }

            group!.LastAt = at ?? group.LastAt;

            var line = new ChatLine { Text = text };

            group.Lines.Add(line);

            tab.RowsChanged();

            return line;
        }

        private static void AddDivider(ChatTab tab, DateTime? at)
        {
            if (at is not DateTime when)
                return;

            DateTime day = when.ToLocalTime().Date;

            ChatDayDivider? last = tab.Rows.OfType<ChatDayDivider>().LastOrDefault();

            if (last is not null && last.Day == day)
                return;

            tab.Rows.Add(new ChatDayDivider(day, DayText(day)));
        }

        public static string DayText(DateTime day)
        {
            if (day == DateTime.Today)
                return Strings.Menu_Overlay_Messages_Today;

            if (day == DateTime.Today.AddDays(-1))
                return Strings.Menu_Overlay_Messages_Yesterday;

            return day.ToString("D", Locale.CurrentCulture);
        }

        public async Task SendAsync()
        {
            const string LOG_IDENT = "FriendActivityViewModel::SendAsync";

            ChatTab? tab = _selectedTab;

            if (tab is null || _party is null || _me is null || !tab.CanSend)
                return;

            string text = tab.Draft.Trim();

            tab.Draft = String.Empty;

            ChatLine line = AddLine(tab, _me.Id, text, DateTime.UtcNow);
            line.State = ChatLineState.Pending;

            MessagesAdded?.Invoke(this, EventArgs.Empty);

            try
            {
                if (String.IsNullOrEmpty(tab.Friend.ConversationId))
                {
                    Conversation? created = tab.Friend.IsGroup ? null : await _party.CreateConversation(tab.Friend.UserId);

                    if (created is null)
                    {
                        line.Failure = String.Format(Strings.Menu_Overlay_Messages_CouldntStart, tab.Friend.DisplayName);
                        line.State = ChatLineState.Failed;
                        return;
                    }

                    tab.Friend.ConversationId = created.Id;
                }

                UserMessage? sent = await _party.SendMessage(tab.Friend.ConversationId!, text);

                if (!String.IsNullOrEmpty(sent?.Id))
                    tab.KnownMessages.Add(sent.Id);

                line.State = ChatLineState.Sent;
            }
            catch (MessageModeratedException)
            {
                line.Failure = Strings.Menu_Overlay_Messages_Moderated;
                line.State = ChatLineState.Failed;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to send a message");
                App.Logger.WriteException(LOG_IDENT, ex);

                line.Failure = Strings.Menu_Overlay_Messages_NotSent;
                line.State = ChatLineState.Failed;
            }
        }

        private void OnIncomingMessage(object? sender, MessageEvent message)
        {
            if (message.IsTyping is not null)
                return;

            App.Current.Dispatcher.InvokeAsync(async () =>
            {
                ChatTab? tab = Tabs.FirstOrDefault(x => x.Friend.ConversationId == message.ConversationId);

                if (tab is not null)
                {
                    await RefreshTabAsync(tab);

                    if (tab != _selectedTab)
                        tab.Friend.Unread++;

                    return;
                }

                ChatFriend? owner = _people.Values.Concat(_groups).FirstOrDefault(x => x.ConversationId == message.ConversationId);

                if (owner is not null)
                    owner.Unread++;
                else
                    await LoadAsync(true);
            });
        }

        private void Join(ChatFriend? friend)
        {
            const string LOG_IDENT = "FriendActivityViewModel::Join";

            if (friend is null)
                return;

            try
            {
                if (friend.CanJoin)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Joining a friend from their chat");
                    GameServers.Join(friend.PlaceId, friend.ServerId!);
                }
                else if (friend.CanViewGame)
                {
                    long place = friend.RootPlaceId > 0 ? friend.RootPlaceId : friend.PlaceId;
                    _overlay?.OpenPage(GameServers.GamePage(place));
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to follow a friend");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
