using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace FolderUIGram
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            CheckLoginAndNavigate();
        }

        private async void CheckLoginAndNavigate()
        {
            // Delay slightly to ensure shell is layouted and ready for navigation commands
            await Task.Delay(200);

            // Connect using loaded settings if possible
            var result = await TelegramService.Instance.InitializeClientAndConnectAsync();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Always navigate to settings page first as requested
                await GoToAsync("///SettingsPage");
            });
        }
    }
}
