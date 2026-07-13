using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace TapoGreeBridge.Android;

[Activity(Label = "TapoGree Bridge", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
public sealed class MainActivity : Activity
{
    private EditText _serverUrlInput = null!;
    private TextView _statusText = null!;
    private LinearLayout _roomsContainer = null!;
    private BridgeApiClient? _apiClient;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        _serverUrlInput = FindViewById<EditText>(Resource.Id.serverUrlInput)!;
        _statusText = FindViewById<TextView>(Resource.Id.statusText)!;
        _roomsContainer = FindViewById<LinearLayout>(Resource.Id.roomsContainer)!;

        var refreshButton = FindViewById<Button>(Resource.Id.refreshButton)!;
        refreshButton.Click += async (_, _) => await RefreshAsync();

        // Pre-fill the last used server URL, if any.
        var prefs = GetPreferences(FileCreationMode.Private)!;
        _serverUrlInput.Text = prefs.GetString("server_url", "") ?? "";
    }

    private async Task RefreshAsync()
    {
        var baseUrl = _serverUrlInput.Text?.Trim();
        if (string.IsNullOrEmpty(baseUrl))
        {
            _statusText.Text = "Add meg a szerver címét (pl. http://192.168.0.201:5080).";
            return;
        }

        // Persist the URL so it's remembered next time the app opens.
        GetPreferences(FileCreationMode.Private)!.Edit()!.PutString("server_url", baseUrl)!.Apply();

        if (!IsOnExpectedWifi(out var ssid))
        {
            _statusText.Text = $"Figyelem: nem otthoni WiFi-n vagy (jelenlegi: {ssid}). " +
                                "A szolgáltatás csak a helyi hálózaton érhető el.";
            // Don't return here - still let them try, in case the SSID check is wrong
            // (e.g. SSID hidden due to Android permissions) or they're on a VPN/hotspot
            // that still reaches the home network.
        }

        _statusText.Text = "Frissítés...";
        _apiClient = new BridgeApiClient(baseUrl);

        try
        {
            var rooms = await _apiClient.GetStatusAsync();
            RenderRooms(rooms);
            _statusText.Text = $"Frissítve: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Hiba: {ex.Message}";
        }
    }

    private void RenderRooms(List<RoomState> rooms)
    {
        _roomsContainer.RemoveAllViews();
        var inflater = LayoutInflater.FromContext(this)!;

        foreach (var room in rooms)
        {
            var card = inflater.Inflate(Resource.Layout.room_card, _roomsContainer, false)!;

            var nameView = card.FindViewById<TextView>(Resource.Id.roomName)!;
            var activeSwitch = card.FindViewById<ToggleButton>(Resource.Id.activeSwitch)!;
            var readingsView = card.FindViewById<TextView>(Resource.Id.roomReadings)!;
            var targetView = card.FindViewById<TextView>(Resource.Id.targetValue)!;
            var decreaseButton = card.FindViewById<Button>(Resource.Id.decreaseButton)!;
            var increaseButton = card.FindViewById<Button>(Resource.Id.increaseButton)!;

            nameView.Text = room.Name;
            activeSwitch.Checked = room.IsActive;
            targetView.Text = $"Cél: {room.TargetTemperatureCelsius:0.0}°C";

            // Dim the card when inactive so it's visually clear at a glance.
            card.Alpha = room.IsActive ? 1.0f : 0.45f;

            activeSwitch.Click += async (_, _) =>
            {
                if (_apiClient is null) return;
                try
                {
                    await _apiClient.SetActiveAsync(room.Name, activeSwitch.Checked);
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    _statusText.Text = $"Hiba: {ex.Message}";
                }
            };

            var onOffText = room.IsOn == true ? "BE" : "KI";
            var realText = room.RealTemperatureCelsius is { } real ? $"{real:0.0}°C" : "n/a";
            var acText = room.AcOwnTemperatureCelsius is { } ac ? $"{ac}°C" : "n/a";
            var modeText = room.CurrentMode ?? "n/a";
            var wattsText = room.CurrentWatts is { } w ? $"⚡ {w:F0}W" : "⚡ n/a";
            readingsView.Text = $"Valós: {realText}  |  Klíma: {acText}  |  {modeText}  |  {onOffText}  |  {wattsText}";

            var scheduleButton = card.FindViewById<Button>(Resource.Id.scheduleButton)!;
            scheduleButton.Click += (_, _) =>
            {
                var intent = new Intent(this, typeof(ScheduleActivity));
                intent.PutExtra(ScheduleActivity.ExtraRoomName, room.Name);
                intent.PutExtra(ScheduleActivity.ExtraServerUrl, _serverUrlInput.Text?.Trim() ?? "");
                StartActivity(intent);
            };

            var powerButton = card.FindViewById<ToggleButton>(Resource.Id.powerButton)!;
            powerButton.Checked = room.IsOn == true;

            powerButton.Click += async (_, _) =>
            {
                if (_apiClient is null) return;
                try
                {
                    await _apiClient.SendPowerAsync(room.Name, powerButton.Checked);
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    _statusText.Text = $"Hiba: {ex.Message}";
                }
            };

            var fanButton = card.FindViewById<ToggleButton>(Resource.Id.fanButton)!;
            // Fan mode (Mod=3) = checked, Cool mode (Mod=1) = unchecked
            fanButton.Checked = room.CurrentMode == "Ventilátor";
            // Fan button only makes sense when AC is on
            fanButton.Enabled = room.IsOn == true;

            fanButton.Click += async (_, _) =>
            {
                if (_apiClient is null) return;
                try
                {
                    var newMod = fanButton.Checked ? 3 : 1; // 3=Fan, 1=Cool
                    await _apiClient.SendModeAsync(room.Name, newMod);
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    _statusText.Text = $"Hiba: {ex.Message}";
                }
            };

            decreaseButton.Click += async (_, _) => await ChangeTargetAsync(room, -0.5);
            increaseButton.Click += async (_, _) => await ChangeTargetAsync(room, +0.5);

            _roomsContainer.AddView(card);
        }
    }

    private async Task ChangeTargetAsync(RoomState room, double delta)
    {
        if (_apiClient is null) return;

        var newTarget = Math.Clamp(room.TargetTemperatureCelsius + delta, 10, 35);
        _statusText.Text = $"{room.Name}: cél állítása {newTarget:0.0}°C-ra...";

        try
        {
            await _apiClient.SetTargetAsync(room.Name, newTarget);
            await RefreshAsync(); // re-fetch so all cards reflect the confirmed new state
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Hiba a cél állításakor: {ex.Message}";
        }
    }

    /// <summary>
    /// Best-effort check that we're on the home WiFi - this is informational only
    /// (shown as a warning), since reading the SSID can be unreliable depending on
    /// Android version/permissions, and we don't want to hard-block the user.
    /// </summary>
    private bool IsOnExpectedWifi(out string ssid)
    {
        ssid = "ismeretlen";
        try
        {
            var wifiManager = (WifiManager?)GetSystemService(WifiService);
            var info = wifiManager?.ConnectionInfo;
            ssid = info?.SSID?.Trim('"') ?? "ismeretlen";
            // No specific expected SSID configured here - this just surfaces what
            // network we're on so the warning text above is informative. If you want
            // a hard check, compare `ssid` to your home network's name here.
            return true;
        }
        catch
        {
            return true; // if we can't read it, don't block the user over it
        }
    }
}