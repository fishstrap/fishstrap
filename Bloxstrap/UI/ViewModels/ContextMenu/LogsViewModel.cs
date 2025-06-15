using System;
using System.Collections.Generic;
using System.Windows.Input;
using Bloxstrap.Integrations;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.ContextMenu
{
    internal class LogsViewModel : NotifyPropertyChangedViewModel
    {
        private readonly ActivityWatcher _activityWatcher;

        public Dictionary<int, ActivityData.UserLog>? PlayerLogs { get; private set; }
        public IEnumerable<KeyValuePair<int, ActivityData.UserLog>>? PlayerLogsCollection => PlayerLogs;

        public Dictionary<int, ActivityData.UserMessage>? MessageLogs { get; private set; }
        public IEnumerable<KeyValuePair<int, ActivityData.UserMessage>>? MessageLogsCollection => MessageLogs;

        public GenericTriState LoadState { get; private set; } = GenericTriState.Unknown;
        public string Error { get; private set; } = string.Empty;

        public ICommand CloseWindowCommand => new RelayCommand(RequestClose);
        public EventHandler? RequestCloseEvent;

        public LogsViewModel(ActivityWatcher watcher)
        {
            _activityWatcher = watcher;

            _activityWatcher.OnNewPlayerRequest += (_, _) => LoadData();
            _activityWatcher.OnNewMessageRequest += (_, _) => LoadData();

            LoadData();
        }

        private void LoadData()
        {
            LoadState = GenericTriState.Unknown;
            OnPropertyChanged(nameof(LoadState));

            try
            {
                PlayerLogs = new Dictionary<int, ActivityData.UserLog>(_activityWatcher.PlayerLogs);
                MessageLogs = new Dictionary<int, ActivityData.UserMessage>(_activityWatcher.MessageLogs);
                Error = string.Empty;

                LoadState = GenericTriState.Successful;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                LoadState = GenericTriState.Failed;
            }

            OnPropertyChanged(nameof(PlayerLogsCollection));
            OnPropertyChanged(nameof(MessageLogsCollection));
            OnPropertyChanged(nameof(Error));
            OnPropertyChanged(nameof(LoadState));
        }

        private void RequestClose() => RequestCloseEvent?.Invoke(this, EventArgs.Empty);
    }
}
