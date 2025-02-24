namespace PlumbBuddy.Components.Controls.Parlay;

partial class ParlayTranslateStringTable
{
    ParlayStringTableEntry? editingEntry;
    string editingEntryTranslation = string.Empty;
    StandaloneCodeEditor? standaloneCodeEditor;
    MudTable<ParlayStringTableEntry>? stringTable;

    [Parameter]
    public IEnumerable<ParlayStringTableEntry> StringTableEntries { get; set; } = [];

    protected override void ConfigureBindings()
    {
        base.ConfigureBindings();
        Observed(() => Parlay.EntrySearchText);
    }

    StandaloneEditorConstructionOptions CreateEditorOptions(StandaloneCodeEditor editor) =>
        new()
        {
            AutomaticLayout = true,
            BracketPairColorization = new BracketPairColorizationOptions { Enabled = true },
            Folding = false,
            GlyphMargin = false,
            Language = "ts4-stbl",
            LineNumbers = "off",
            Minimap = new EditorMinimapOptions { Enabled = false },
            RenderLineHighlight = "none",
            Theme = "ts4-stbl-theme",
            Value = editingEntryTranslation,
            WordWrap = "on"
        };

    async void HandleEditorOnDidChangeModelContent(ModelContentChangedEvent data)
    {
        if (standaloneCodeEditor is { } nonNullStandaloneCodeEditor)
        {
            standaloneCodeEditor = nonNullStandaloneCodeEditor;
            editingEntryTranslation = await standaloneCodeEditor.GetValue();
        }
    }

    async Task HandleEditorOnDidInitAsync()
    {
        await JSRuntime.InvokeVoidAsync("scrollToCenterElement", ".parlay-string-table .mud-table-container", ".parlay-editor");
        if (standaloneCodeEditor is { } editor)
            await editor.Focus();
    }

    async void HandleEditorKeyUp(BlazorMonaco.KeyboardEvent keyboardEvent)
    {
        var moveDown = keyboardEvent.KeyCode is BlazorMonaco.KeyCode.PageDown;
        var moveUp = keyboardEvent.KeyCode is BlazorMonaco.KeyCode.PageUp;
        if (!(moveDown || moveUp)
            || editingEntry is null
            || stringTable is null
            || stringTable.FilteredItems is not { } items)
            return;
        var entries = items.ToImmutableArray();
        var entryIndex = entries.IndexOf(editingEntry);
        var nextEntryIndex = entryIndex + (moveDown ? 1 : -1);
        if (nextEntryIndex < 0 || nextEntryIndex >= entries.Length)
        {
            await JSRuntime.InvokeVoidAsync("mudTableCommitAndMove", "tr:has(div.parlay-editor)", 0);
            return;
        }
        await JSRuntime.InvokeVoidAsync("mudTableCommitAndMove", "tr:has(div.parlay-editor)", nextEntryIndex - entryIndex);
    }

    void HandleStringTableRowEditCancel(object? item) =>
        editingEntry = null;

    void HandleStringTableRowEditCommit(object? item)
    {
        if (item is ParlayStringTableEntry entry)
        {
            entry.Translation = editingEntryTranslation;
            editingEntry = null;
        }
    }

    void HandleStringTableOnPreviewEditClick(object? item)
    {
        if (item is ParlayStringTableEntry entry)
        {
            editingEntry = entry;
            editingEntryTranslation = editingEntry.Translation;
        }
    }
}
