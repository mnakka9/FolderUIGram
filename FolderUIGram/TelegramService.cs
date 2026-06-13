using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WTelegram;
using TL;

namespace FolderUIGram
{
    public class TelegramService
    {
        private static TelegramService? _instance;
        public static TelegramService Instance => _instance ??= new TelegramService();

        private const string ConfigFileName = "telegram.json";
        public Client? Client { get; private set; }
        public TelegramConfig? Config { get; private set; }
        public string? LastConnectionResult { get; private set; }

        public bool IsLoggedIn => Client != null && Client.User != null;

        private TelegramService()
        {
            // Redirect WTelegram logs to avoid PlatformNotSupportedException on Android (Console.Out crash)
            WTelegram.Helpers.Log = (level, message) => 
            {
                System.Diagnostics.Debug.WriteLine($"[WTelegram {level}] {message}");
            };
            
            LoadConfig();
        }

        public void LoadConfig()
        {
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, ConfigFileName);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    Config = JsonSerializer.Deserialize<TelegramConfig>(json);
                }
            }
            catch
            {
                // Ignore malformed config
            }
        }

        public async Task SaveConfigAsync(string apiId, string apiHash, string phoneNumber)
        {
            Config = new TelegramConfig
            {
                ApiId = apiId,
                ApiHash = apiHash,
                PhoneNumber = phoneNumber
            };
            var path = Path.Combine(FileSystem.AppDataDirectory, ConfigFileName);
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            await File.WriteAllTextAsync(path, json);
        }

        private readonly System.Threading.SemaphoreSlim _connectionLock = new System.Threading.SemaphoreSlim(1, 1);

        public async Task<string?> InitializeClientAndConnectAsync()
        {
            if (Config == null || string.IsNullOrEmpty(Config.ApiId) || string.IsNullOrEmpty(Config.ApiHash) || string.IsNullOrEmpty(Config.PhoneNumber))
            {
                LastConnectionResult = "config_missing";
                return "config_missing";
            }

            await _connectionLock.WaitAsync();
            try
            {
                var sessionPath = Path.Combine(FileSystem.AppDataDirectory, "wtelegram.session");
                
                if (Client == null)
                {
                    Client = new Client(int.Parse(Config.ApiId), Config.ApiHash, sessionPath);
                }
                
                // Start connection and check login status
                var result = await Client.Login(Config.PhoneNumber);
                LastConnectionResult = result;
                return result; // "verification_code", "password", or null (fully logged in)
            }
            catch (Exception ex)
            {
                LastConnectionResult = $"error: {ex.Message}";
                return $"error: {ex.Message}";
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task<string?> SubmitCodeAsync(string code)
        {
            if (Client == null) return "error: client not initialized";
            var result = await Client.Login(code);
            LastConnectionResult = result;
            return result;
        }

        public async Task<string?> SubmitPasswordAsync(string password)
        {
            if (Client == null) return "error: client not initialized";
            var result = await Client.Login(password);
            LastConnectionResult = result;
            return result;
        }

        public void Logout()
        {
            Client?.Dispose();
            Client = null;
            LastConnectionResult = null;
            
            try
            {
                var sessionPath = Path.Combine(FileSystem.AppDataDirectory, "wtelegram.session");
                if (File.Exists(sessionPath))
                {
                    File.Delete(sessionPath);
                }
            }
            catch { }
        }
    }
}
