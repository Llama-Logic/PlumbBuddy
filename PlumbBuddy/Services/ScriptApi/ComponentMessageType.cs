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
    LookUpLocalizedModStrings,
    OpenUrl,
    QueryRelationalDataStorage,
    SendDataToBridgedUi,
    SendLoadedSaveIdentifiersResponse
}
