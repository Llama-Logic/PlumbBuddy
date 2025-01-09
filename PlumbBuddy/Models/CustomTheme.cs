namespace PlumbBuddy.Models;

public class CustomTheme
{
    public bool CustomAppLogo { get; set; }

    public required string DisplayName { get; set; }

    public required string Description { get; set; }

    public string? DefaultBorderRadius { get; set; }

    public string? Font { get; set; }

    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Serialization")]
    public Dictionary<string, string>? PaletteLight { get; set; }

    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Serialization")]
    public Dictionary<string, string>? PaletteDark { get; set; }

    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Serialization")]
    public Dictionary<string, Dictionary<string, string?>?>? BackgroundedTabs { get; set; }
}
