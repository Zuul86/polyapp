using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Represents a Bambu Lab A1 filament slot (AMS Lite / External spool).
/// </summary>
public sealed record FilamentSlotDto
{
    [JsonPropertyName("slotIndex")]
    public int SlotIndex { get; init; }

    [JsonPropertyName("material")]
    public string Material { get; init; } = "PLA";

    [JsonPropertyName("colorHex")]
    public string ColorHex { get; init; } = "#00AE42";

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }
}

/// <summary>
/// Represents a Bambu Lab A1 print status snapshot or print history log entry stored under partitionKey <c>"print-history"</c>.
/// </summary>
public sealed record PrintStatusDto : CosmosDocument
{
    [JsonPropertyName("partitionKey")]
    public override string PartitionKey { get; init; } = PartitionKeys.PrintHistory;

    [JsonPropertyName("printerName")]
    public string PrinterName { get; init; } = "Bambu Lab A1";

    [JsonPropertyName("state")]
    public string State { get; init; } = "Idle";

    [JsonPropertyName("jobName")]
    public string JobName { get; init; } = "None";

    [JsonPropertyName("progressPercent")]
    public int ProgressPercent { get; init; }

    [JsonPropertyName("currentLayer")]
    public int CurrentLayer { get; init; }

    [JsonPropertyName("totalLayers")]
    public int TotalLayers { get; init; }

    [JsonPropertyName("remainingMinutes")]
    public int RemainingMinutes { get; init; }

    [JsonPropertyName("nozzleTempCelsius")]
    public double NozzleTempCelsius { get; init; }

    [JsonPropertyName("bedTempCelsius")]
    public double BedTempCelsius { get; init; }

    [JsonPropertyName("filamentSlots")]
    public IReadOnlyList<FilamentSlotDto> FilamentSlots { get; init; } = [];
}
