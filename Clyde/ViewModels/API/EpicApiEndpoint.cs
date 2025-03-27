using Apollo.Framework;
using Apollo.ViewModels.API.Models;
using EpicManifestParser.Api;
using RestSharp;
using Serilog;

namespace Apollo.ViewModels.API;

public class EpicApiEndpoint : AbstractApiProvider
{
    private const string AUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string MANFEST_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";
    private const string VERIFY_URL = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify";
    private const string BASIC_TOKEN = "basic ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ=";
    
    private string AuthToken { get; set; }

    public EpicApiEndpoint(RestClient client) : base(client)
    {
        AuthToken = string.Empty;
    }
    
    public async Task<ManifestInfo> GetManifestAsync()
    {
        await VerifyTokenAsync().ConfigureAwait(false);
        
        var request = new FRestRequest(MANFEST_URL);
        request.AddHeader("Authorization", $"bearer {AuthToken}");
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return ManifestInfo.Deserialize(response.RawBytes) ?? throw new InvalidOperationException("Response Data for manifest was null");
    }
    
    private async Task<string> CreateAuthAsync()
    {
        var request = new FRestRequest(AUTH_URL, Method.Post);
        request.AddHeader("Authorization", BASIC_TOKEN);
        request.AddParameter("grant_type", "client_credentials");
        var response = await _client.ExecuteAsync<AuthResponse>(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data != null ? response.Data.AccessToken : string.Empty;
    }

    private async Task VerifyTokenAsync()
    {
        if (await IsTokenExpiredAsync().ConfigureAwait(false))
            AuthToken = await CreateAuthAsync().ConfigureAwait(false);
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        var request = new FRestRequest(VERIFY_URL);
        request.AddHeader("Authorization", $"bearer {AuthToken}");
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        return !response.IsSuccessful;
    }
}
