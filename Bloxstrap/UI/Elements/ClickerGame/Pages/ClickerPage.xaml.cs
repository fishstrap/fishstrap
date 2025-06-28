using System;
using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.ClickerGame.Pages
{
    public partial class ClickerPage : UiPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string AutoclickerStatus => ClickerGameManager.Instance.IsAutoclickerEnabled ? "Enabled" : "Disabled";

        public BigInteger PointsPerClick => ClickerGameManager.Instance.PointsPerClick;
        public BigInteger EffectivePointsPerClick => ClickerGameManager.Instance.PointsPerClickEffective;

        public string PointsFormatted => ClickerGameManager.Instance.Points.ToString("N0");
        public string PointsPerClickFormatted => EffectivePointsPerClick.ToString("N0");

        private static readonly Random _rng = new();

        public ClickerPage()
        {
            InitializeComponent();

            DataContext = this;

            ClickerGameManager.PointsUpdated += UpdatePointsDisplay;
            ClickerGameManager.PointsUpdated += UpdateStats;

            UpdatePointsDisplay();
            UpdateStats();
        }

        private void ClickButton_Click(object sender, RoutedEventArgs e)
        {
            var manager = ClickerGameManager.Instance;

            bool isCritical = _rng.Next(100) < manager.CriticalClickChancePercent;
            BigInteger basePoints = manager.PointsPerClickEffective;
            BigInteger pointsEarned = isCritical ? basePoints * manager.CriticalClickMultiplier : basePoints;

            manager.AddPoints(pointsEarned);

            ShowClickIndicator(pointsEarned.ToString("N0"), isCritical);
        }

        private void ShowClickIndicator(string text, bool isCritical)
        {
            TextBlock indicator = new()
            {
                Text = $"+{text}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = isCritical ? Brushes.Gold : Brushes.White,
                Opacity = 1,
            };

            double baseX = ClickButton.TranslatePoint(new Point(0, 0), ClickIndicatorCanvas).X;
            double baseY = ClickButton.TranslatePoint(new Point(0, 0), ClickIndicatorCanvas).Y;

            double randomOffsetX = _rng.NextDouble() * ClickButton.ActualWidth - 30;
            double randomOffsetY = _rng.NextDouble() * ClickButton.ActualHeight - 60;

            Canvas.SetLeft(indicator, baseX + randomOffsetX);
            Canvas.SetTop(indicator, baseY + randomOffsetY);
            ClickIndicatorCanvas.Children.Add(indicator);

            DoubleAnimation moveUp = new()
            {
                From = Canvas.GetTop(indicator),
                To = Canvas.GetTop(indicator) - 50,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation fadeOut = new()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1)
            };

            Storyboard sb = new();
            sb.Children.Add(moveUp);
            sb.Children.Add(fadeOut);

            Storyboard.SetTarget(moveUp, indicator);
            Storyboard.SetTargetProperty(moveUp, new PropertyPath("(Canvas.Top)"));

            Storyboard.SetTarget(fadeOut, indicator);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

            sb.Completed += (_, _) => ClickIndicatorCanvas.Children.Remove(indicator);
            sb.Begin();
        }

        private void UpdatePointsDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                PointsText.Text = $"Points: {PointsFormatted}";
            });
        }

        private void UpdateStats()
        {
            OnPropertyChanged(nameof(AutoclickerStatus));
            OnPropertyChanged(nameof(PointsPerClick));
            OnPropertyChanged(nameof(PointsPerClickFormatted));
            OnPropertyChanged(nameof(PointsFormatted));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}