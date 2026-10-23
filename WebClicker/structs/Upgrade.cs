using System.Text.Json.Serialization;
using WebClicker.enums;

namespace WebClicker.structs;

public class Upgrade
{
    [JsonPropertyName("upgradeType")]
    public UpgradeTypes UpgradeType { get; set; }

    [JsonInclude]
    [JsonPropertyName("toggle")]
    public bool Toggle => CanToggle();

    [JsonPropertyName("state")]
    public bool State { get; set; }

    [JsonPropertyName("multilevel")]
    public bool Multilevel { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; } = 0;

    [JsonInclude]
    [JsonPropertyName("cost")]
    public int Cost => GetCostForLevel();

    [JsonInclude]
    [JsonPropertyName("cap")]
    public int Cap => GetLevelCap();

    [JsonConstructor]
    public Upgrade() { }

    public Upgrade(UpgradeTypes upgradeType)
    {
        UpgradeType = upgradeType;
        UpgradeStats stats = Costs[UpgradeType];
        Multilevel = stats.Multi;
    }

    private bool CanToggle()
    {
        UpgradeStats stats = Costs[UpgradeType];
        return stats.Toggle;
    }

    private int GetCostForLevel()
    {
        UpgradeStats stats = Costs[UpgradeType];
        return (int)(stats.Cost * Math.Pow(stats.Scaling, Level));
    }

    private int GetLevelCap()
    {
        UpgradeStats stats = Costs[UpgradeType];
        return stats.Cap;
    }

    private record UpgradeStats(int Cost, bool Toggle, bool Multi, float Scaling, int Cap);

    private static readonly Dictionary<UpgradeTypes, UpgradeStats> Costs = new()
    {
        { UpgradeTypes.Nickname, new(100, false, false, 1.0f, 0) },
        { UpgradeTypes.Multiplier, new(100, true, true, 1.5f, 0) },
        { UpgradeTypes.Theme, new(250, false, true, 1.5f, 6) },
    };
}
