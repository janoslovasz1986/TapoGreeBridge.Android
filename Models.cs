using System.Text.Json.Serialization;

namespace TapoGreeBridge.Android;

public sealed class RoomState
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("targetTemperatureCelsius")]
    public double TargetTemperatureCelsius { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("realTemperatureCelsius")]
    public double? RealTemperatureCelsius { get; set; }

    [JsonPropertyName("acOwnTemperatureCelsius")]
    public int? AcOwnTemperatureCelsius { get; set; }

    [JsonPropertyName("isOn")]
    public bool? IsOn { get; set; }

    [JsonPropertyName("currentSetTem")]
    public int? CurrentSetTem { get; set; }

    [JsonPropertyName("currentMode")]
    public string? CurrentMode { get; set; }

    [JsonPropertyName("currentWatts")]
    public double? CurrentWatts { get; set; }

    [JsonPropertyName("lastUpdatedUtc")]
    public DateTime? LastUpdatedUtc { get; set; }
}

public sealed class TargetRequest
{
    [JsonPropertyName("celsius")]
    public double Celsius { get; set; }
}

public sealed class ModeRequest
{
    [JsonPropertyName("mod")]
    public int Mod { get; set; }
}

public sealed class PowerRequest
{
    [JsonPropertyName("on")]
    public bool On { get; set; }
}

public enum ScheduleType { Once, Daily }
public enum ScheduleAction { TurnOff, TurnOn }

public sealed class ScheduleEntry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("roomName")]
    public string RoomName { get; set; } = "";

    [JsonPropertyName("type")]
    public ScheduleType Type { get; set; }

    [JsonPropertyName("executeAt")]
    public DateTime ExecuteAt { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("lastExecutedUtc")]
    public DateTime? LastExecutedUtc { get; set; }

    [JsonPropertyName("action")]
    public ScheduleAction Action { get; set; }

    [JsonPropertyName("mode")]
    public int? Mode { get; set; }

    [JsonPropertyName("targetTemperature")]
    public double? TargetTemperature { get; set; }
}

public sealed class CreateScheduleRequest
{
    [JsonPropertyName("type")]
    public ScheduleType Type { get; set; }

    [JsonPropertyName("executeAt")]
    public DateTime ExecuteAt { get; set; }

    [JsonPropertyName("action")]
    public ScheduleAction Action { get; set; }

    [JsonPropertyName("mode")]
    public int? Mode { get; set; }

    [JsonPropertyName("targetTemperature")]
    public double? TargetTemperature { get; set; }
}

public sealed class ActiveRequest
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
}