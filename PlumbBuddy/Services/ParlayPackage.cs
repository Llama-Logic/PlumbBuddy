namespace PlumbBuddy.Services;

public record ParlayPackage(string ModFilePath, string? ManifestedName, string? ManifestedCreators, string? ManifestedVersion, IReadOnlyList<ParlayStringTable> StringTables)
{
    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(ManifestedName))
            return $"Unnamed Mod at {ModFilePath}";
        return $"{ManifestedName}{(string.IsNullOrWhiteSpace(ManifestedVersion) ? string.Empty : $" ({ManifestedVersion})")}{(string.IsNullOrWhiteSpace(ManifestedCreators) ? string.Empty : $" by {ManifestedCreators}")} at {ModFilePath}";
    }
}
