using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Apollo.Service;
using CUE4Parse.FileProvider;

namespace Apollo.Export
{
    public partial class FileExporter : IExporter
    {
        // Use IFileProvider so that a StreamedFileProvider is acceptable.
        private readonly IFileProvider _provider;

        public FileExporter()
        {
            // ApplicationService.CUE4Parse.Provider is a StreamedFileProvider which implements IFileProvider.
            _provider = ApplicationService.CUE4Parse.Provider;
        }

        /// <summary>
        /// Exports the file at the given relative path if it does not already exist.
        /// If the file exceeds 8 MB, it is compressed into a ZIP.
        /// Allows the user to omit the extension.
        /// </summary>
        /// <param name="exportRelativePath">Relative path of the asset file (e.g., "assets/myfile")</param>
        public async Task ExportAsync(string exportRelativePath)
        {
            // If no extension is provided, try to find a matching file from the provider.
            if (!Path.HasExtension(exportRelativePath))
            {
                var match = _provider.Files.Keys.FirstOrDefault(k =>
                    Path.GetFileNameWithoutExtension(k).Equals(exportRelativePath, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    exportRelativePath = match;
                }
                else
                {
                    Log.Error("No file found matching: {path}", exportRelativePath);
                    return;
                }
            }

            string fullPath = Path.Combine(ApplicationService.ExportDirectory, exportRelativePath);

            // Check if the file already exists.
            if (File.Exists(fullPath))
            {
                Log.Information("File already exists: {path}", fullPath);
            }
            else
            {
                Log.Information("File not found, extracting: {path}", exportRelativePath);
                try
                {
                    await ExtractFileAsync(exportRelativePath, fullPath);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to extract file: {error}", ex.Message);
                    return;
                }
            }

            // If the file exceeds 8 MB, compress it into a ZIP archive.
            FileInfo fileInfo = new FileInfo(fullPath);
            if (fileInfo.Exists && fileInfo.Length > 8 * 1024 * 1024)
            {
                string zipPath = Path.ChangeExtension(fullPath, ".zip");
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(fullPath, Path.GetFileName(fullPath));
                }
                Log.Information("File was larger than 8MB. Compressed to {zipPath}", zipPath);
            }
        }

        /// <summary>
        /// Extracts the file from the .pak archive using the file provider.
        /// </summary>
        /// <param name="relativePath">Relative path of the asset in the .pak</param>
        /// <param name="outputPath">Output path for the extracted file</param>
        private async Task ExtractFileAsync(string relativePath, string outputPath)
        {
            // Adjusted to new signature: TryCreateReader requires an out parameter.
            if (!_provider.TryCreateReader(relativePath, out var reader))
            {
                Log.Error("Could not find {0} in the .pak files.", relativePath);
                return;
            }

            long bytesToRead = reader.Length - reader.Position;
            byte[] data = reader.ReadBytes((int)bytesToRead);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
            await File.WriteAllBytesAsync(outputPath, data);
            Log.Information("Successfully extracted {0} to {1}", relativePath, outputPath);
        }

        // A default ExportAsync method for interface compliance.
        public async Task ExportAsync()
        {
            await Task.CompletedTask;
        }
    }
}
