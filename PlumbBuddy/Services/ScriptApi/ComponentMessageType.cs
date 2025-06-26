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
    QueryRelationalDataStorage,
    SendDataToBridgedUi,
    SendLoadedSaveIdentifiersResponse
}
