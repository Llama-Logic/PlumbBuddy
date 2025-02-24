namespace PlumbBuddy.Components.Controls.Parlay;

partial class ParlayDisplay
{
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await JSRuntime.InvokeVoidAsync("initializeMonacoStblSupport");
    }
}
