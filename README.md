# FolderUIGram 🚀

A modern, cross-platform Telegram bulk file uploader built with **.NET MAUI** and **C#**. FolderUIGram features a sleek "Material Liquid Glass" UI and allows users to easily log in to their Telegram accounts, select a local folder, and automatically upload all its contents directly to a specific Telegram Chat or Channel.

## ✨ Features

- **Cross-Platform Support**: Fully supports Windows natively, and Android via APK. (iOS and macOS compatible via MAUI).
- **Sleek UI/UX**: Designed with a premium "Glassmorphism" UI, dynamic gradients, and real-time upload progress indicators.
- **Bulk Folder Uploads**: Recursively scans folders and uploads all files inside them efficiently.
- **Automatic FFmpeg Integration**: 
  - Need to upload `.mkv` or `.avi` videos? The app automatically checks, downloads, and uses an embedded **FFmpeg** instance to seamlessly convert non-MP4 videos to `.mp4` before uploading them.
- **Secure Telegram Login**: Built directly on top of `WTelegramClient`, fully supporting Phone Number + Code + 2FA Password login flows.
- **Activity Logging**: Built-in visual console to watch the background upload and conversion processes in real time.

## 🛠 Tech Stack

- **Framework**: [.NET MAUI](https://dotnet.microsoft.com/en-us/apps/maui) (.NET 11 Preview)
- **Language**: C#
- **Telegram API Engine**: [WTelegramClient](https://github.com/WTelegram/WTelegramClient)
- **Video Conversion**: [FFmpeg.NET](https://github.com/cmxl/FFmpeg.NET)
- **Storage Management**: CommunityToolkit.Maui.Storage

## 🚀 Getting Started

### Prerequisites
- .NET 11 SDK (or newer)
- Visual Studio 2022 / VS Code with MAUI Workload installed
- A Telegram API ID and API Hash (Get it from [my.telegram.org](https://my.telegram.org))

### Installation & Build
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/FolderUIGram.git
   cd FolderUIGram
   ```
2. Build for Windows:
   ```bash
   dotnet build -f net11.0-windows10.0.19041.0 -c Release
   ```
3. Build the Android APK:
   ```bash
   dotnet publish -f net11.0-android -c Release -p:AndroidPackageFormat=apk
   ```

## 📸 Usage
1. Open the application and navigate to the **Settings** tab.
2. Enter your Telegram API ID, API Hash, and Phone Number.
3. Verify your login via the code sent to your Telegram app (and 2FA password if enabled).
4. Navigate to the **Upload** tab, click **Choose Local Folder**, and select a target Telegram Chat/Channel from the dropdown.
5. Click **Start Upload** and watch it go!

## 📜 License
This project is licensed under the MIT License.

## Limitations:
- Conversion via ffmpeg is not working in Android as it is windows related library. 
