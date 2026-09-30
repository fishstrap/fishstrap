using System.Windows.Input;

using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Bloxstrap.Models.Overlay
{
    public static class OverlayHotkey
    {
        public const ModifierKeys DefaultModifiers = ModifierKeys.Control | ModifierKeys.Alt;

        public const Key DefaultKey = Key.L;

        public static bool IsModifier(Key key) => key is
            Key.LeftCtrl or Key.RightCtrl or
            Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or
            Key.LWin or Key.RWin or
            Key.System;

        public static bool IsAllowed(ModifierKeys modifiers, Key key)
        {
            if (key is Key.None or Key.Escape or Key.ImeProcessed or Key.DeadCharProcessed || IsModifier(key))
                return false;

            if (key >= Key.F1 && key <= Key.F24)
                return true;

            if (IsTypingKey(key))
                return (modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0;

            return modifiers != ModifierKeys.None;
        }

        public static string Describe(ModifierKeys modifiers, Key key)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");

            if (modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");

            if (modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");

            if (modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            if (key != Key.None)
                parts.Add(KeyName(key));

            return String.Join(" + ", parts);
        }

        internal static HOT_KEY_MODIFIERS ToNative(ModifierKeys modifiers)
        {
            HOT_KEY_MODIFIERS native = HOT_KEY_MODIFIERS.MOD_NOREPEAT;

            if (modifiers.HasFlag(ModifierKeys.Control))
                native |= HOT_KEY_MODIFIERS.MOD_CONTROL;

            if (modifiers.HasFlag(ModifierKeys.Alt))
                native |= HOT_KEY_MODIFIERS.MOD_ALT;

            if (modifiers.HasFlag(ModifierKeys.Shift))
                native |= HOT_KEY_MODIFIERS.MOD_SHIFT;

            if (modifiers.HasFlag(ModifierKeys.Windows))
                native |= HOT_KEY_MODIFIERS.MOD_WIN;

            return native;
        }

        private static bool IsTypingKey(Key key) =>
            (key >= Key.A && key <= Key.Z)
            || (key >= Key.D0 && key <= Key.D9)
            || (key >= Key.NumPad0 && key <= Key.Divide)
            || (key >= Key.Oem1 && key <= Key.OemBackslash)
            || key == Key.Space;

        private static string KeyName(Key key) => key switch
        {
            >= Key.D0 and <= Key.D9 => ((int)(key - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => $"Num {(int)(key - Key.NumPad0)}",
            Key.OemTilde => "`",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            Key.Enter => "Enter",
            Key.Back => "Backspace",
            Key.PageUp => "Page Up",
            Key.PageDown => "Page Down",
            Key.CapsLock => "Caps Lock",
            Key.PrintScreen => "Print Screen",
            Key.Scroll => "Scroll Lock",
            _ => key.ToString()
        };
    }
}
