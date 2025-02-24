namespace PlumbBuddy.Components.Controls.Parlay;

partial class ParlayReadStringTable
{
    [Parameter]
    public IEnumerable<ParlayStringTableEntry> StringTableEntries { get; set; } = [];

    protected override void ConfigureBindings()
    {
        base.ConfigureBindings();
        Observed(() => Parlay.EntrySearchText);
    }
}
