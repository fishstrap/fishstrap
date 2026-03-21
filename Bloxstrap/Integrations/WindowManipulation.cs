using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Bloxstrap.Integrations
{
    public class WindowManipulation
    {
        private HWND hWnd;

        public WindowManipulation(long windowHandle)
        {
            const string LOG_IDENT = "WindowManipulation";

            App.Logger.WriteLine(LOG_IDENT, $"Got window handle as {windowHandle}");
            hWnd = (HWND)(IntPtr)windowHandle; // amazing
        }

        public void FakeBorderless()
        {
            const string LOG_IDENT = "WindowManipulation::BorderlessFullscreen";
            App.Logger.WriteLine(LOG_IDENT, "Setting Roblox to borderless fullscreen");

            const int GWLSTYLE = -16;

            int style = PInvoke.GetWindowLong(hWnd, (WINDOW_LONG_PTR_INDEX)GWLSTYLE);

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

            PInvoke.SetWindowLong((HWND)hWnd, (WINDOW_LONG_PTR_INDEX)GWLSTYLE, style);

            // hack or else it'll still be exclusive
            PInvoke.SetWindowPos((HWND)hWnd, (HWND)IntPtr.Zero, 0, 0, resolution.Width, resolution.Height + 1, SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        }

        public void ApplyWindowModifications()
        {
            const string LOG_IDENT = "WindowManipulation::ApplyWindowModifications";
            const int WM_SETICON = 0x0080;

            App.Logger.WriteLine(LOG_IDENT, "Applying window modifications");


            // icon
            App.Logger.WriteLine(LOG_IDENT, "Setting Roblox icon");
            RobloxIcon robloxIcon = App.Settings.Prop.RobloxIcon;
            if (robloxIcon != RobloxIcon.IconDefault)
                using (var icon = robloxIcon.GetIcon())
                {
                    IntPtr hIconCopy = PInvoke.CopyIcon((HICON)icon.Handle); // copy the icon so its under Roblox
                    PInvoke.SendMessage(hWnd, WM_SETICON, 0, hIconCopy);
                }


            // title
            App.Logger.WriteLine(LOG_IDENT, "Setting Roblox title");
            string robloxTitle = App.Settings.Prop.RobloxTitle;
            if (robloxTitle != "Roblox")
                // really hacky
                // because (Internal) exists Roblox will reset the title after couple of seconds
                Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        PInvoke.SetWindowText(hWnd, robloxTitle);
                        await Task.Delay(1000);
                    }
                });
        }
    }
}
