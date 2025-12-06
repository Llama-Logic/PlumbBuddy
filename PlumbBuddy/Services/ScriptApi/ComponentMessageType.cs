namespace PlumbBuddy.Services.ScriptApi;

public enum ComponentMessageType
{
    Announcement,
    BridgedUiDomLoaded,
    BridgedUiLookUp,
    BridgedUiRequest,
    CloseBridgedUi,
    FocusBridgedUi,
    ForegroundGame,
    GameServiceEvent,
    GetScreenshotDetails,
    ListScreenshots,
    LookUpLocalizedStrings,
    OpenUrl,
    QueryRelationalDataStorage,
    SendDataToBridgedUi,
    SendLoadedSaveIdentifiersResponse,
    VibrateGamepad
}
