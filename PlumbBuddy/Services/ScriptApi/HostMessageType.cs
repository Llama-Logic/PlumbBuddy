namespace PlumbBuddy.Services.ScriptApi;

public enum HostMessageType
{
    BridgedUiAnnouncement,
    BridgedUiData,
    BridgedUiDestroyed,
    BridgedUiLookUpResponse,
    BridgedUiRequestResponse,
    FocusBridgedUiResponse,
    ForegroundPlumbbuddy,
    ListScreenshotsResponse,
    LookUpLocalizedStringsResponse,
    RelationalDataStorageQueryResults,
    ScreenshotsChanged,
    SendLoadedSaveIdentifiers
}
