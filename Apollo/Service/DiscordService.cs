using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

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

    public static async Task SendVideoAsync()
    {
        if (await Client.GetChannelAsync(ChannelId) is not IMessageChannel channel)
        {
            Log.Error("Unable to find channel");
            return;
        }
        
        var videoPath = Path.Combine(ApplicationService.ExportDirectory, "output.mp4");

        if (!File.Exists(videoPath)) return;
        await channel.SendFileAsync(videoPath, "@everyone");
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
