namespace PlumbBuddy.Components.Controls;

partial class ChipSetField
{
    MudTextField<string>? entry;
    string entryText = string.Empty;
    readonly string randomClass = $"chip-set-field-{Guid.NewGuid():n}";
    readonly List<string> values = [];

    [Parameter]
    public MudBlazor.Color AdornmentColor { get; set; } = MudBlazor.Color.Default;

    [Parameter]
    public string? AdornmentIcon { get; set; }

    [Parameter]
    public string? ChipIcon { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public MudBlazor.Color Color { get; set; }

    [Parameter]
    public string? HelperText { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public IReadOnlyList<string> Values { get; set; } = [];

    [Parameter]
    public EventCallback<IReadOnlyList<string>> ValuesChanged { get; set; }

    [JSInvokable]
    public async Task AddEntryAsync()
    {
        if (entry is not null && !string.IsNullOrWhiteSpace(entryText))
        {
            values.Add(entryText);
            Values = values.AsReadOnly();
            await ValuesChanged.InvokeAsync(Values);
            entryText = string.Empty;
            StateHasChanged();
            await JSRuntime.InvokeVoidAsync("blurElement", $".{randomClass} input");
            await JSRuntime.InvokeVoidAsync("focusElement", $".{randomClass} input");
        }
    }

    public async Task ClearAsync()
    {
        values.Clear();
        Values = values.AsReadOnly();
        await ValuesChanged.InvokeAsync(Values);
        entryText = string.Empty;
        StateHasChanged();
    }

    public async Task CommitPendingEntryIfEmptyAsync()
    {
        if (values.Count is 0 && !string.IsNullOrWhiteSpace(entryText))
        {
            values.Add(entryText);
            Values = values.AsReadOnly();
            await ValuesChanged.InvokeAsync(Values);
        }
        entryText = string.Empty;
        StateHasChanged();
    }

    async Task HandleChipClosedAsync(MudChip<string> chip)
    {
        if (chip.Text is { } chipText)
        {
            values.Remove(chipText);
            Values = values.AsReadOnly();
            await ValuesChanged.InvokeAsync(Values);
        }
        await JSRuntime.InvokeVoidAsync("focusElement", $".{randomClass} input");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && entry is not null)
            await JSRuntime.InvokeVoidAsync("handleReturnFromDotNet", $".{randomClass}", DotNetObjectReference.Create(this), nameof(AddEntryAsync));
    }

    protected override void OnParametersSet()
    {
        if (!Values.Order().SequenceEqual(values.Order()))
        {
            values.Clear();
            values.AddRange(Values);
            StateHasChanged();
        }
    }

    public void Refresh() =>
        StateHasChanged();
}
