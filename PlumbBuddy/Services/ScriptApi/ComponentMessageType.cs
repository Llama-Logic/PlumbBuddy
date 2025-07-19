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
    ListScreenshots,
    LookUpLocalizedStrings,
    OpenUrl,
    QueryRelationalDataStorage,
    SendDataToBridgedUi,
    SendLoadedSaveIdentifiersResponse
}
