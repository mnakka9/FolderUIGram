using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using WTelegram;

namespace FolderUIGram
{
    public partial class MessagesPage : ContentPage
    {
        private Client? _client;

        public MessagesPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool loggedIn = TelegramService.Instance.IsLoggedIn;
            if (!loggedIn)
            {
                MainContentScroll.IsVisible = false;
                NotLoggedInPanel.IsVisible = true;
            }
            else
            {
                MainContentScroll.IsVisible = true;
                NotLoggedInPanel.IsVisible = false;
                _client = TelegramService.Instance.Client;

                // Auto load chats if connected and picker is empty
                if (_client != null && (TargetChatPicker.ItemsSource == null || !TargetChatPicker.ItemsSource.Cast<object>().Any()))
                {
                    await LoadChatsAsync();
                }
            }
        }

        private async void LoadChatsButton_Clicked(object sender, EventArgs e)
        {
            await LoadChatsAsync();
        }

        private async Task LoadChatsAsync()
        {
            if (_client == null) return;

            try
            {
                StatusCard.IsVisible = true;
                StatusTextLabel.Text = "Fetching active chats and channels...";
                
                var chats = await _client.Messages_GetAllChats();
                var channels = chats.chats.Where(c => c.Value.IsActive).Select(x => new ChannelModel(x.Key, x.Value.Title, x.Value)).ToList();

                if (channels.Count > 0)
                {
                    TargetChatPicker.ItemsSource = channels;
                    StatusTextLabel.Text = $"Found {channels.Count} active destinations.";
                }
                else
                {
                    TargetChatPicker.ItemsSource = null;
                    StatusTextLabel.Text = "No active channels/chats found.";
                    await DisplayAlertAsync("Channels", "No active channels or chats found.", "OK");
                }
            }
            catch (Exception ex)
            {
                StatusTextLabel.Text = $"Error: {ex.Message}";
                await DisplayAlertAsync("Error", $"Failed to fetch chats: {ex.Message}", "OK");
            }
        }

        private async void SendMessageButton_Clicked(object sender, EventArgs e)
        {
            if (_client == null)
            {
                await DisplayAlertAsync("Error", "Telegram client not connected.", "OK");
                return;
            }

            var selectedChannel = TargetChatPicker.SelectedItem as ChannelModel;
            if (selectedChannel == null)
            {
                await DisplayAlertAsync("Validation Error", "Please select a target chat or channel.", "OK");
                return;
            }

            string text = MessageEditor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlertAsync("Validation Error", "Please write a message to send.", "OK");
                return;
            }

            try
            {
                StatusCard.IsVisible = true;
                StatusTextLabel.Text = $"Sending message to {selectedChannel.Title}...";
                SendMessageButton.IsEnabled = false;

                await _client.SendMessageAsync(selectedChannel.Chat, text);

                StatusTextLabel.Text = $"✅ Message sent successfully to {selectedChannel.Title} at {DateTime.Now:HH:mm:ss}!";
                MessageEditor.Text = string.Empty;
            }
            catch (Exception ex)
            {
                StatusTextLabel.Text = $"❌ Error: {ex.Message}";
                await DisplayAlertAsync("Error", $"Failed to send message: {ex.Message}", "OK");
            }
            finally
            {
                SendMessageButton.IsEnabled = true;
            }
        }

        private async void GoToSettingsButton_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///SettingsPage");
        }
    }
}
