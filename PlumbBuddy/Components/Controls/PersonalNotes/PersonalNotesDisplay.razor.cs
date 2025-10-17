namespace PlumbBuddy.Components.Controls.PersonalNotes;

partial class PersonalNotesDisplay
{
    MudTable<PersonalNotesRecord>? recordsTable;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
            PersonalNotes.DataAltered -= HandledPersonalNotesDataAltered;
            PersonalNotes.PropertyChanged -= HandlePersonalNotesPropertyChanged;
        }
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State)
            && ModsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
            recordsTable?.ReloadServerData();
    }

    void HandledPersonalNotesDataAltered(object? sender, EventArgs e) =>
        recordsTable?.ReloadServerData();

    void HandlePersonalNotesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPersonalNotes.SearchText)
            or nameof(IPersonalNotes.FileDateLowerBound)
            or nameof(IPersonalNotes.FileDateUpperBound)
            or nameof(IPersonalNotes.ModFilesDateLowerBound)
            or nameof(IPersonalNotes.ModFilesDateUpperBound)
            or nameof(IPersonalNotes.PersonalDateLowerBound)
            or nameof(IPersonalNotes.PersonalDateUpperBound)
            or nameof(IPersonalNotes.PlayerDataDateLowerBound)
            or nameof(IPersonalNotes.PlayerDataDateUpperBound))
            recordsTable?.ReloadServerData();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        PersonalNotes.DataAltered += HandledPersonalNotesDataAltered;
        PersonalNotes.PropertyChanged += HandlePersonalNotesPropertyChanged;
    }

    void ViewFile(FileInfo file)
    {
        if (!file.Exists)
        {
            SuperSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundDisplay_Snack_Error_CannotViewRemovedFile), Severity.Error, options =>
            {
                options.Icon = MaterialDesignIcons.Normal.FileAlert;
                options.RequireInteraction = true;
            });
            return;
        }
        PlatformFunctions.ViewFile(file);
    }
}
