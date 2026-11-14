using System.Text.Json.Serialization;

namespace QuestDebloater;

public sealed class CatalogRoot
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("deviceTarget")]
    public string DeviceTarget { get; set; } = "";

    [JsonPropertyName("packages")]
    public List<CatalogPackage> Packages { get; set; } = new();
}

public sealed class CatalogPackage
{
    [JsonPropertyName("package")]
    public string Package { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "CAUTION";

    [JsonPropertyName("presets")]
    public List<string> Presets { get; set; } = new();

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}

public sealed class LivePackageEntry
{
    public CatalogPackage Spec { get; set; } = new();
    public bool Installed { get; set; }
    public bool Enabled { get; set; }
    public bool SystemApp { get; set; }
    public double RamMb { get; set; }
    public bool Protected { get; set; }

    public string LiveStatus => !Installed ? "NOT INSTALLED" : (Enabled ? "ACTIVE" : "DISABLED");
}

public sealed class HeadsetInfo
{
    public string Serial { get; set; } = "";
    public string Model { get; set; } = "Meta Quest";
    public string DeviceCode { get; set; } = "";
    public string AndroidVersion { get; set; } = "";
    public string FirmwareDisplay { get; set; } = "";
    public int BatteryLevel { get; set; } = -1;
    public double BatteryTempC { get; set; }
    public double MemTotalMb { get; set; }
    public double MemAvailableMb { get; set; }
    public double MemUsedMb => Math.Max(0, MemTotalMb - MemAvailableMb);
    public string StorageSummary { get; set; } = "";
    public string HomeActivity { get; set; } = "";
    public int TotalInstalledCount { get; set; }
    public int DisabledCount { get; set; }
    public bool GuardianEnabled { get; set; } = true;
}

public sealed class BackupSnapshot
{
    public string TimestampUtc { get; set; } = "";
    public HeadsetInfo Headset { get; set; } = new();
    public List<string> PreviouslyDisabledPackages { get; set; } = new();
    public List<CatalogPackageState> CatalogStates { get; set; } = new();
}

public sealed class CatalogPackageState
{
    public string Package { get; set; } = "";
    public string Tier { get; set; } = "";
    public bool Installed { get; set; }
    public bool Enabled { get; set; }
    public double RamMb { get; set; }
}
