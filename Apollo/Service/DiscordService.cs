using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;
using Apollo.Service;

namespace Apollo.Service;

public static class DiscordService
{
    private const string Token = "";
    private const ulong ChannelId = 0;

    private static readonly DiscordSocketClient Client;

    static DiscordService()
    {
        Client = new DiscordSocketClient();
    }

    public static async Task InitializeAsync()
    {
        Client.Log += LogAsync;
        
        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();
    }
    
    /// <summary>
    /// Sends the exported file (or zip) to the Discord channel.
    /// </summary>
    /// <param name="filePath">Absolute path of the file to send.</param>
    public static async Task SendExportFileAsync(string filePath)
    {
        if (await Client.GetChannelAsync(ChannelId) is not IMessageChannel channel)
        {
            Log.Error("Unable to find channel");
            return;
        }
        
        if (!File.Exists(filePath))
        {
            Log.Error("File {filePath} does not exist.", filePath);
            return;
        }
        
        await channel.SendFileAsync(filePath, "Here is your exported file");
        Log.Information("Sent exported file {filePath} to Discord", filePath);
    }
    
    private static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        
        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }
}
