using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Relief operation mode for token text and face graphics.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReliefMode
{
    Emboss,
    Engrave
}

/// <summary>
/// Export layout mode when generating STL files in the browser.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TokenExportLayout
{
    CombinedBuildPlate,
    WholeTokenUnsplit,
    FrontHalfOnly,
    BackHalfOnly
}

/// <summary>
/// Parametric curved text configuration along the top and bottom outer token rims.
/// </summary>
public sealed record CurvedRimTextConfig
{
    [JsonPropertyName("topText")]
    [MaxLength(48)]
    public string TopText { get; init; } = "MINI APP HUB";

    [JsonPropertyName("bottomText")]
    [MaxLength(48)]
    public string BottomText { get; init; } = "2026 EDITION";

    [JsonPropertyName("fontSizeMm")]
    [Range(1.5, 10.0)]
    public double FontSizeMm { get; init; } = 3.2;

    [JsonPropertyName("depthMm")]
    [Range(0.2, 3.0)]
    public double DepthMm { get; init; } = 0.6;

    [JsonPropertyName("mode")]
    public ReliefMode Mode { get; init; } = ReliefMode.Emboss;

    [JsonPropertyName("letterSpacingDegrees")]
    [Range(2.0, 30.0)]
    public double LetterSpacingDegrees { get; init; } = 9.5;

    [JsonPropertyName("applyToBackFace")]
    public bool ApplyToBackFace { get; init; } = true;
}

/// <summary>
/// Embossing/engraving configuration for a single token face (front or back).
/// Supports built-in geometric symbols as well as user-uploaded SVG or heightmap images.
/// </summary>
public sealed record TokenFaceReliefConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("mode")]
    public ReliefMode Mode { get; init; } = ReliefMode.Emboss;

    [JsonPropertyName("depthMm")]
    [Range(0.2, 3.0)]
    public double DepthMm { get; init; } = 0.8;

    [JsonPropertyName("scale")]
    [Range(0.2, 2.0)]
    public double Scale { get; init; } = 1.0;

    [JsonPropertyName("rotationDegrees")]
    [Range(-180.0, 180.0)]
    public double RotationDegrees { get; init; }

    [JsonPropertyName("presetIcon")]
    public string PresetIcon { get; init; } = "star";

    [JsonPropertyName("customAssetDataUrl")]
    public string? CustomAssetDataUrl { get; init; }

    [JsonPropertyName("customAssetFileName")]
    public string? CustomAssetFileName { get; init; }

    [JsonPropertyName("invertHeightmap")]
    public bool InvertHeightmap { get; init; }
}

/// <summary>
/// Split-body print optimization settings for slicing the token along the <c>Z = 0</c> center plane
/// with alignment pins/keyways so both halves print flat on the build plate without supports.
/// </summary>
public sealed record SplitBodyPrintConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("splitPlaneZ")]
    public double SplitPlaneZ { get; init; } = 0.0;

    [JsonPropertyName("alignmentPinCount")]
    [Range(0, 6)]
    public int AlignmentPinCount { get; init; } = 3;

    [JsonPropertyName("pinRadiusMm")]
    [Range(0.8, 4.0)]
    public double PinRadiusMm { get; init; } = 1.5;

    [JsonPropertyName("pinHeightMm")]
    [Range(0.6, 3.0)]
    public double PinHeightMm { get; init; } = 1.2;

    [JsonPropertyName("keywayRadialClearanceMm")]
    [Range(0.05, 0.5)]
    public double KeywayRadialClearanceMm { get; init; } = 0.15;

    [JsonPropertyName("keywayDepthClearanceMm")]
    [Range(0.05, 0.5)]
    public double KeywayDepthClearanceMm { get; init; } = 0.2;

    [JsonPropertyName("pinBoltCircleRatio")]
    [Range(0.3, 0.75)]
    public double PinBoltCircleRatio { get; init; } = 0.55;

    [JsonPropertyName("plateSeparationMm")]
    [Range(2.0, 30.0)]
    public double PlateSeparationMm { get; init; } = 6.0;
}

/// <summary>
/// Complete parametric model for generating a 3D printable token.
/// </summary>
public sealed record TokenParametersDto
{
    [JsonPropertyName("diameterMm")]
    [Range(20.0, 100.0)]
    public double DiameterMm { get; init; } = 40.0;

    [JsonPropertyName("thicknessMm")]
    [Range(2.0, 12.0)]
    public double ThicknessMm { get; init; } = 4.0;

    [JsonPropertyName("rimWidthMm")]
    [Range(1.0, 10.0)]
    public double RimWidthMm { get; init; } = 3.5;

    [JsonPropertyName("rimReliefHeightMm")]
    [Range(0.0, 3.0)]
    public double RimReliefHeightMm { get; init; } = 0.6;

    [JsonPropertyName("radialSegments")]
    [Range(24, 128)]
    public int RadialSegments { get; init; } = 64;

    [JsonPropertyName("rimText")]
    public CurvedRimTextConfig RimText { get; init; } = new();

    [JsonPropertyName("frontFace")]
    public TokenFaceReliefConfig FrontFace { get; init; } = new()
    {
        Enabled = true,
        Mode = ReliefMode.Emboss,
        PresetIcon = "star"
    };

    [JsonPropertyName("backFace")]
    public TokenFaceReliefConfig BackFace { get; init; } = new()
    {
        Enabled = true,
        Mode = ReliefMode.Engrave,
        PresetIcon = "hexagon"
    };

    [JsonPropertyName("splitBody")]
    public SplitBodyPrintConfig SplitBody { get; init; } = new();
}

/// <summary>
/// Saved 3D token preset document stored in Cosmos DB (<c>partitionKey = "tokens"</c>).
/// </summary>
public sealed record TokenPresetDocument : UserItemDocument
{
    public TokenPresetDocument()
    {
        PartitionKey = CosmosConstants.PartitionKeys.Tokens;
    }

    [JsonPropertyName("name")]
    [Required]
    [MaxLength(80)]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    [MaxLength(240)]
    public string? Description { get; init; }

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; init; } = true;

    [JsonPropertyName("parameters")]
    public required TokenParametersDto Parameters { get; init; }
}
