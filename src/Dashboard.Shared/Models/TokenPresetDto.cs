using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Embossing / engraving configuration for either the front or back face of a 3D token.
/// </summary>
public sealed record TokenFaceReliefDto
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    /// <summary>
    /// Positive values emboss (raise above face); negative values engrave (cut into face).
    /// </summary>
    [JsonPropertyName("depthMm")]
    public double DepthMm { get; init; } = 0.8;

    [JsonPropertyName("scale")]
    public double Scale { get; init; } = 0.65;

    [JsonPropertyName("invertHeightmap")]
    public bool InvertHeightmap { get; init; }

    /// <summary>
    /// Optional inline SVG markup or base64 data URI for the face graphic.
    /// </summary>
    [JsonPropertyName("assetDataUri")]
    public string? AssetDataUri { get; init; }

    [JsonPropertyName("assetFileName")]
    public string? AssetFileName { get; init; }
}

/// <summary>
/// Split-body optimization settings for slicing a token along Z = 0 into two flat-bottomed halves with alignment pins/keyways.
/// </summary>
public sealed record SplitBodyOptionsDto
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("pinCount")]
    public int PinCount { get; init; } = 2;

    [JsonPropertyName("pinDiameterMm")]
    public double PinDiameterMm { get; init; } = 3.2;

    [JsonPropertyName("pinHeightMm")]
    public double PinHeightMm { get; init; } = 1.4;

    /// <summary>
    /// Radial & axial clearance added to the female keyway socket for reliable FDM fit.
    /// </summary>
    [JsonPropertyName("clearanceToleranceMm")]
    public double ClearanceToleranceMm { get; init; } = 0.2;

    [JsonPropertyName("partSpacingMm")]
    public double PartSpacingMm { get; init; } = 6.0;
}

/// <summary>
/// Parametric 3D token preset stored under partitionKey <c>"tokens"</c>.
/// </summary>
public sealed record TokenPresetDto : CosmosDocument
{
    [JsonPropertyName("partitionKey")]
    public override string PartitionKey { get; init; } = PartitionKeys.Tokens;

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("diameterMm")]
    public double DiameterMm { get; init; } = 40.0;

    [JsonPropertyName("thicknessMm")]
    public double ThicknessMm { get; init; } = 4.0;

    [JsonPropertyName("rimWidthMm")]
    public double RimWidthMm { get; init; } = 3.0;

    [JsonPropertyName("rimReliefMm")]
    public double RimReliefMm { get; init; } = 0.8;

    [JsonPropertyName("topRimText")]
    public string TopRimText { get; init; } = "MINI APP HUB";

    [JsonPropertyName("bottomRimText")]
    public string BottomRimText { get; init; } = "3D TOKEN";

    [JsonPropertyName("textSizeMm")]
    public double TextSizeMm { get; init; } = 2.4;

    [JsonPropertyName("textDepthMm")]
    public double TextDepthMm { get; init; } = 0.6;

    [JsonPropertyName("frontFace")]
    public TokenFaceReliefDto FrontFace { get; init; } = new();

    [JsonPropertyName("backFace")]
    public TokenFaceReliefDto BackFace { get; init; } = new();

    [JsonPropertyName("splitBody")]
    public SplitBodyOptionsDto SplitBody { get; init; } = new();
}
