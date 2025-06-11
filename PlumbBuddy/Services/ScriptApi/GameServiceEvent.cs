namespace PlumbBuddy.Services.ScriptApi;

public enum GameServiceEvent
{
    Load,
    OnAllHouseholdsAndSimInfosLoaded,
    OnCleanupZoneObjects,
    OnZoneLoad,
    OnZoneUnload,
    PreSave,
    Save,
    Setup,
    Start,
    Stop
}