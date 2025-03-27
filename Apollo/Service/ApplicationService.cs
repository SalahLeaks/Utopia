using System.IO.Compression;
using System.Reflection;
using Apollo.ViewModels;
using CUE4Parse.Compression;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Apollo.Service
{
    public static class ApplicationService
    {
        private static string OutputDirectory = Path.Combine(Environment.CurrentDirectory, "Output");
        public static readonly string DataDirectory = Path.Combine(OutputDirectory, ".data");
        public static readonly string LogsDirectory = Path.Combine(OutputDirectory, "Logs");
        public static readonly string ManifestCacheDirectory = Path.Combine(DataDirectory, "ManifestCache");
        public static readonly string ChunkCacheDirectory = Path.Combine(DataDirectory, "ChunksCache");
        public static readonly string ExportDirectory = Path.Combine(OutputDirectory, "Export");
        public static readonly string AudioFilesDirectory = Path.Combine(ExportDirectory, "Audios");

        public static readonly ApiEndpointViewModel Api = new();
        public static readonly CUE4ParseViewModel CUE4Parse = new();
        public static readonly BackupViewModel Backup = new();

        public static async Task InitializeAsync()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                .WriteTo.File(Path.Combine(LogsDirectory, $"Apollo-{DateTime.Now:dd-MM-yyyy}.log"))
                .CreateLogger();

            foreach (var directory in new[]
                     { OutputDirectory, DataDirectory, ManifestCacheDirectory, ChunkCacheDirectory, ExportDirectory, AudioFilesDirectory, LogsDirectory })
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            // Clean previously exported files.
            var exportedFiles = Directory.GetFiles(ExportDirectory, "*.*", SearchOption.AllDirectories);
            Parallel.ForEach(exportedFiles, File.Delete);

            await InitializeDependenciesAsync().ConfigureAwait(false);
        }

        private static async Task InitializeDependenciesAsync()
        {
            // Removed resource copying – resources are no longer used.
            await InitializeOodle().ConfigureAwait(false);
            await InitializeZlib().ConfigureAwait(false);
        }

        private static async Task InitializeOodle()
        {
            var oodlePath = Path.Combine(DataDirectory, OodleHelper.OODLE_DLL_NAME);
            if (!File.Exists(oodlePath))
                await OodleHelper.DownloadOodleDllAsync(oodlePath).ConfigureAwait(false);
            OodleHelper.Initialize(oodlePath);
        }

        private static async Task InitializeZlib()
        {
            var zlibPath = Path.Combine(DataDirectory, ZlibHelper.DLL_NAME);
            if (!File.Exists(zlibPath))
                await ZlibHelper.DownloadDllAsync(zlibPath).ConfigureAwait(false);
            ZlibHelper.Initialize(zlibPath);
        }
    }
}
