namespace PlumbBuddy.Components.Controls;

partial class ModComponentEditor
{
    bool exclusivityPopoverOpen;
    ModComponent? lastModComponent;
    bool requirementIdentifierPopoverOpen;

    [Parameter]
    public string? Exclusivity { get; set; }

    [Parameter]
    public FileInfo? File { get; set; }

    [Parameter]
    public string? IgnoreIfHashAvailable { get; set; }

    [Parameter]
    public string? IgnoreIfHashUnavailable { get; set; }

    [Parameter]
    public string? IgnoreIfPackAvailable { get; set; }

    [Parameter]
    public string? IgnoreIfPackUnavailable { get; set; }

    [Parameter]
    public bool IsRequired { get; set; }

    [Parameter]
    public ModComponent? ModComponent { get; set; }

    [Parameter]
    public string? RequirementIdentifier { get; set; }

    void HandleExclusivityChanged(string? newValue)
    {
        Exclusivity = newValue;
        if (ModComponent is { } modComponent)
            modComponent.Exclusivity = newValue;
    }

    void HandleFileChanged(FileInfo? newValue)
    {
        File = newValue;
        if (ModComponent is { } modComponent && newValue is { } nonNullNewValue)
            modComponent.File = nonNullNewValue;
    }

    void HandleIgnoreIfHashAvailableChanged(string? newValue)
    {
        IgnoreIfHashAvailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfHashAvailable = newValue;
    }

    void HandleIgnoreIfHashUnavailableChanged(string? newValue)
    {
        IgnoreIfHashUnavailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfHashUnavailable = newValue;
    }

    void HandleIgnoreIfPackAvailableChanged(string? newValue)
    {
        IgnoreIfPackAvailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfPackAvailable = newValue;
    }

    void HandleIgnoreIfPackUnavailableChanged(string? newValue)
    {
        IgnoreIfPackUnavailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfPackUnavailable = newValue;
    }

    void HandleIsRequiredChanged(bool newValue)
    {
        IsRequired = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IsRequired = newValue;
    }

    void HandleRequirementIdentifierChanged(string? newValue)
    {
        RequirementIdentifier = newValue;
        if (ModComponent is { } modComponent)
            modComponent.RequirementIdentifier = newValue;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ModComponent != lastModComponent)
        {
            lastModComponent = ModComponent;
            await SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(Exclusivity), lastModComponent?.Exclusivity },
                { nameof(File), lastModComponent?.File },
                { nameof(IgnoreIfPackAvailable), lastModComponent?.IgnoreIfPackAvailable },
                { nameof(IgnoreIfPackUnavailable), lastModComponent?.IgnoreIfPackUnavailable },
                { nameof(IgnoreIfHashAvailable), lastModComponent?.IgnoreIfHashAvailable },
                { nameof(IgnoreIfHashUnavailable), lastModComponent?.IgnoreIfHashUnavailable },
                { nameof(IsRequired), lastModComponent?.IsRequired },
                { nameof(RequirementIdentifier), lastModComponent?.RequirementIdentifier }
            }));
        }
    }
}
