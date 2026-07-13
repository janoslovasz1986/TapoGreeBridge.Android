using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace TapoGreeBridge.Android;

[Activity(Label = "Időzítők", ScreenOrientation = ScreenOrientation.Portrait)]
public sealed class ScheduleActivity : Activity
{
    public const string ExtraRoomName = "room_name";
    public const string ExtraServerUrl = "server_url";

    private string _roomName = "";
    private BridgeApiClient _apiClient = null!;
    private TextView _statusText = null!;
    private LinearLayout _schedulesContainer = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_schedule);

        _roomName = Intent?.GetStringExtra(ExtraRoomName) ?? "";
        var serverUrl = Intent?.GetStringExtra(ExtraServerUrl) ?? "";
        _apiClient = new BridgeApiClient(serverUrl);

        FindViewById<TextView>(Resource.Id.scheduleTitle)!.Text = $"{_roomName} – Időzítők";
        _statusText = FindViewById<TextView>(Resource.Id.scheduleStatusText)!;
        _schedulesContainer = FindViewById<LinearLayout>(Resource.Id.schedulesContainer)!;

        FindViewById<Button>(Resource.Id.addScheduleButton)!.Click += (_, _) => ShowAddDialog();

        _ = LoadSchedulesAsync();
    }

    private async Task LoadSchedulesAsync()
    {
        _statusText.Text = "Betöltés...";
        try
        {
            var schedules = await _apiClient.GetSchedulesAsync(_roomName);
            RenderSchedules(schedules);
            _statusText.Text = schedules.Count == 0 ? "Nincs időzítő." : "";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Hiba: {ex.Message}";
        }
    }

    private void RenderSchedules(List<ScheduleEntry> schedules)
    {
        _schedulesContainer.RemoveAllViews();
        var inflater = LayoutInflater.FromContext(this)!;

        foreach (var entry in schedules)
        {
            var item = inflater.Inflate(Resource.Layout.schedule_item, _schedulesContainer, false)!;

            var descView = item.FindViewById<TextView>(Resource.Id.scheduleDescription)!;
            var lastExecView = item.FindViewById<TextView>(Resource.Id.scheduleLastExecuted)!;
            var enabledToggle = item.FindViewById<ToggleButton>(Resource.Id.scheduleEnabledToggle)!;
            var deleteButton = item.FindViewById<Button>(Resource.Id.deleteScheduleButton)!;

            descView.Text = FormatSchedule(entry);
            lastExecView.Text = entry.LastExecutedUtc.HasValue
                ? $"Utoljára: {entry.LastExecutedUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}"
                : "Még nem futott le";

            enabledToggle.Checked = entry.IsEnabled;
            enabledToggle.Click += async (_, _) =>
            {
                try
                {
                    await _apiClient.SetScheduleEnabledAsync(entry.Id, enabledToggle.Checked);
                }
                catch (Exception ex)
                {
                    _statusText.Text = $"Hiba: {ex.Message}";
                }
            };

            deleteButton.Click += async (_, _) =>
            {
                try
                {
                    await _apiClient.DeleteScheduleAsync(entry.Id);
                    await LoadSchedulesAsync();
                }
                catch (Exception ex)
                {
                    _statusText.Text = $"Hiba: {ex.Message}";
                }
            };

            _schedulesContainer.AddView(item);
        }
    }

    private static string FormatSchedule(ScheduleEntry entry)
    {
        var timeStr = entry.Type == ScheduleType.Once
            ? entry.ExecuteAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : entry.ExecuteAt.ToLocalTime().ToString("HH:mm") + " (minden nap)";

        var actionStr = entry.Action == ScheduleAction.TurnOff
            ? "⏹ Kikapcsolás"
            : entry.Mode == 1
                ? $"❄️ Hűtés BE – {entry.TargetTemperature:0.0}°C"
                : "💨 Ventilátor BE";

        return $"{timeStr}  |  {actionStr}";
    }

    private void ShowAddDialog()
    {
        // Step 1: Type selection
        new AlertDialog.Builder(this)!
            .SetTitle("Időzítő típusa")!
            .SetItems(["Egyszeri", "Minden nap"], (_, typeArgs) =>
            {
                var type = typeArgs.Which == 0 ? ScheduleType.Once : ScheduleType.Daily;
                ShowTimePicker(type);
            })!
            .Show();
    }

    private void ShowTimePicker(ScheduleType type)
    {
        var now = DateTime.Now;
        var timePicker = new TimePickerDialog(this, (_, timeArgs) =>
        {
            DateTime executeAt;
            if (type == ScheduleType.Once)
            {
                // For Once: show date picker too
                new DatePickerDialog(this, (_, dateArgs) =>
                {
                    executeAt = new DateTime(
                        dateArgs.Year, dateArgs.Month + 1, dateArgs.DayOfMonth,
                        timeArgs.HourOfDay, timeArgs.Minute, 0,
                        DateTimeKind.Local).ToUniversalTime();
                    ShowActionPicker(type, executeAt);
                }, now.Year, now.Month - 1, now.Day)!.Show();
            }
            else
            {
                // For Daily: only time matters, use today's date as placeholder
                executeAt = new DateTime(
                    now.Year, now.Month, now.Day,
                    timeArgs.HourOfDay, timeArgs.Minute, 0,
                    DateTimeKind.Local).ToUniversalTime();
                ShowActionPicker(type, executeAt);
            }
        }, now.Hour, now.Minute, true);

        timePicker.SetTitle(type == ScheduleType.Once ? "Időpont" : "Napszak");
        timePicker.Show();
    }

    private void ShowActionPicker(ScheduleType type, DateTime executeAt)
    {
        new AlertDialog.Builder(this)!
            .SetTitle("Művelet")!
            .SetItems(["⏹ Kikapcsolás", "❄️ Hűtés BE", "💨 Ventilátor BE"], (_, actionArgs) =>
            {
                switch (actionArgs.Which)
                {
                    case 0: // TurnOff
                        _ = SaveScheduleAsync(new CreateScheduleRequest
                        {
                            Type = type,
                            ExecuteAt = executeAt,
                            Action = ScheduleAction.TurnOff
                        });
                        break;
                    case 1: // TurnOn + Cool
                        ShowTempPicker(type, executeAt);
                        break;
                    case 2: // TurnOn + Fan
                        _ = SaveScheduleAsync(new CreateScheduleRequest
                        {
                            Type = type,
                            ExecuteAt = executeAt,
                            Action = ScheduleAction.TurnOn,
                            Mode = 3
                        });
                        break;
                }
            })!
            .Show();
    }

    private void ShowTempPicker(ScheduleType type, DateTime executeAt)
    {
        var temps = Enumerable.Range(16, 15).Select(t => $"{t}°C").ToArray();

        new AlertDialog.Builder(this)!
            .SetTitle("Cél hőmérséklet")!
            .SetItems(temps, (_, tempArgs) =>
            {
                var temp = 16 + tempArgs.Which;
                _ = SaveScheduleAsync(new CreateScheduleRequest
                {
                    Type = type,
                    ExecuteAt = executeAt,
                    Action = ScheduleAction.TurnOn,
                    Mode = 1,
                    TargetTemperature = temp
                });
            })!
            .Show();
    }

    private async Task SaveScheduleAsync(CreateScheduleRequest request)
    {
        try
        {
            await _apiClient.CreateScheduleAsync(_roomName, request);
            await LoadSchedulesAsync();
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Hiba: {ex.Message}";
        }
    }
}