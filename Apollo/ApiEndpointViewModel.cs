using Apollo.Framework;
using Apollo.ViewModels.API;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using Serilog;

namespace Apollo.ViewModels;

public class ApiEndpointViewModel
{
    public EpicApiEndpoint EpicApi;
    public FortniteCentralApiEndpoint FortniteCentralApi;
    public FModelApiEndpoint FModelApi;

    private readonly RestClient _client = new(new RestClientOptions
    {
        UserAgent = "Apollo",
        Timeout = TimeSpan.FromSeconds(15),
    }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());

    public ApiEndpointViewModel()
    {
        EpicApi = new(_client);
        FortniteCentralApi = new(_client);
        FModelApi = new(_client);
    }
    
    public async Task DownloadFileAsync(string url, string installationLocation)
    {
        if (File.Exists(installationLocation))
            return;
        
        var request = new FRestRequest(url);
        var data = await _client.DownloadDataAsync(request).ConfigureAwait(false);
        if (data?.Length <= 0)
        {
            Log.Error("An error occured while downloading the file");
            return;
        }

        await File.WriteAllBytesAsync(installationLocation, data!).ConfigureAwait(false);
    }
}
