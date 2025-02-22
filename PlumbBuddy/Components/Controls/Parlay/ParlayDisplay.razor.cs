namespace PlumbBuddy.Components.Controls.Parlay;

partial class ParlayDisplay
{
    [GeneratedRegex(@"\\.")]
    private static partial Regex GetEscapeSequencePattern();

    [GeneratedRegex(@"\{F\d+\..*?\}")]
    private static partial Regex GetFemaleTokenPattern();

    [GeneratedRegex(@"\{M\d+\..*?\}")]
    private static partial Regex GetMaleTokenPattern();

    [GeneratedRegex(@"\{\d+\..*?\}")]
    private static partial Regex GetTokenPattern();

    static MarkupString HighlightForDisplay(string text)
    {
        text = WebUtility.HtmlEncode(text);
        text = GetMaleTokenPattern().Replace(text, $"<span class=\"male-token\">$0</span>");
        text = GetFemaleTokenPattern().Replace(text, $"<span class=\"female-token\">$0</span>");
        text = GetTokenPattern().Replace(text, $"<span class=\"token\">$0</span>");
        text = GetEscapeSequencePattern().Replace(text, $"<span class=\"escape-sequence\">$0</span>");
        return new(text);
    }

    ParlayStringTableEntry? editingEntry;
    string editingEntryTranslation = string.Empty;
    StandaloneCodeEditor? standaloneCodeEditor;
    MudTable<ParlayStringTableEntry>? stringTable;

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

    bool IncludeEntry(ParlayStringTableEntry entry)
    {
        var entrySearchText = Parlay.EntrySearchText;
        if (string.IsNullOrWhiteSpace(entrySearchText))
            return true;
        if (entry.Hash.ToString("x8").Contains(entrySearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (entry.Original.Contains(entrySearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (entry.Translation.Contains(entrySearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    async Task MergeStringTableAsync()
    {
        if (await ModFileSelector.SelectAModFileAsync() is not { } modFile)
            return;
        if (!modFile.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase))
        {
            await DialogService.ShowErrorDialogAsync("File Format Invalid", "The file selected should be a Sims 4 mod package file.");
            return;
        }
        var stringsImported = await Parlay.MergeStringTableAsync(modFile);
        if (stringsImported is 0)
        {
            await DialogService.ShowInfoDialogAsync("No Strings Were Merged", "Did you select the wrong file? It needs to have a string table with the same resource key which is being generated in your current translation in order to work.");
            return;
        }
        await DialogService.ShowInfoDialogAsync("String Merge Complete", $"I successfully merged {"string".ToQuantity(stringsImported)} in the current translation from the file you selected.");
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await JSRuntime.InvokeVoidAsync("initializeMonacoStblSupport");
    }

    async Task ShowCreatorMessageAsync()
    {
        var text = new List<string>();
        if (Parlay.MessageFromCreators is { } messageFromCreators)
            text.Add(messageFromCreators);
        if (Parlay.TranslationSubmissionUrl is { } transmissionSubmissionUrl)
            text.Add($"Translation Submission URL:{Environment.NewLine}{transmissionSubmissionUrl}");
        await DialogService.ShowInfoDialogAsync($"Message to Translators from {Parlay.Creators ?? "Mod Creator"}", string.Join($"{Environment.NewLine}{Environment.NewLine}", text));
    }

    async Task ShowOriginalPackageFileAsync()
    {
        if (Parlay.OriginalPackageFile is { } originalPackageFile)
        {
            originalPackageFile.Refresh();
            if (!originalPackageFile.Exists)
            {
                await DialogService.ShowErrorDialogAsync("Whoops", "The original mod package file has disappeared!");
                return;
            }
            PlatformFunctions.ViewFile(originalPackageFile);
        }
    }

    async Task ShowTranslationPackageFileAsync()
    {
        if (Parlay.TranslationPackageFile is { } translationPackageFile)
        {
            translationPackageFile.Refresh();
            if (!translationPackageFile.Exists)
            {
                await DialogService.ShowErrorDialogAsync("Whoops", "I can't show you the translation override package file because it doesn't exist on disk. Have you clicked on any string table entries to enter and commit a translation yet? If not, then the file's not there because I haven't had a reason to save it.");
                return;
            }
            PlatformFunctions.ViewFile(translationPackageFile);
        }
    }
}
