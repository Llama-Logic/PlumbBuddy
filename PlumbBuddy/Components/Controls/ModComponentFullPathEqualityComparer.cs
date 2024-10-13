namespace PlumbBuddy.Components.Controls;

class ModComponentFullPathEqualityComparer :
    EqualityComparer<ModComponent>
{
    public static new ModComponentFullPathEqualityComparer Default { get; } = new();

    public override bool Equals(ModComponent? x, ModComponent? y) =>
        x is null && y is null || x is not null && y is not null && Path.GetFullPath(x.File.FullName).Equals(Path.GetFullPath(y.File.FullName), StringComparison.Ordinal);

    public override int GetHashCode([DisallowNull] ModComponent obj) =>
        obj.File.FullName.GetHashCode(StringComparison.Ordinal);
}
