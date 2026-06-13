using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace FolderUIGram
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadConfig();
            await UpdateStatusUIAsync();
            UpdateFFmpegUI();
        }

        private void LoadConfig()
        {
            var cfg = TelegramService.Instance.Config;
            if (cfg != null)
            {
                ApiIdEntry.Text = cfg.ApiId ?? string.Empty;
                ApiHashEntry.Text = cfg.ApiHash ?? string.Empty;
                PhoneNumberEntry.Text = cfg.PhoneNumber ?? string.Empty;
            }
        }

        private async Task UpdateStatusUIAsync()
        {
            bool loggedIn = TelegramService.Instance.IsLoggedIn;

            if (loggedIn)
            {
                var user = TelegramService.Instance.Client?.User;
                StatusIndicator.Color = Colors.Green;
                StatusLabel.Text = "Connected";
                UserDetailLabel.Text = $"Logged in as: {user?.first_name} {user?.last_name} (@{user?.username})";
                LogoutButton.IsVisible = true;
                CredentialsCard.IsVisible = false;
                CodePanel.IsVisible = false;
                TwoFactorPanel.IsVisible = false;
            }
            else
            {
                StatusIndicator.Color = Colors.Red;
                StatusLabel.Text = "Disconnected";
                LogoutButton.IsVisible = false;

                var lastResult = TelegramService.Instance.LastConnectionResult;
                if (lastResult == "verification_code")
                {
                    UserDetailLabel.Text = "Verification code requested by Telegram.";
                    CredentialsCard.IsVisible = false;
                    CodePanel.IsVisible = true;
                    TwoFactorPanel.IsVisible = false;
                }
                else if (lastResult == "password")
                {
                    UserDetailLabel.Text = "2FA password required by Telegram.";
                    CredentialsCard.IsVisible = false;
                    CodePanel.IsVisible = false;
                    TwoFactorPanel.IsVisible = true;
                }
                else
                {
                    UserDetailLabel.Text = "Please enter API configuration and sign in.";
                    CredentialsCard.IsVisible = true;
                    CodePanel.IsVisible = false;
                    TwoFactorPanel.IsVisible = false;
                }
            }
        }

        private async void LoginButton_Clicked(object sender, EventArgs e)
        {
            string apiId = ApiIdEntry.Text?.Trim() ?? string.Empty;
            string apiHash = ApiHashEntry.Text?.Trim() ?? string.Empty;
            string phoneNumber = PhoneNumberEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(apiId) || string.IsNullOrEmpty(apiHash) || string.IsNullOrEmpty(phoneNumber))
            {
                await DisplayAlertAsync("Error", "Please fill in all configuration fields.", "OK");
                return;
            }

            // Save config
            await TelegramService.Instance.SaveConfigAsync(apiId, apiHash, phoneNumber);

            // Connect
            await ProcessLoginStepAsync(() => TelegramService.Instance.InitializeClientAndConnectAsync());
        }

        private async Task ProcessLoginStepAsync(Func<Task<string?>> loginFunc)
        {
            try
            {
                var result = await loginFunc();

                if (result == "verification_code")
                {
                    CodePanel.IsVisible = true;
                    TwoFactorPanel.IsVisible = false;
                    CredentialsCard.IsVisible = false;
                }
                else if (result == "password")
                {
                    CodePanel.IsVisible = false;
                    TwoFactorPanel.IsVisible = true;
                    CredentialsCard.IsVisible = false;
                }
                else if (result == null) // Fully logged in
                {
                    await UpdateStatusUIAsync();
                    await DisplayAlertAsync("Success", "Successfully connected to Telegram!", "OK");
                    
                    // Navigate to Upload tab
                    await Shell.Current.GoToAsync("///UploadPage");
                }
                else if (result.StartsWith("error:"))
                {
                    await DisplayAlertAsync("Connection Error", result.Substring(6), "OK");
                    await UpdateStatusUIAsync();
                }
                else
                {
                    await DisplayAlertAsync("Warning", $"Unknown connection response: {result}", "OK");
                    await UpdateStatusUIAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
                await UpdateStatusUIAsync();
            }
        }

        private async void SubmitCodeButton_Clicked(object sender, EventArgs e)
        {
            string code = CodeEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                await DisplayAlertAsync("Error", "Please enter the code.", "OK");
                return;
            }

            await ProcessLoginStepAsync(() => TelegramService.Instance.SubmitCodeAsync(code));
        }

        private async void SubmitTwoFactorButton_Clicked(object sender, EventArgs e)
        {
            string password = TwoFactorEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                await DisplayAlertAsync("Error", "Please enter your 2FA password.", "OK");
                return;
            }

            await ProcessLoginStepAsync(() => TelegramService.Instance.SubmitPasswordAsync(password));
        }

        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync("Logout", "Are you sure you want to log out?", "Yes", "No");
            if (confirm)
            {
                TelegramService.Instance.Logout();
                await UpdateStatusUIAsync();
            }
        }

        private void UpdateFFmpegUI()
        {
            if (FFmpegService.Instance.IsFFmpegInstalled)
            {
                FFmpegStatusIndicator.Color = Colors.Green;
                FFmpegStatusLabel.Text = "Installed";
                FFmpegDetailLabel.Text = $"Located at: {FFmpegService.Instance.FFmpegExePath}";
                DownloadFFmpegButton.IsVisible = false;
            }
            else
            {
                FFmpegStatusIndicator.Color = Colors.Red;
                FFmpegStatusLabel.Text = "Not Installed";
                FFmpegDetailLabel.Text = "FFmpeg is required for video conversion. It will be downloaded automatically when needed, or you can download it now.";
                DownloadFFmpegButton.IsVisible = true;
            }
        }

        private async void DownloadFFmpegButton_Clicked(object sender, EventArgs e)
        {
            DownloadFFmpegButton.IsEnabled = false;
            FFmpegDetailLabel.Text = "Starting download...";
            
            try
            {
                await FFmpegService.Instance.DownloadAndExtractFFmpegAsync(msg => 
                {
                    MainThread.BeginInvokeOnMainThread(() => FFmpegDetailLabel.Text = msg);
                });
                
                await DisplayAlertAsync("Success", "FFmpeg downloaded and installed successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to download FFmpeg: {ex.Message}", "OK");
            }
            finally
            {
                UpdateFFmpegUI();
            }
        }
    }
}
