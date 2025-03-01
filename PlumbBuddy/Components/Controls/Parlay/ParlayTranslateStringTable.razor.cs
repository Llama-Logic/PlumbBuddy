namespace PlumbBuddy.Components.Controls.Parlay;

partial class ParlayTranslateStringTable
{
    bool skipSettingEditingEntryTranslation;
    StandaloneCodeEditor? standaloneCodeEditor;
    MudTable<ParlayStringTableEntry>? stringTable;

    [Parameter]
    public ReadOnlyObservableCollection<ParlayStringTableEntry> StringTableEntries { get; set; } = new([]);

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
            Value = Parlay.EditingEntryTranslation,
            WordWrap = "on"
        };

    async void HandleEditorOnDidChangeModelContent(ModelContentChangedEvent data)
    {
        if (standaloneCodeEditor is { } nonNullStandaloneCodeEditor)
            Parlay.EditingEntryTranslation = await nonNullStandaloneCodeEditor.GetValue();
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
            || Parlay.EditingEntry is null
            || stringTable is null
            || stringTable.FilteredItems is not { } items)
            return;
        var entries = items.ToImmutableArray();
        var entryIndex = entries.IndexOf(Parlay.EditingEntry);
        var nextEntryIndex = entryIndex + (moveDown ? 1 : -1);
        if (nextEntryIndex < 0 || nextEntryIndex >= entries.Length)
        {
            await JSRuntime.InvokeVoidAsync("mudTableCommitAndMove", "tr:has(div.parlay-editor)", 0);
            return;
        }
        await JSRuntime.InvokeVoidAsync("mudTableCommitAndMove", "tr:has(div.parlay-editor)", nextEntryIndex - entryIndex);
    }

    void HandleStringTableRowEditCommit(object? item)
    {
        if (item is ParlayStringTableEntry entry)
        {
            entry.Translation = Parlay.EditingEntryTranslation;
            Parlay.EditingEntry = null;
        }
    }

    void HandleStringTableOnPreviewEditClick(object? item)
    {
        if (item is ParlayStringTableEntry entry)
        {
            Parlay.EditingEntry = entry;
            if (skipSettingEditingEntryTranslation)
            {
                skipSettingEditingEntryTranslation = false;
                return;
            }
            Parlay.EditingEntryTranslation = Parlay.EditingEntry.Translation;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender
            && stringTable is not null
            && Parlay.EditingEntry is { } editingEntry)
        {
            var editingEntryIndex = StringTableEntries.IndexOf(editingEntry);
            if (editingEntryIndex >= 0)
            {
                skipSettingEditingEntryTranslation = true;
                await JSRuntime.InvokeVoidAsync("mudTableClickRow", ".parlay-string-table", editingEntryIndex);
            }
        }
    }
}
