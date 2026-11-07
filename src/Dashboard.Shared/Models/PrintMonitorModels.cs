using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Operational state of the Bambu Lab A1 3D printer.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrinterOperationalState
{
    Idle,
    Preparing,
    Printing,
    Paused,
    Finished,
    Failed,
    Offline
}

/// <summary>
/// Status of a single AMS Lite filament spool slot on the Bambu Lab A1.
/// </summary>
public sealed record AmsFilamentSlotDto
{
    [JsonPropertyName("slotIndex")]
    public int SlotIndex { get; init; }

    [JsonPropertyName("materialType")]
    public string MaterialType { get; init; } = "PLA";

    [JsonPropertyName("colorHex")]
    public string ColorHex { get; init; } = "#FFFFFF";

    [JsonPropertyName("colorName")]
    public string ColorName { get; init; } = "White";

    [JsonPropertyName("remainingPercent")]
    public int RemainingPercent { get; init; } = 100;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }
}

/// <summary>
/// Real-time status snapshot for the Bambu Lab A1 Print Monitor widget.
/// </summary>
public sealed record PrintStatusSnapshotDto
{
    [JsonPropertyName("printerId")]
    public string PrinterId { get; init; } = "bambu-a1-primary";

    [JsonPropertyName("printerName")]
    public string PrinterName { get; init; } = "Bambu Lab A1";

    [JsonPropertyName("state")]
    public PrinterOperationalState State { get; init; } = PrinterOperationalState.Idle;

    [JsonPropertyName("jobId")]
    public string? JobId { get; init; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; init; }

    [JsonPropertyName("subtaskName")]
    public string? SubtaskName { get; init; }

    [JsonPropertyName("progressPercent")]
    public double ProgressPercent { get; init; }

    [JsonPropertyName("currentLayer")]
    public int CurrentLayer { get; init; }

    [JsonPropertyName("totalLayers")]
    public int TotalLayers { get; init; }

    [JsonPropertyName("elapsedMinutes")]
    public int ElapsedMinutes { get; init; }

    [JsonPropertyName("remainingMinutes")]
    public int RemainingMinutes { get; init; }

    [JsonPropertyName("nozzleTempCelsius")]
    public double NozzleTempCelsius { get; init; }

    [JsonPropertyName("nozzleTargetCelsius")]
    public double NozzleTargetCelsius { get; init; }

    [JsonPropertyName("bedTempCelsius")]
    public double BedTempCelsius { get; init; }

    [JsonPropertyName("bedTargetCelsius")]
    public double BedTargetCelsius { get; init; }

    [JsonPropertyName("speedProfile")]
    public string SpeedProfile { get; init; } = "Standard (100%)";

    [JsonPropertyName("amsSlots")]
    public IReadOnlyList<AmsFilamentSlotDto> AmsSlots { get; init; } = [];

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Historical 3D print job entry stored in Cosmos DB (<c>partitionKey = "print-history"</c>).
/// </summary>
public sealed record PrintHistoryDocument : UserItemDocument
{
    public PrintHistoryDocument()
    {
        PartitionKey = CosmosConstants.PartitionKeys.PrintHistory;
    }

    [JsonPropertyName("printerId")]
    public string PrinterId { get; init; } = "bambu-a1-primary";

    [JsonPropertyName("printerName")]
    public string PrinterName { get; init; } = "Bambu Lab A1";

    [JsonPropertyName("jobId")]
    public required string JobId { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("finalState")]
    public PrinterOperationalState FinalState { get; init; } = PrinterOperationalState.Finished;

    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; init; }

    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset? CompletedAtUtc { get; init; }

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; init; }

    [JsonPropertyName("filamentUsedGrams")]
    public double FilamentUsedGrams { get; init; }

    [JsonPropertyName("materialType")]
    public string MaterialType { get; init; } = "PLA";

    [JsonPropertyName("colorHex")]
    public string ColorHex { get; init; } = "#FFFFFF";

    [JsonPropertyName("snapshot")]
    public PrintStatusSnapshotDto? LatestSnapshot { get; init; }
}
