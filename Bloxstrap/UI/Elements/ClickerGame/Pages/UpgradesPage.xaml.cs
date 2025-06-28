using System.ComponentModel;
using System.Windows;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.ClickerGame.Pages
{
    public partial class UpgradesPage : UiPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string DoubleClickPowerPriceFormatted => ClickerGameManager.FormatBigInteger(ClickerGameManager.Instance.DoubleClickPowerPrice);
        public string AutoClickerPriceFormatted => ClickerGameManager.FormatBigInteger(ClickerGameManager.Instance.AutoClickerPrice);
        public string BonusMultiplierPriceFormatted => ClickerGameManager.FormatBigInteger(ClickerGameManager.Instance.BonusMultiplierPrice);
        public string CriticalClickChancePriceFormatted => ClickerGameManager.FormatBigInteger(ClickerGameManager.Instance.CriticalClickChancePrice);
        public string CriticalClickMultiplierPriceFormatted => ClickerGameManager.FormatBigInteger(ClickerGameManager.Instance.CriticalClickMultiplierPrice);
        public string UpgradeDiscountPriceFormatted => ClickerGameManager.FormatBigInteger(ClickerGameManager.Instance.UpgradeDiscountPrice);

        public UpgradesPage()
        {
            InitializeComponent();
            DataContext = this;

            UpdateButtons();

            ClickerGameManager.PointsUpdated += UpdateButtons;
            ClickerGameManager.UpgradesUpdated += UpdateButtons;
            ClickerGameManager.UpgradesUpdated += OnUpgradesUpdated;
        }

        private void UpdateButtons()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(UpdateButtons);
                return;
            }

            var mgr = ClickerGameManager.Instance;

            // Double Click Power
            var dcp = mgr.ApplyDiscount(mgr.DoubleClickPowerPrice);
            DoubleClickPowerPriceText.Text = dcp.ToString("N0");
            DoubleClickPowerButton.IsEnabled = mgr.Points >= dcp;

            // Auto Clicker
            var acp = mgr.ApplyDiscount(mgr.AutoClickerPrice);
            AutoClickerPriceText.Text = mgr.AutoClickerEnabled ? "Bought" : acp.ToString("N0");
            AutoClickerButton.IsEnabled = mgr.Points >= acp && !mgr.AutoClickerEnabled;

            // Bonus Multiplier
            var bmp = mgr.ApplyDiscount(mgr.BonusMultiplierPrice);
            BonusMultiplierPriceText.Text = bmp.ToString("N0");
            BonusMultiplierButton.IsEnabled = mgr.Points >= bmp;

            // Critical Click Chance
            var ccp = mgr.ApplyDiscount(mgr.CriticalClickChancePrice);
            CriticalClickChancePriceText.Text = mgr.CriticalClickChancePercent >= 30 ? "Maxed" : ccp.ToString("N0");
            CriticalClickChanceButton.IsEnabled = mgr.Points >= ccp && mgr.CriticalClickChancePercent < 30;

            // Critical Click Multiplier
            var cmp = mgr.ApplyDiscount(mgr.CriticalClickMultiplierPrice);
            CriticalClickMultiplierPriceText.Text = cmp.ToString("N0");
            CriticalClickMultiplierButton.IsEnabled = mgr.Points >= cmp;

            // Upgrade Discount
            var udp = mgr.ApplyDiscount(mgr.UpgradeDiscountPrice);
            UpgradeDiscountPriceText.Text = mgr.UpgradeDiscountPercent >= 50 ? "Maxed" : udp.ToString("N0");
            UpgradeDiscountButton.IsEnabled = mgr.Points >= udp && mgr.UpgradeDiscountPercent < 50;
        }


        private void OnUpgradesUpdated()
        {
            OnPropertyChanged(nameof(DoubleClickPowerPriceFormatted));
            OnPropertyChanged(nameof(AutoClickerPriceFormatted));
            OnPropertyChanged(nameof(BonusMultiplierPriceFormatted));
            OnPropertyChanged(nameof(CriticalClickChancePriceFormatted));
            OnPropertyChanged(nameof(CriticalClickMultiplierPriceFormatted));
            OnPropertyChanged(nameof(UpgradeDiscountPriceFormatted));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn && btn.Tag is string tag)
            {
                bool success = tag switch
                {
                    "Upgrade1" => ClickerGameManager.Instance.TryPurchaseDoubleClickPower(),
                    "Upgrade2" => ClickerGameManager.Instance.TryPurchaseAutoClicker(),
                    "Upgrade3" => ClickerGameManager.Instance.TryPurchaseBonusMultiplier(),
                    "Upgrade4" => ClickerGameManager.Instance.TryPurchaseCriticalClickChance(),
                    "Upgrade5" => ClickerGameManager.Instance.TryPurchaseCriticalClickMultiplier(),
                    "Upgrade6" => ClickerGameManager.Instance.TryPurchaseUpgradeDiscount(),
                    _ => false
                };

                if (!success)
                {
                    Frontend.ShowMessageBox(
                        "Not enough points or upgrade already purchased.",
                        MessageBoxImage.Error,
                        MessageBoxButton.OK
                    );
                }
            }
        }
    }
}
