using CommunityToolkit.Maui.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using WTelegram;

namespace FolderUIGram
{
    public partial class ActionsPage : ContentPage, INotifyPropertyChanged
    {
        private Client? _client;
        private string _selectedFolderPath = string.Empty;
        private double _overallProgress;
        private string _overallProgressText = string.Empty;
        private string _currentFileText = string.Empty;
        private bool _isUploading;
        private StringBuilder _logBuilder = new();
        private string _logText = string.Empty;

        public double OverallProgress
        {
            get => _overallProgress;
            set
            {
                if (_overallProgress != value)
                {
                    _overallProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OverallProgressText
        {
            get => _overallProgressText;
            set
            {
                if (_overallProgressText != value)
                {
                    _overallProgressText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentFileText
        {
            get => _currentFileText;
            set
            {
                if (_currentFileText != value)
                {
                    _currentFileText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                if (_isUploading != value)
                {
                    _isUploading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LogText
        {
            get => _logText;
            set
            {
                if (_logText != value)
                {
                    _logText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ActionsPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
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
                if (_client != null && _client.User != null)
                {
                    UserLabel.Text = $"Logged in as: {_client.User.first_name} {_client.User.last_name} (@{_client.User.username})";
                }
            }
        }

        private async void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logBuilder.AppendLine($"[{timestamp}] {message}");
            LogText = _logBuilder.ToString();

            // Auto-scroll to bottom of logs
            await Task.Delay(50);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await LogScrollView.ScrollToAsync(LogTextArea, ScrollToPosition.End, true);
                }
                catch
                {
                    // Ignore transient exceptions if scroll fails
                }
            });
        }

        private async void ChoseFolder_Clicked(object sender, EventArgs e)
        {
            if (_client == null)
            {
                await DisplayAlertAsync("Error", "Please connect to Telegram first.", "OK");
                return;
            }

#if ANDROID
            if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity != null)
            {
                if (!Android.OS.Environment.IsExternalStorageManager)
                {
                    var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                    var uri = Android.Net.Uri.Parse("package:" + Microsoft.Maui.ApplicationModel.AppInfo.Current.PackageName);
                    intent.SetData(uri);
                    Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.StartActivity(intent);
                    await DisplayAlertAsync("Permission Required", "Please grant 'All files access' permission and try again.", "OK");
                    return;
                }
            }
#endif

            var result = await FolderPicker.Default.PickAsync();

            if (!result.IsSuccessful)
            {
                await DisplayAlertAsync("Folder Selection", "No folder was selected.", "OK");
                return;
            }

            FolderPathLabel.Text = $"Selected Folder: {result.Folder.Path}";
            _selectedFolderPath = result.Folder.Path;

            try
            {
                AddLog("Fetching channels and chats...");
                var chats = await _client.Messages_GetAllChats();
                var channels = chats.chats.Where(c => c.Value.IsActive).Select(x => new ChannelModel(x.Key, x.Value.Title, x.Value)).ToList();

                if (channels.Count > 0)
                {
                    ChannelsListView.ItemsSource = channels;
                    AddLog($"Found {channels.Count} active channels/chats.");
                }
                else
                {
                    await DisplayAlertAsync("Channels", "No active channels found.", "OK");
                    AddLog("No active channels/chats found.");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to fetch channels: {ex.Message}", "OK");
                AddLog($"[ERROR] Failed to fetch channels: {ex.Message}");
            }
        }

        private async void StartUploadButton_Clicked(object sender, EventArgs e)
        {
            if (_client == null)
            {
                await DisplayAlertAsync("Upload Error", "Client is not connected.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                await DisplayAlertAsync("Upload Error", "Please select a folder first.", "OK");
                return;
            }

            var selectedChannel = ChannelsListView.SelectedItem as ChannelModel;
            if (selectedChannel is null)
            {
                await DisplayAlertAsync("Upload Error", "No channel/chat selected for upload.", "OK");
                return;
            }

            IsUploading = true;
            ProgressCard.IsVisible = true;
            LogCard.IsVisible = true;
            OverallProgress = 0;
            OverallProgressText = "Analyzing folders...";
            CurrentFileText = "Analyzing...";
            _logBuilder.Clear();
            LogText = string.Empty;

            DirectoryInfo dirInfo = new(_selectedFolderPath);

            AddLog($"Starting upload process to channel: {selectedChannel.Title}");
            AddLog($"Root folder: {dirInfo.Name}");

            try
            {
                // Get all directories (including root)
                var allDirectories = new List<DirectoryInfo> { dirInfo };
                allDirectories.AddRange(dirInfo.GetDirectories("*", SearchOption.AllDirectories));

                // Count total files for progress tracking
                int totalFiles = allDirectories.Sum(d => d.GetFiles().Length);
                int uploadedFiles = 0;

                AddLog($"Found {allDirectories.Count} folder(s) with {totalFiles} total file(s)");

                if (totalFiles == 0)
                {
                    AddLog("ERROR: No files found in the selected folder");
                    await DisplayAlertAsync("Upload Error", "No files found in the selected folder.", "OK");
                    IsUploading = false;
                    ProgressCard.IsVisible = false;
                    return;
                }

                // Process each directory
                foreach (var directory in allDirectories)
                {
                    var filesInDirectory = directory.GetFiles();

                    if (filesInDirectory.Length == 0)
                        continue;

                    // Send folder name message at the beginning
                    string relativePath = Path.GetRelativePath(_selectedFolderPath, directory.FullName);
                    string folderDisplayName = relativePath == "." ? dirInfo.Name : relativePath;

                    AddLog($"[FOLDER] Processing folder: {folderDisplayName} ({filesInDirectory.Length} files)");
                    await _client.SendMessageAsync(selectedChannel.Chat, $"📁 Starting upload from folder: {folderDisplayName}");

                    // Upload all files in current directory
                    foreach (var file in filesInDirectory)
                    {
                        bool convertVideo = ConvertVideosSwitch.IsToggled && IsConvertibleVideo(file.FullName);
                        string fileToUpload = file.FullName;
                        string tempConvertedPath = string.Empty;

                        if (convertVideo)
                        {
                            try
                            {
                                string ffmpegPath = await FFmpegService.Instance.DownloadAndExtractFFmpegAsync(msg => AddLog($"[FFMPEG] {msg}"));
                                tempConvertedPath = await ConvertToMp4Async(ffmpegPath, file.FullName, msg => AddLog($"[FFMPEG] {msg}"));
                                fileToUpload = tempConvertedPath;
                                AddLog($"[FFMPEG] Successfully converted video to {Path.GetFileName(tempConvertedPath)}");
                            }
                            catch (Exception ex)
                            {
                                AddLog($"[FFMPEG] Error during video conversion: {ex.Message}. Uploading original file instead.");
                            }
                        }

                        CurrentFileText = $"Uploading: {Path.GetFileName(fileToUpload)} ({FormatFileSize(new FileInfo(fileToUpload).Length)})";
                        AddLog($"  [UPLOAD] Uploading: {Path.GetFileName(fileToUpload)} ({FormatFileSize(new FileInfo(fileToUpload).Length)})");
                        try
                        {
                            await UploadFile(fileToUpload, selectedChannel, uploadedFiles, totalFiles);
                            uploadedFiles++;

                            // Update overall progress
                            OverallProgress = (double)uploadedFiles / totalFiles;
                            OverallProgressText = $"Uploaded {uploadedFiles} of {totalFiles} files";
                            AddLog($"  [DONE] Completed: {Path.GetFileName(fileToUpload)}");
                        }
                        catch (Exception ex)
                        {
                            AddLog($"  [ERROR] Failed to upload: {Path.GetFileName(fileToUpload)} - {ex.Message}");
                            // continue with next file
                            continue;
                        }
                        finally
                        {
                            // Delete temporary converted file if we created one
                            if (!string.IsNullOrEmpty(tempConvertedPath) && File.Exists(tempConvertedPath))
                            {
                                try
                                {
                                    File.Delete(tempConvertedPath);
                                    AddLog($"[FFMPEG] Cleaned up temporary converted file.");
                                }
                                catch { }
                            }
                        }
                    }

                    // Send folder completion message
                    await _client.SendMessageAsync(selectedChannel.Chat, $"✅ Completed upload from folder: {folderDisplayName} ({filesInDirectory.Length} files uploaded)");
                    AddLog($"[COMPLETE] Folder complete: {folderDisplayName}");
                }

                // Set progress to complete
                OverallProgress = 1.0;
                OverallProgressText = $"All {totalFiles} files uploaded successfully!";
                CurrentFileText = "All done!";

                AddLog($"[SUCCESS] Upload process completed successfully!");
                AddLog($"Total: {totalFiles} files from {allDirectories.Count} folder(s)");

                await DisplayAlertAsync("Upload Complete", $"Successfully uploaded {totalFiles} files from {allDirectories.Count} folder(s)", "OK");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] {ex.Message}");
                AddLog($"Stack trace: {ex.StackTrace}");
                await DisplayAlertAsync("Upload Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsUploading = false;
            }
        }

        private async Task UploadFile(string filePath, ChannelModel channel, int fileIndex, int totalFiles)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            using FileStream fileStream = fileInfo.OpenRead();

            // Upload the file and get the InputFile
            var inputFile = await _client!.UploadFileAsync(fileStream, fileInfo.Name, (p, r) => 
            {
                double fileProgress = (double)p / r;
                // Update overall progress using fractional part of current file
                OverallProgress = (fileIndex + fileProgress) / totalFiles;
                OverallProgressText = $"Uploading file {fileIndex + 1} of {totalFiles} ({fileProgress:P0})";
            });

            // Send the file as a document to the channel
            await _client.SendMediaAsync(channel.Chat, fileInfo.Name, inputFile);
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void GoToSettingsButton_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///SettingsPage");
        }

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv", ".avi", ".mov", ".flv", ".webm", ".3gp", ".wmv", ".mpeg", ".mpg", ".m4v"
        };

        private static bool IsConvertibleVideo(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            return VideoExtensions.Contains(ext);
        }



        private async Task<string> ConvertToMp4Async(string ffmpegPath, string inputFilePath, Action<string> statusCallback)
        {
            var tempDir = FileSystem.CacheDirectory;
            var outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".mp4";
            var outputFilePath = Path.Combine(tempDir, outputFileName);

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            statusCallback($"Remuxing {Path.GetFileName(inputFilePath)} to MP4 using FFmpeg.NET...");

            try
            {
                var engine = new FFmpeg.NET.Engine(ffmpegPath);
                var inputFile = new FFmpeg.NET.InputFile(inputFilePath);
                var outputFile = new FFmpeg.NET.OutputFile(outputFilePath);

                var options = new FFmpeg.NET.ConversionOptions
                {
                    ExtraArguments = "-c copy"
                };

                // Add progress tracking or callbacks
                engine.Progress += (s, e) =>
                {
                    double pct = 0;
                    if (e.TotalDuration.TotalSeconds > 0)
                    {
                        pct = e.ProcessedDuration.TotalSeconds / e.TotalDuration.TotalSeconds;
                    }
                    statusCallback($"Conversion Progress: {pct:P0}");
                };

                await engine.ConvertAsync(inputFile, outputFile, options, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                statusCallback($"Direct remux failed ({ex.Message}). Transcoding {Path.GetFileName(inputFilePath)} to H.264/AAC MP4 using FFmpeg.NET...");
                
                try
                {
                    var engine = new FFmpeg.NET.Engine(ffmpegPath);
                    var inputFile = new FFmpeg.NET.InputFile(inputFilePath);
                    var outputFile = new FFmpeg.NET.OutputFile(outputFilePath);

                    var options = new FFmpeg.NET.ConversionOptions
                    {
                        ExtraArguments = "-c:v libx264 -preset fast -crf 23 -c:a aac"
                    };

                    await engine.ConvertAsync(inputFile, outputFile, options, System.Threading.CancellationToken.None);
                }
                catch (Exception fallbackEx)
                {
                    throw new Exception($"FFmpeg.NET conversion failed. Details: {fallbackEx.Message}");
                }
            }

            return outputFilePath;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
