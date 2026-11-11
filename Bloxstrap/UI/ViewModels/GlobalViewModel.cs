using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels
{
    public static class GlobalViewModel
    {
        public static ICommand OpenWebpageCommand => new RelayCommand<string>(OpenWebpage);

        private static void OpenWebpage(string? location)
        {
            if (location is null)
                return;

            if (!Uri.TryCreate(location, UriKind.Absolute, out Uri? uri))
                return;

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                return;

            Utilities.ShellExecute(location);
        }
    }
}
