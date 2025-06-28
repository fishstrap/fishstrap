using System;
using System.Numerics;
using System.Diagnostics;

namespace Bloxstrap
{
    public class ClickerGameManager
    {
        public static ClickerGameManager Instance { get; } = new();

        public BigInteger Points { get; private set; } = BigInteger.Zero;
        public BigInteger PointsPerClick { get; private set; } = BigInteger.One;
        public bool AutoClickerEnabled { get; private set; } = false;
        public decimal BonusMultiplier { get; private set; } = 1.0m;

        public BigInteger DoubleClickPowerPrice { get; private set; } = new BigInteger(50);
        public BigInteger AutoClickerPrice { get; private set; } = new BigInteger(1000);
        public BigInteger BonusMultiplierPrice { get; private set; } = new BigInteger(500);

        public BigInteger CriticalClickChancePrice { get; private set; } = new BigInteger(4000);
        public BigInteger CriticalClickMultiplierPrice { get; private set; } = new BigInteger(8000);
        public BigInteger UpgradeDiscountPrice { get; private set; } = new BigInteger(500);

        public int BonusMultiplierLevel { get; private set; } = 0;
        public int CriticalClickChancePercent { get; private set; } = 0;
        public int CriticalClickMultiplier { get; private set; } = 2;
        public int UpgradeDiscountPercent { get; private set; } = 0;

        private System.Timers.Timer? _autoClickerTimer;

        private BigInteger _totalPointsSpent = BigInteger.Zero;
        private BigInteger _totalPointsEarned = BigInteger.Zero;
        private Stopwatch _playtimeStopwatch = new();

        private long _savedPlaytimeTicks = 0;

        public TimeSpan TotalPlaytime => TimeSpan.FromTicks(_savedPlaytimeTicks) + _playtimeStopwatch.Elapsed;

        public BigInteger TotalPointsSpent => _totalPointsSpent;
        public BigInteger TotalPointsEarned => _totalPointsEarned;

        public static event Action? PointsUpdated;
        public static event Action? UpgradesUpdated;

        private ClickerGameManager()
        {
            LoadFromSettings();
            _playtimeStopwatch.Start();
        }

        public static string FormatBigInteger(BigInteger value) => value.ToString("N0");

        public static string FormatTimeSpan(TimeSpan ts) => $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";

        public BigInteger PointsPerClickEffective => new BigInteger((decimal)PointsPerClick * BonusMultiplier);

        public void RaisePointsUpdated() => PointsUpdated?.Invoke();

        public void LoadFromSettings()
        {
            var s = App.Settings.Prop;

            Points = BigInteger.TryParse(s.Points, out var p) ? p : BigInteger.Zero;
            PointsPerClick = BigInteger.TryParse(s.PointsPerClick, out var ppc) ? ppc : BigInteger.One;
            AutoClickerEnabled = s.AutoClickerEnabled;
            BonusMultiplier = s.BonusMultiplier;
            BonusMultiplierLevel = s.BonusMultiplierLevel;
            CriticalClickChancePercent = s.CriticalClickChancePercent;
            CriticalClickMultiplier = s.CriticalClickMultiplier;
            UpgradeDiscountPercent = s.UpgradeDiscountPercent;
            _totalPointsSpent = BigInteger.TryParse(s.TotalPointsSpent, out var tps) ? tps : BigInteger.Zero;
            _totalPointsEarned = BigInteger.TryParse(s.TotalPointsEarned, out var tpe) ? tpe : BigInteger.Zero;

            _savedPlaytimeTicks = s.TotalPlaytimeTicks;

            DoubleClickPowerPrice = BigInteger.TryParse(s.DoubleClickPowerPrice, out var dpp) ? dpp : new BigInteger(50);
            AutoClickerPrice = BigInteger.TryParse(s.AutoClickerPrice, out var acp) ? acp : new BigInteger(1000);
            BonusMultiplierPrice = BigInteger.TryParse(s.BonusMultiplierPrice, out var bmp) ? bmp : new BigInteger(500);
            CriticalClickChancePrice = BigInteger.TryParse(s.CriticalClickChancePrice, out var cccp) ? cccp : new BigInteger(4000);
            CriticalClickMultiplierPrice = BigInteger.TryParse(s.CriticalClickMultiplierPrice, out var ccmp) ? ccmp : new BigInteger(8000);
            UpgradeDiscountPrice = BigInteger.TryParse(s.UpgradeDiscountPrice, out var udp) ? udp : new BigInteger(500);

            _playtimeStopwatch.Reset();
            _playtimeStopwatch.Start();
        }

        public void SaveToSettings()
        {
            var s = App.Settings.Prop;

            s.Points = Points.ToString();
            s.PointsPerClick = PointsPerClick.ToString();
            s.AutoClickerEnabled = AutoClickerEnabled;
            s.BonusMultiplier = BonusMultiplier;
            s.BonusMultiplierLevel = BonusMultiplierLevel;
            s.CriticalClickChancePercent = CriticalClickChancePercent;
            s.CriticalClickMultiplier = CriticalClickMultiplier;
            s.UpgradeDiscountPercent = UpgradeDiscountPercent;
            s.TotalPointsSpent = _totalPointsSpent.ToString();
            s.TotalPointsEarned = _totalPointsEarned.ToString();
            s.TotalPlaytimeTicks = _savedPlaytimeTicks + _playtimeStopwatch.Elapsed.Ticks;

            s.DoubleClickPowerPrice = DoubleClickPowerPrice.ToString();
            s.AutoClickerPrice = AutoClickerPrice.ToString();
            s.BonusMultiplierPrice = BonusMultiplierPrice.ToString();
            s.CriticalClickChancePrice = CriticalClickChancePrice.ToString();
            s.CriticalClickMultiplierPrice = CriticalClickMultiplierPrice.ToString();
            s.UpgradeDiscountPrice = UpgradeDiscountPrice.ToString();

            App.Settings.Save();
        }

        public void ResetProgress()
        {
            Points = BigInteger.Zero;
            PointsPerClick = BigInteger.One;
            AutoClickerEnabled = false;
            BonusMultiplier = 1.0m;
            BonusMultiplierLevel = 0;
            CriticalClickChancePercent = 0;
            CriticalClickMultiplier = 2;
            UpgradeDiscountPercent = 0;

            DoubleClickPowerPrice = new BigInteger(50);
            AutoClickerPrice = new BigInteger(1000);
            BonusMultiplierPrice = new BigInteger(500);
            CriticalClickChancePrice = new BigInteger(4000);
            CriticalClickMultiplierPrice = new BigInteger(8000);
            UpgradeDiscountPrice = new BigInteger(500);

            _totalPointsSpent = BigInteger.Zero;
            _totalPointsEarned = BigInteger.Zero;

            _savedPlaytimeTicks = 0;
            _playtimeStopwatch.Reset();
            _playtimeStopwatch.Start();

            SaveToSettings();
            RaisePointsUpdated();
            UpgradesUpdated?.Invoke();
        }

        public void AddPoints(BigInteger amount)
        {
            Points += amount;
            _totalPointsEarned += amount;
            RaisePointsUpdated();
            SaveToSettings();
        }

        private bool TrySpendPoints(BigInteger amount)
        {
            if (Points < amount)
                return false;

            Points -= amount;
            _totalPointsSpent += amount;
            return true;
        }

        public void Click()
        {
            decimal pointsToAddDecimal = (decimal)PointsPerClick * BonusMultiplier;
            var pointsToAdd = new BigInteger(pointsToAddDecimal);

            AddPoints(pointsToAdd);
        }

        public bool IsAutoclickerEnabled => AutoClickerEnabled;

        public BigInteger ApplyDiscount(BigInteger price)
        {
            if (UpgradeDiscountPercent <= 0)
                return price;

            try
            {
                decimal priceDecimal = (decimal)price;
                decimal discountFactor = (100m - UpgradeDiscountPercent) / 100m;
                decimal discountedPriceDecimal = priceDecimal * discountFactor;
                return new BigInteger(discountedPriceDecimal);
            }
            catch (OverflowException)
            {
                return price;
            }
        }

        public bool TryPurchaseDoubleClickPower()
        {
            var price = ApplyDiscount(DoubleClickPowerPrice);
            if (!TrySpendPoints(price))
                return false;

            PointsPerClick *= 2;
            DoubleClickPowerPrice *= 2;

            RaisePointsUpdated();
            UpgradesUpdated?.Invoke();
            SaveToSettings();
            return true;
        }

        public bool TryPurchaseAutoClicker()
        {
            var price = ApplyDiscount(AutoClickerPrice);
            if (!TrySpendPoints(price) || AutoClickerEnabled)
                return false;

            AutoClickerEnabled = true;

            _autoClickerTimer?.Stop();
            _autoClickerTimer?.Dispose();

            _autoClickerTimer = new System.Timers.Timer(1000);
            _autoClickerTimer.Elapsed += (_, _) => Click();
            _autoClickerTimer.Start();

            RaisePointsUpdated();
            UpgradesUpdated?.Invoke();
            SaveToSettings();
            return true;
        }

        public bool TryPurchaseBonusMultiplier()
        {
            var price = ApplyDiscount(BonusMultiplierPrice);
            if (!TrySpendPoints(price))
                return false;

            BonusMultiplier += 0.25m;
            BonusMultiplierPrice *= 2;
            BonusMultiplierLevel += 1;

            RaisePointsUpdated();
            UpgradesUpdated?.Invoke();
            SaveToSettings();
            return true;
        }

        public bool TryPurchaseCriticalClickChance()
        {
            var price = ApplyDiscount(CriticalClickChancePrice);
            if (!TrySpendPoints(price) || CriticalClickChancePercent >= 30)
                return false;

            CriticalClickChancePercent += 2;
            if (CriticalClickChancePercent > 30)
                CriticalClickChancePercent = 30;

            CriticalClickChancePrice *= 2;

            RaisePointsUpdated();
            UpgradesUpdated?.Invoke();
            SaveToSettings();
            return true;
        }

        public bool TryPurchaseCriticalClickMultiplier()
        {
            var price = ApplyDiscount(CriticalClickMultiplierPrice);
            if (!TrySpendPoints(price))
                return false;

            CriticalClickMultiplier += 1;
            CriticalClickMultiplierPrice *= 2;

            RaisePointsUpdated();
            UpgradesUpdated?.Invoke();
            SaveToSettings();
            return true;
        }

        public bool TryPurchaseUpgradeDiscount()
        {
            var price = ApplyDiscount(UpgradeDiscountPrice);
            if (!TrySpendPoints(price))
                return false;

            UpgradeDiscountPercent += 5;
            if (UpgradeDiscountPercent > 50)
                UpgradeDiscountPercent = 50;

            UpgradeDiscountPrice *= 2;

            RaisePointsUpdated();
            UpgradesUpdated?.Invoke();
            SaveToSettings();
            return true;
        }
    }
}