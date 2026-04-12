namespace Keuken_inmeten.Services;

using System.Text.Json.Serialization;

public static partial class KeukenShareCodec
{
    private sealed class CompactShareData
    {
        [JsonPropertyName("v")]
        public int? SchemaVersion { get; set; }

        [JsonPropertyName("w")]
        public List<CompactWall>? Walls { get; set; }

        [JsonPropertyName("k")]
        public List<CompactCabinet>? Cabinets { get; set; }

        [JsonPropertyName("a")]
        public List<CompactAppliance>? Appliances { get; set; }

        [JsonPropertyName("t")]
        public List<CompactPanel>? Panels { get; set; }

        [JsonPropertyName("p")]
        public double? LastCupOffset { get; set; }

        [JsonPropertyName("r")]
        public double? PanelEdgeClearance { get; set; }
    }

    private sealed class CompactWall
    {
        [JsonPropertyName("n")]
        public string? Name { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("p")]
        public double? PlinthHeight { get; set; }

        [JsonPropertyName("k")]
        public List<int>? CabinetIndexes { get; set; }

        [JsonPropertyName("a")]
        public List<int>? ApplianceIndexes { get; set; }
    }

    private sealed class CompactCabinet
    {
        [JsonPropertyName("n")]
        public string? Name { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("d")]
        public double? Depth { get; set; }

        [JsonPropertyName("w")]
        public double? WallThickness { get; set; }

        [JsonPropertyName("g")]
        public double? HoleSpacing { get; set; }

        [JsonPropertyName("e")]
        public double? FirstHoleBelowTopShelf { get; set; }

        [JsonPropertyName("x")]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        public double? FloorHeight { get; set; }

        [JsonPropertyName("p")]
        public List<double>? ShelfHeights { get; set; }
    }

    private sealed class CompactAppliance
    {
        [JsonPropertyName("n")]
        public string? Name { get; set; }

        [JsonPropertyName("t")]
        public int? Type { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("d")]
        public double? Depth { get; set; }

        [JsonPropertyName("x")]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        public double? FloorHeight { get; set; }
    }

    private sealed class CompactPanel
    {
        [JsonPropertyName("c")]
        public List<int>? CabinetIndexes { get; set; }

        [JsonPropertyName("t")]
        public int? Type { get; set; }

        [JsonPropertyName("s")]
        public int? HingeSide { get; set; }

        [JsonPropertyName("p")]
        public double? CupOffset { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("x")]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        public double? FloorHeight { get; set; }

        [JsonPropertyName("q")]
        public int? VerificationStatus { get; set; }
    }
}
