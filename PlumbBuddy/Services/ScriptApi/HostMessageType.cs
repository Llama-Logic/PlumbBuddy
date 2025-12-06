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
    GamepadButtonChanged,
    GamepadConnected,
    GamepadDisconnected,
    GamepadMoved,
    GamepadsReset,
    GamepadThumbstickChanged,
    GamepadTriggerChanged,
    GetScreenshotDetailsResponse,
    ListScreenshotsResponse,
    LookUpLocalizedStringsResponse,
    RelationalDataStorageQueryResults,
    ScreenshotsChanged,
    SendLoadedSaveIdentifiers
}
