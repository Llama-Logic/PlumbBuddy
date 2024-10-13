namespace PlumbBuddy.Components.Controls;

partial class ManifestEditor
{
    static readonly string[] hashingLevelTickMarkLabels =
    [
        "Permissive (Recommended)",
        "Lenient",
        "Moderate",
        "Strict"
    ];

    [GeneratedRegex(@"^https?://.*")]
    private static partial Regex GetUrlPattern();

    bool addRequiredModGuidanceOpen;
    DirectoryInfo? commonComponentDirectory;
    readonly List<ModComponent> components = [];
    ModComponent? componentsStepSelectedComponent;
    IReadOnlyCollection<ModComponent> componentsStepSelectedComponents = [];
    MudBlazor.SelectionMode componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
    readonly List<TreeItemData<ModComponent?>> componentTreeItemData = [];
    IReadOnlyList<string> creators = [];
    ChipSetField? creatorsChipSetField;
    MudForm? detailsStepForm;
    IReadOnlyList<string> features = [];
    ChipSetField? featuresChipSetField;
    int hashingLevel;
    bool isComposing;
    bool isLoading;
    string loadingText = string.Empty;
    readonly List<ModRequirement> requiredMods = [];
    IReadOnlyList<string> requiredPacks = [];
    ChipSetField? requiredPacksChipSetField;
    bool requiredPacksGuidanceOpen;
    MudForm? requirementsStepForm;
    MudStepperExtended? stepper;
    FileInfo? selectStepFile;
    ModsDirectoryFileType selectStepFileType;
    MudForm? selectStepForm;
    string title = string.Empty;
    string url = string.Empty;
    bool urlProhibitiveGuidanceOpen;
    bool urlEncouragingGuidanceOpen;
    int versionBuild;
    bool versionEnabled;
    int versionMajor;
    int versionMinor;
    int versionRevision;

    ModComponent? ComponentsStepSelectedComponent
    {
        get => componentsStepSelectedComponent;
        set
        {
            if (componentsStepSelectedComponent == value)
                return;
            componentsStepSelectedComponent = value;
            StateHasChanged();
        }
    }

    async Task HandleAddFilesClickedAsync()
    {
        var filesAdded = false;
        foreach (var modFile in await ModFileSelector.SelectModFilesAsync())
            if (!components.Any(component => component.File is { } file && Path.GetFullPath(file.FullName) == Path.GetFullPath(modFile.FullName)))
            {
                var component = new ModComponent(modFile, true, null, null, null, null, null, null, null);
                component.PropertyChanged += HandleComponentPropertyChanged;
                components.Add(component);
                filesAdded = true;
            }
        if (filesAdded)
        {
            UpdateComponentsStructure();
            StateHasChanged();
        }
    }

    async Task HandleCancelOnClickAsync()
    {
        if (await DialogService.ShowCautionDialogAsync("Are you sure you want to cancel?", "I'll discard any changes you've made to this manifest and go back to the beginning of the process."))
            await ResetAsync();
    }

    void HandleComponentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ModComponent.File))
            StateHasChanged();
    }

    async Task HandleDuplicateComponentSettingsClickedAsync()
    {
        if (componentsStepSelectionMode is MudBlazor.SelectionMode.SingleSelection)
        {
            componentsStepSelectionMode = MudBlazor.SelectionMode.MultiSelection;
            StateHasChanged();
        }
        else
        {
            try
            {
                if (componentsStepSelectedComponent is not { } displayedComponent || componentsStepSelectedComponents.Count == 0 || !await DialogService.ShowCautionDialogAsync("This Cannot Be Undone", "I am about to apply the component settings on screen now to every mod file you have checked in the list."))
                    return;
                var componentsToApplySettings = componentsStepSelectedComponents.ToImmutableHashSet();
                foreach (var componentToApplySettings in componentsToApplySettings)
                {
                    componentToApplySettings.IsRequired = displayedComponent.IsRequired;
                    componentToApplySettings.RequirementIdentifier = displayedComponent.RequirementIdentifier;
                    componentToApplySettings.IgnoreIfHashAvailable = displayedComponent.IgnoreIfHashAvailable;
                    componentToApplySettings.IgnoreIfHashUnavailable = displayedComponent.IgnoreIfHashUnavailable;
                    componentToApplySettings.IgnoreIfPackAvailable = displayedComponent.IgnoreIfPackAvailable;
                    componentToApplySettings.IgnoreIfPackUnavailable = displayedComponent.IgnoreIfPackUnavailable;
                    componentToApplySettings.Exclusivity = displayedComponent.Exclusivity;
                }
            }
            finally
            {
                await JSRuntime.InvokeVoidAsync("uncheckAllCheckboxes", ".manifest-editor-components-step-tree-view");
                componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
                StateHasChanged();
            }
        }
    }

    async Task<bool> HandlePreventStepChangeAsync(StepChangeDirection direction, int targetIndex)
    {
        if (direction is StepChangeDirection.Backward)
            return false;
        if (targetIndex == 1 && components.Count == 0)
        {
            await (selectStepForm?.Validate() ?? Task.CompletedTask);
            if (!(selectStepForm?.IsValid ?? false) || selectStepFile is null)
                return true;
            loadingText = $"Examining {(selectStepFileType is ModsDirectoryFileType.ScriptArchive ? "TS4 script archive" : "Maxis DBPF package")}";
            isLoading = true;
            StateHasChanged();
            var selectStepComponent = new ModComponent(selectStepFile, true, null, null, null, null, null, null, null);
            selectStepComponent.PropertyChanged += HandleComponentPropertyChanged;
            components.Add(selectStepComponent);
            UpdateComponentsStructure();
            isLoading = false;
            StateHasChanged();
            return false;
        }
        if (targetIndex == 2)
        {
            if (!components.Any(component => component.IsRequired))
                return true;
        }
        if (targetIndex == 3)
        {
            await (detailsStepForm?.Validate() ?? Task.CompletedTask);
            if (!(detailsStepForm?.IsValid ?? false))
                return true;
            if (creatorsChipSetField is not null)
                await creatorsChipSetField.CommitPendingEntryIfEmptyAsync();
        }
        if (targetIndex == 4)
        {
            await (requirementsStepForm?.Validate() ?? Task.CompletedTask);
            if (!(requirementsStepForm?.IsValid ?? false))
                return true;
            if (requiredPacksChipSetField is not null)
                await requiredPacksChipSetField.CommitPendingEntryIfEmptyAsync();
            foreach (var requiredMod in requiredMods)
            {
                if (requiredMod.CreatorsChipSetField is { } creatorsChipSetField)
                    await creatorsChipSetField.CommitPendingEntryIfEmptyAsync();
                if (requiredMod.HashesChipSetField is { } hashesChipSetField)
                    await hashesChipSetField.CommitPendingEntryIfEmptyAsync();
            }
        }
        if (targetIndex == 5)
        {
            if (featuresChipSetField is not null)
                await featuresChipSetField.CommitPendingEntryIfEmptyAsync();
        }
        if (targetIndex == 7)
        {
            _ = Task.Run(() => Dispatcher.Dispatch(async () =>
            {
                isComposing = true;
                StateHasChanged();
                await Task.Delay(TimeSpan.FromSeconds(10));
                isComposing = false;
                await ResetAsync();
            }));
        }
        return false;
    }

    void HandleQuickSemanticBuildOnClick()
    {
        ++versionBuild;
        versionRevision = 0;
    }

    void HandleQuickSemanticMajorOnClick()
    {
        ++versionMajor;
        versionMinor = 0;
        versionBuild = 0;
        versionRevision = 0;
    }

    void HandleQuickSemanticMinorOnClick()
    {
        ++versionMinor;
        versionBuild = 0;
        versionRevision = 0;
    }

    void HandleQuickSemanticRevisionOnClick() =>
        ++versionRevision;

    async Task HandleRemoveFilesClickedAsync()
    {
        if (componentsStepSelectionMode is MudBlazor.SelectionMode.SingleSelection)
        {
            componentsStepSelectionMode = MudBlazor.SelectionMode.MultiSelection;
            StateHasChanged();
        }
        else
        {
            try
            {
                if (componentsStepSelectedComponents.Count == 0 || !await DialogService.ShowCautionDialogAsync("This Cannot Be Undone", "If I remove the selected components of your mod and you change your mind, you'll need to click cancel on the bottom of the window and start the whole process over again or add them back manually."))
                    return;
                var componentsToRemove = componentsStepSelectedComponents.ToImmutableHashSet();
                components.RemoveAll(componentsToRemove.Contains);
                UpdateComponentsStructure();
                if (componentsStepSelectedComponent is not null && componentsToRemove.Contains(componentsStepSelectedComponent))
                    componentsStepSelectedComponent = null;
            }
            finally
            {
                await JSRuntime.InvokeVoidAsync("uncheckAllCheckboxes", ".manifest-editor-components-step-tree-view");
                componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
                StateHasChanged();
            }
        }
    }

    async Task ResetAsync()
    {
        hashingLevel = 0;
        await (featuresChipSetField?.ClearAsync() ?? Task.CompletedTask);
        versionRevision = 0;
        versionBuild = 0;
        versionMinor = 0;
        versionMajor = 0;
        versionEnabled = false;
        requiredMods.Clear();
        await (requiredPacksChipSetField?.ClearAsync() ?? Task.CompletedTask);
        await (requirementsStepForm?.ResetAsync() ?? Task.CompletedTask);
        url = string.Empty;
        await (creatorsChipSetField?.ClearAsync() ?? Task.CompletedTask);
        title = string.Empty;
        await (detailsStepForm?.ResetAsync() ?? Task.CompletedTask);
        componentsStepSelectedComponent = null;
        componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
        foreach (var component in components)
            component.PropertyChanged -= HandleComponentPropertyChanged;
        components.Clear();
        UpdateComponentsStructure();
        selectStepFileType = ModsDirectoryFileType.Ignored;
        selectStepFile = null;
        await (selectStepForm?.ResetAsync() ?? Task.CompletedTask);
        stepper?.Reset();
        stepper?.ForceRender();
        isLoading = false;
        StateHasChanged();
    }

    void UpdateComponentsStructure()
    {
        var previousCommonComponentDirectoryFullName = commonComponentDirectory is not null
            ? Path.GetFullPath(commonComponentDirectory.FullName)
            : string.Empty;
        var componentPathsSplitBySegment = components
            .Select(component => component.File.FullName)
            .Select(path => path!.Split(Path.DirectorySeparatorChar))
            .ToImmutableArray();
        if (componentPathsSplitBySegment.Length == 0)
            commonComponentDirectory = null;
        else
        {
            var commonPathSegments = componentPathsSplitBySegment[0];
            foreach (var splitPath in componentPathsSplitBySegment.Skip(1))
                commonPathSegments = commonPathSegments
                    .Zip(splitPath, (part1, part2) => part1 == part2 ? part1 : null)
                    .TakeWhile(part => part != null)
                    .ToArray()!;
            var commonPath = Path.Combine(commonPathSegments);
            if (Directory.Exists(commonPath))
                commonComponentDirectory = new(commonPath);
            else if (File.Exists(commonPath))
                commonComponentDirectory = new FileInfo(commonPath).Directory;
            else
                throw new InvalidOperationException("common component directory algorithm failed");
        }
        var currentCommonComponentDirectoryFullName = commonComponentDirectory is not null
            ? Path.GetFullPath(commonComponentDirectory.FullName)
            : string.Empty;
        if (!currentCommonComponentDirectoryFullName.Equals(previousCommonComponentDirectoryFullName, StringComparison.Ordinal))
            componentTreeItemData.Clear();
        static IEnumerable<ModComponent> collectComponentsFromTreeItemData(IReadOnlyList<TreeItemData<ModComponent?>> readOnlyList)
        {
            foreach (var node in readOnlyList)
            {
                if (node.Value is { } component)
                    yield return component;
                else if (node.Children is { } children)
                    foreach (var childComponent in collectComponentsFromTreeItemData(children))
                        yield return childComponent;
            }
        }
        var comparer = PlatformFunctions.FileSystemStringComparison switch
        {
            StringComparison.Ordinal => StringComparer.Ordinal,
            _ => StringComparer.OrdinalIgnoreCase
        };
        TreeItemData<ModComponent?> integrateTreeItemData(IList<TreeItemData<ModComponent?>> list, string text, ModComponent? component)
        {
            var incorporationIndex = 0;
            foreach (var node in list)
            {
                if (comparer.Compare(node.Text, text) < 0)
                {
                    ++incorporationIndex;
                    continue;
                }
                break;
            }
            var treeItem = component is null
                ? new TreeItemData<ModComponent?>
                {
                    Children = [],
                    Expandable = true,
                    Expanded = true,
                    Icon = MaterialDesignIcons.Normal.Folder,
                    Text = text
                }
                : new TreeItemData<ModComponent?>
                {
                    Children = null,
                    Expandable = false,
                    Expanded = false,
                    Icon = component.File.Extension switch
                    {
                        string packageExtension when packageExtension.Equals(".package", StringComparison.OrdinalIgnoreCase) =>
                            MaterialDesignIcons.Normal.PackageVariantClosedCheck,
                        string ts4scriptExtension when ts4scriptExtension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase) =>
                            MaterialDesignIcons.Normal.SourceBranchCheck,
                        _ =>
                            MaterialDesignIcons.Normal.FileCancel
                    },
                    Text = text,
                    Value = component
                };
            list.Insert(incorporationIndex, treeItem);
            return treeItem;
        }
        var incorporatedComponents = collectComponentsFromTreeItemData(componentTreeItemData).ToImmutableArray();
        foreach (var componentToIncorporate in components.Except(incorporatedComponents, ModComponentFullPathEqualityComparer.Default))
        {
            var treeItemData = componentTreeItemData;
            var relativePath = componentToIncorporate.File.FullName[commonComponentDirectory!.FullName.Length..];
            var segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            for (var s = 0; s < segments.Length - 1; ++s)
            {
                var segment = segments[s];
                var treeItem = treeItemData.FirstOrDefault(tid => tid.Text == segment)
                    ?? integrateTreeItemData(treeItemData, segment, null);
                treeItemData = treeItem.Children!;
            }
            integrateTreeItemData(treeItemData, componentToIncorporate.File.Name, componentToIncorporate);
        }
        bool detachComponent(List<TreeItemData<ModComponent?>> list, ModComponent component)
        {
            if (list.RemoveAll(node => node.Value == component) > 0)
                return true;
            foreach (var node in list)
                if (node.Children is { } children && detachComponent(children, component))
                {
                    if (children.Count == 0)
                        list.Remove(node);
                    return true;
                }
            return false;
        }
        foreach (var componentToDetach in incorporatedComponents.Except(components, ModComponentFullPathEqualityComparer.Default))
            detachComponent(componentTreeItemData, componentToDetach);
    }

    IEnumerable<string> ValidateUrl(string url)
    {
        if (url is not null && !GetUrlPattern().IsMatch(url))
            yield return "Enter a valid universal resource locator (URL).";
    }
}
