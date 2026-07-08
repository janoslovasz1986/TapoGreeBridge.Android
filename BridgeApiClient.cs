using System.Net.Http.Json;

namespace TapoGreeBridge.Android;

/// <summary>
/// Talks to the TapoGreeBridge Windows service's HTTP API (see the main project's
/// Program.cs: GET /status, POST /rooms/{name}/target). This only works on the home
/// WiFi network, since the service has no auth and is bound to the local network.
/// </summary>
public sealed class BridgeApiClient
{
    private readonly HttpClient _http;

    public BridgeApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<List<RoomState>> GetStatusAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<List<RoomState>>("/status", ct);
        return response ?? new List<RoomState>();
    }

    public async Task SetTargetAsync(string roomName, double celsius, CancellationToken ct = default)
    {
        var encodedName = Uri.EscapeDataString(roomName);
        var response = await _http.PostAsJsonAsync($"/rooms/{encodedName}/target", new TargetRequest { Celsius = celsius }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendPowerAsync(string roomName, bool on, CancellationToken ct = default)
    {
        var encodedName = Uri.EscapeDataString(roomName);
        var response = await _http.PostAsJsonAsync($"/rooms/{encodedName}/power", new PowerRequest { On = on }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendModeAsync(string roomName, int mod, CancellationToken ct = default)
    {
        var encodedName = Uri.EscapeDataString(roomName);
        var response = await _http.PostAsJsonAsync($"/rooms/{encodedName}/mode", new ModeRequest { Mod = mod }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetActiveAsync(string roomName, bool active, CancellationToken ct = default)
    {
        var encodedName = Uri.EscapeDataString(roomName);
        var response = await _http.PostAsJsonAsync($"/rooms/{encodedName}/active", new ActiveRequest { Active = active }, ct);
        response.EnsureSuccessStatusCode();
    }
}