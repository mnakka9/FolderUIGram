#r "nuget: xFFmpeg.NET, 7.4.0"

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

// To use this script, you must have dotnet-script installed:
// dotnet tool install -g dotnet-script
//
// Run the script:
// dotnet script ConvertDirectoryToMp4.csx <directory_path>

if (Args.Count < 1)
{
    Console.WriteLine("Usage: dotnet script ConvertDirectoryToMp4.csx <directory_path>");
    return;
}

string directoryPath = Args[0];
string ffmpegPath = @"C:\Users\mahes\AppData\Local\User Name\mn.blazeapps.folderuigram\Data\ffmpeg\ffmpeg.exe";

if (!Directory.Exists(directoryPath))
{
    Console.WriteLine($"Directory not found: {directoryPath}");
    return;
}

if (!File.Exists(ffmpegPath))
{
    Console.WriteLine($"FFmpeg not found at: {ffmpegPath}");
    return;
}

// Same video extensions used in the project
var VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".mkv", ".avi", ".mov", ".flv", ".webm", ".3gp", ".wmv", ".mpeg", ".mpg", ".m4v"
};

bool IsConvertibleVideo(string filePath)
{
    string ext = Path.GetExtension(filePath);
    return VideoExtensions.Contains(ext);
}

// Exactly the same logic as ConvertToMp4Async in the project (ActionsPage.xaml.cs)
async Task<string> ConvertToMp4Async(string ffmpegExe, string inputFilePath, Action<string> statusCallback)
{
    // Replaced FileSystem.CacheDirectory with Path.GetTempPath() since we are running as a standalone script
    var tempDir = Path.GetTempPath(); 
    var outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".mp4";
    var outputFilePath = Path.Combine(tempDir, outputFileName);

    if (File.Exists(outputFilePath))
    {
        File.Delete(outputFilePath);
    }

    statusCallback($"Remuxing {Path.GetFileName(inputFilePath)} to MP4 using FFmpeg.NET...");

    try
    {
        var engine = new FFmpeg.NET.Engine(ffmpegExe);
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
            var engine = new FFmpeg.NET.Engine(ffmpegExe);
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

// Main logic to iterate through files in the directory
var files = Directory.GetFiles(directoryPath);
int convertedCount = 0;

foreach (var file in files)
{
    if (IsConvertibleVideo(file))
    {
        Console.WriteLine($"\n--- Processing: {Path.GetFileName(file)} ---");
        try
        {
            var resultPath = await ConvertToMp4Async(ffmpegPath, file, msg => Console.WriteLine(msg));
            Console.WriteLine($"\nSuccessfully converted to: {resultPath}");
            convertedCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFailed to convert {file}: {ex.Message}");
        }
    }
}

Console.WriteLine($"\nDone. Converted {convertedCount} file(s).");
