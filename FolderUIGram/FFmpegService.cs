using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace FolderUIGram
{
    public class FFmpegService
    {
        private static FFmpegService? _instance;
        public static FFmpegService Instance => _instance ??= new FFmpegService();

        public string FFmpegDirPath => Path.Combine(FileSystem.AppDataDirectory, "ffmpeg");
        public string FFmpegExePath => Path.Combine(FFmpegDirPath, "ffmpeg.exe");

        public bool IsFFmpegInstalled => File.Exists(FFmpegExePath);

        private FFmpegService() { }

        public async Task<string> DownloadAndExtractFFmpegAsync(Action<string> statusCallback)
        {
            if (IsFFmpegInstalled)
            {
                statusCallback("FFmpeg is already installed.");
                return FFmpegExePath;
            }

            Directory.CreateDirectory(FFmpegDirPath);
            var zipPath = Path.Combine(FFmpegDirPath, "ffmpeg.zip");

            statusCallback("Downloading FFmpeg (approx. 90MB)...");
            using (var client = new System.Net.Http.HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                
                using (var response = await client.GetAsync("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip", System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var bytesReceived = 0L;
                    
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            bytesReceived += bytesRead;
                            
                            if (totalBytes > 0)
                            {
                                double pct = (double)bytesReceived / totalBytes;
                                statusCallback($"Downloading FFmpeg: {pct:P0}...");
                            }
                            else
                            {
                                statusCallback($"Downloading FFmpeg: {FormatFileSize(bytesReceived)}...");
                            }
                        }
                    }
                }
            }

            statusCallback("Extracting FFmpeg...");
            await Task.Run(() =>
            {
                if (File.Exists(zipPath))
                {
                    var extractTempDir = Path.Combine(FFmpegDirPath, "temp_extract");
                    if (Directory.Exists(extractTempDir))
                    {
                        Directory.Delete(extractTempDir, true);
                    }
                    Directory.CreateDirectory(extractTempDir);
                    
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractTempDir);

                    var ffmpegFiles = Directory.GetFiles(extractTempDir, "ffmpeg.exe", SearchOption.AllDirectories);
                    if (ffmpegFiles.Length > 0)
                    {
                        if (File.Exists(FFmpegExePath))
                        {
                            File.Delete(FFmpegExePath);
                        }
                        File.Move(ffmpegFiles[0], FFmpegExePath);
                    }

                    try
                    {
                        Directory.Delete(extractTempDir, true);
                        File.Delete(zipPath);
                    }
                    catch { }
                }
            });

            if (File.Exists(FFmpegExePath))
            {
                statusCallback("FFmpeg downloaded and ready.");
                return FFmpegExePath;
            }
            else
            {
                throw new FileNotFoundException("Failed to download or extract FFmpeg executable.");
            }
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
    }
}
