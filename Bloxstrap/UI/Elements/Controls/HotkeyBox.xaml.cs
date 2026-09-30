using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Wpf.Ui.Common;

namespace Bloxstrap.UI.Elements.Controls
{
    public partial class HotkeyBox : UserControl
    {
        public static readonly DependencyProperty ModifiersProperty = DependencyProperty.Register(
            nameof(Modifiers), typeof(ModifierKeys), typeof(HotkeyBox),
            new FrameworkPropertyMetadata(ModifierKeys.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HotkeyBox)d).ShowShortcut()));

        public ModifierKeys Modifiers
        {
            get => (ModifierKeys)GetValue(ModifiersProperty);
            set => SetValue(ModifiersProperty, value);
        }

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(Key), typeof(HotkeyBox),
            new FrameworkPropertyMetadata(Key.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HotkeyBox)d).ShowShortcut()));

        public Key Key
        {
            get => (Key)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        private bool _recording;

        public bool IsRecording => _recording;

        public event EventHandler? RecordingChanged;

        public HotkeyBox()
        {
            InitializeComponent();

            ShowShortcut();
        }

        private void RecorderClicked(object sender, RoutedEventArgs e)
        {
            if (_recording)
            {
                StopRecording();
                return;
            }

            _recording = true;

            Recorder.Appearance = ControlAppearance.Primary;
            ShortcutText.Text = Strings.Menu_Integrations_OverlayHotkey_Recording;

            Recorder.Focus();

            RecordingChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RecorderKeyDown(object sender, KeyEventArgs e)
        {
            if (!_recording)
                return;

            e.Handled = true;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys modifiers = Keyboard.Modifiers;

            if (key == Key.Escape)
            {
                StopRecording();
                return;
            }

            if (OverlayHotkey.IsModifier(key))
            {
                ShortcutText.Text = $"{OverlayHotkey.Describe(modifiers, Key.None)} + …";
                return;
            }

            if (!OverlayHotkey.IsAllowed(modifiers, key))
            {
                ShortcutText.Text = Strings.Menu_Integrations_OverlayHotkey_NeedsModifier;
                return;
            }

            Modifiers = modifiers;
            Key = key;

            StopRecording();
        }

        private void RecorderKeyUp(object sender, KeyEventArgs e)
        {
            if (_recording)
                e.Handled = true;
        }

        private void RecorderLostFocus(object sender, KeyboardFocusChangedEventArgs e) => StopRecording();

        private void StopRecording()
        {
            bool wasRecording = _recording;

            _recording = false;

            Recorder.Appearance = ControlAppearance.Secondary;

            ShowShortcut();

            if (wasRecording)
                RecordingChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ShowShortcut()
        {
            if (_recording)
                return;

            ShortcutText.Text = OverlayHotkey.Describe(Modifiers, Key);
        }
    }
}
