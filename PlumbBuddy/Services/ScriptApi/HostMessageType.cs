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
    LookUpLocalizedStringsResponse,
    RelationalDataStorageQueryResults,
    SendLoadedSaveIdentifiers
}
