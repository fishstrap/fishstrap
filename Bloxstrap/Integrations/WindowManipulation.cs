using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Windows.Forms;
using System.Drawing;
using Windows.Win32.UI.Accessibility;

namespace Bloxstrap.Integrations
{
    public class WindowManipulation
    {
        private WINEVENTPROC? _setTitleHook;

        private HWND _hWnd;
        private uint _robloxPID;

        public WindowManipulation(long windowHandle, long robloxProcessId)
        {
            const string LOG_IDENT = "WindowManipulation";

            App.Logger.WriteLine(LOG_IDENT, $"Got window handle as {windowHandle}");
            _hWnd = (HWND)(IntPtr)windowHandle; // amazing
            _robloxPID = (uint)robloxProcessId;
        }

        public static bool WindowManipulationAvailable => App.Settings.Prop.EnableActivityTracking;

        public void FakeBorderless()
        {
            const string LOG_IDENT = "WindowManipulation::BorderlessFullscreen";
            App.Logger.WriteLine(LOG_IDENT, "Setting Roblox to borderless fullscreen");

            const int GWLSTYLE = -16;

            int style = PInvoke.GetWindowLong(_hWnd, (WINDOW_LONG_PTR_INDEX)GWLSTYLE);

            const int WS_CAPTION = 0x00C00000;
            const int WS_THICKFRAME = 0x00040000;
            const int WS_MINIMIZEBOX = 0x00020000;
            const int WS_MAXIMIZEBOX = 0x00010000;
            const int WS_SYSMENU = 0x00080000;

            style &= ~WS_CAPTION;
            style &= ~WS_THICKFRAME;
            style &= ~WS_MINIMIZEBOX;
            style &= ~WS_MAXIMIZEBOX;
            style &= ~WS_SYSMENU;

            Rectangle resolution = Screen.PrimaryScreen.Bounds;

            PInvoke.SetWindowLong((HWND)_hWnd, (WINDOW_LONG_PTR_INDEX)GWLSTYLE, style);

            // hack or else it'll still be exclusive
            PInvoke.SetWindowPos((HWND)_hWnd, (HWND)IntPtr.Zero, 0, 0, resolution.Width, resolution.Height + 1, SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        }

        public void ApplyWindowModifications()
        {
            const string LOG_IDENT = "WindowManipulation::ApplyWindowModifications";
            const int WINEVENT_OUTOFCONTEXT = 0x0;
            const int EVENT_OBJECT_NAMECHANGE = 0x800C;
            const int WM_SETICON = 0x0080;

            App.Logger.WriteLine(LOG_IDENT, "Applying window modifications");

            _setTitleHook = new(SetWindowTitleHook);

            // icon
            App.Logger.WriteLine(LOG_IDENT, "Setting Roblox icon");
            RobloxIcon robloxIcon = App.Settings.Prop.RobloxIcon;
            if (robloxIcon != RobloxIcon.IconDefault)
                using (var icon = robloxIcon.GetIcon())
                {
                    IntPtr hIconCopy = PInvoke.CopyIcon((HICON)icon.Handle); // copy the icon so its under Roblox
                    PInvoke.SendMessage(_hWnd, WM_SETICON, 0, hIconCopy);
                }


            // title
            App.Logger.WriteLine(LOG_IDENT, "Setting Roblox title");
            string robloxTitle = App.Settings.Prop.RobloxTitle;
            if (robloxTitle != "Roblox")
            {
                PInvoke.SetWindowText(_hWnd, robloxTitle);

                // because (Internal) exists Roblox will reset the title after couple of seconds
                App.Current.Dispatcher.Invoke(() => PInvoke.SetWinEventHook(EVENT_OBJECT_NAMECHANGE, EVENT_OBJECT_NAMECHANGE, null, _setTitleHook, _robloxPID, 0, WINEVENT_OUTOFCONTEXT));
            }
        }

        private void SetWindowTitleHook(HWINEVENTHOOK hWinEventHook, uint iEvent, HWND hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            const string LOG_IDENT = "WindowManipulation::SetWindowTitleHook";
            string robloxTitle = App.Settings.Prop.RobloxTitle;
            string newRobloxTitle = robloxTitle;

            Span<char> titleBuffer = new char[256];
            PInvoke.GetWindowText(_hWnd, titleBuffer);

            newRobloxTitle = titleBuffer.TrimEnd('\0').ToString();

            if (newRobloxTitle != robloxTitle)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Setting Roblox title back to {robloxTitle}");
                PInvoke.SetWindowText(_hWnd, robloxTitle);
            }
        }
    }
}
