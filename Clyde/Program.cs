using Apollo.Enums;
using Apollo.Export;
using Apollo.Service;
using System.IO.Compression;
using Serilog;
using Spectre.Console;

namespace Apollo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await ApplicationService.InitializeAsync().ConfigureAwait(false);
            await DiscordService.InitializeAsync().ConfigureAwait(false);

            // Check for /export or /findpath commands.
            if (args.Length >= 1)
            {
                string command = args[0].ToLowerInvariant();
                if (command.Equals("/export"))
                {
                    if (args.Length < 2)
                    {
                        Log.Error("Export command requires a file path parameter.");
                        return;
                    }
                    string exportPath = args[1];
                    Log.Information("Export command received. Exporting file at path: {path}", exportPath);

                    // Call our FileExporter export method.
                    await Exporter.FileExport.ExportAsync(exportPath).ConfigureAwait(false);

                    // Determine the actual file to send: if a zip was created, send that.
                    string fullPath = Path.Combine(ApplicationService.ExportDirectory, exportPath);
                    FileInfo fileInfo = new(fullPath);
                    if (fileInfo.Exists && fileInfo.Length > 8 * 1024 * 1024)
                    {
                        fullPath = Path.ChangeExtension(fullPath, ".zip");
                    }

                    await DiscordService.SendExportFileAsync(fullPath).ConfigureAwait(false);
                    Log.Information("Export operation completed. Press any key to exit.");
                    Console.ReadKey();
                    return;
                }
                else if (command.Equals("/findpath"))
                {
                    if (args.Length < 3)
                    {
                        Log.Error("/findpath command requires 2 parameters: onlynew (true/false) and the search string.");
                        return;
                    }

                    bool onlyNew = bool.TryParse(args[1], out bool result) && result;
                    string searchString = args[2];
                    Log.Information("/findpath command received. Searching for \"{searchString}\" (OnlyNew: {onlyNew})", searchString, onlyNew);

                    await ProcessFindPathCommand(onlyNew, searchString).ConfigureAwait(false);
                    Console.ReadKey();
                    return;
                }
            }

#if !DEBUG
            var updateMode = AnsiConsole.Prompt(new SelectionPrompt<EUpdateMode>()
                .Title("Choose the [45]Update mode[/]")
                .PageSize(10)
                .HighlightStyle("45")
                .MoreChoicesText("[grey](Move up and down to see more options)[/]")
                .AddChoices(Enum.GetValues<EUpdateMode>()));
#else
            var updateMode = EUpdateMode.GetNewFiles;
#endif

            Log.Information("UpdateMode: {0}'", updateMode);
            await ApplicationService.CUE4Parse.InitializeAsync(updateMode).ConfigureAwait(false);

            // Example: send a placeholder export file via Discord.
            await DiscordService.SendExportFileAsync(Path.Combine(ApplicationService.ExportDirectory, "placeholder.json")).ConfigureAwait(false);
            Log.Information("All operations completed. Press any key to exit");
            Console.ReadKey();
        }

        /// <summary>
        /// Processes the /findpath command. Searches through files (either only new files or all files)
        /// and writes a text file listing the paths of files whose contents contain the search string.
        /// If the resulting file exceeds 8 MB, it will be zipped.
        /// </summary>
        private static async Task ProcessFindPathCommand(bool onlyNew, string searchString)
        {
            var resultPaths = new List<string>();

            // Determine which files to search.
            if (onlyNew)
            {
                // Search only in new files loaded from backup.
                foreach (var entry in ApplicationService.CUE4Parse.Entries)
                {
                    try
                    {
                        if (!ApplicationService.CUE4Parse.Provider.TryCreateReader(entry.Path, out var reader))
                            continue;
                        byte[] data = reader.ReadBytes((int)(reader.Length - reader.Position));
                        string text = System.Text.Encoding.UTF8.GetString(data);
                        if (text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        {
                            resultPaths.Add(entry.Path);
                        }
                    }
                    catch { /* skip files that cannot be read as text */ }
                }
            }
            else
            {
                // Search in all files from the provider.
                foreach (var filePath in ApplicationService.CUE4Parse.Provider.Files.Keys)
                {
                    try
                    {
                        if (!ApplicationService.CUE4Parse.Provider.TryCreateReader(filePath, out var reader))
                            continue;
                        byte[] data = reader.ReadBytes((int)(reader.Length - reader.Position));
                        string text = System.Text.Encoding.UTF8.GetString(data);
                        if (text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        {
                            resultPaths.Add(filePath);
                        }
                    }
                    catch { /* skip files that cannot be read as text */ }
                }
            }

            // Write the results to a temporary text file.
            string tempFile = Path.Combine(ApplicationService.ExportDirectory, "findpath_results.txt");
            await File.WriteAllLinesAsync(tempFile, resultPaths);

            // If the file is too large, compress it.
            FileInfo fileInfo = new(tempFile);
            string fileToSend = tempFile;
            if (fileInfo.Length > 8 * 1024 * 1024)
            {
                fileToSend = Path.ChangeExtension(tempFile, ".zip");
                if (File.Exists(fileToSend))
                    File.Delete(fileToSend);
                using (var zip = ZipFile.Open(fileToSend, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(tempFile, Path.GetFileName(tempFile));
                }
            }

            await DiscordService.SendExportFileAsync(fileToSend).ConfigureAwait(false);
            Log.Information("/findpath operation completed. Sent {fileToSend} to Discord.", fileToSend);
        }
    }
}
