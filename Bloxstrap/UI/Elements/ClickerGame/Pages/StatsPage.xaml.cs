using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.ClickerGame.Pages
{
    public partial class StatsPage : UiPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ClickerGameManager Manager => ClickerGameManager.Instance;

        private readonly DispatcherTimer _playtimeTimer;

        public string TotalPointsFormatted => $"Total Points: {Manager.Points:N0}";
        public string PointsPerClickFormatted => $"Points Per Click: {Manager.PointsPerClick:N0}";
        public string BonusMultiplierFormatted => $"Bonus Multiplier: {Manager.BonusMultiplier:F2}x";
        public string CriticalClickChanceFormatted => $"Critical Click Chance: {Manager.CriticalClickChancePercent}%";
        public string CriticalClickMultiplierFormatted => $"Critical Click Multiplier: {Manager.CriticalClickMultiplier}x";
        public string UpgradeDiscountFormatted => $"Upgrade Discount: {Manager.UpgradeDiscountPercent}%";
        public string TotalPointsEarnedFormatted => $"Total Points Earned: {ClickerGameManager.FormatBigInteger(Manager.TotalPointsEarned)}";
        public string TotalPointsSpentFormatted => $"Total Points Spent: {ClickerGameManager.FormatBigInteger(Manager.TotalPointsSpent)}";

        public string TotalPlaytimeFormatted => $"Total Playtime: {ClickerGameManager.FormatTimeSpan(Manager.TotalPlaytime)}";

        public StatsPage()
        {
            InitializeComponent();

            DataContext = this;

            ClickerGameManager.PointsUpdated += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(TotalPointsFormatted));
                    OnPropertyChanged(nameof(PointsPerClickFormatted));
                    OnPropertyChanged(nameof(BonusMultiplierFormatted));
                    OnPropertyChanged(nameof(CriticalClickChanceFormatted));
                    OnPropertyChanged(nameof(CriticalClickMultiplierFormatted));
                    OnPropertyChanged(nameof(UpgradeDiscountFormatted));
                    OnPropertyChanged(nameof(TotalPointsEarnedFormatted));
                    OnPropertyChanged(nameof(TotalPointsSpentFormatted));
                });
            };

            _playtimeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _playtimeTimer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(TotalPlaytimeFormatted));
            };
            _playtimeTimer.Start();
        }

        private void ResetProgress_Click(object sender, RoutedEventArgs e)
        {
            var result = Frontend.ShowMessageBox(
                "Are you sure you want to permanently delete all your clicker progress?",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            ClickerGameManager.Instance.ResetProgress();

            // Refresh stats
            OnPropertyChanged(nameof(TotalPointsFormatted));
            OnPropertyChanged(nameof(PointsPerClickFormatted));
            OnPropertyChanged(nameof(BonusMultiplierFormatted));
            OnPropertyChanged(nameof(CriticalClickChanceFormatted));
            OnPropertyChanged(nameof(CriticalClickMultiplierFormatted));
            OnPropertyChanged(nameof(UpgradeDiscountFormatted));
            OnPropertyChanged(nameof(TotalPointsEarnedFormatted));
            OnPropertyChanged(nameof(TotalPointsSpentFormatted));
            OnPropertyChanged(nameof(TotalPlaytimeFormatted));

            Frontend.ShowMessageBox(
                "Your clicker progress has been successfully reset.",
                MessageBoxImage.Information,
                MessageBoxButton.OK
            );
        }

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}