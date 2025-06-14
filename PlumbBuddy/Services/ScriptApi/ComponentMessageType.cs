namespace PlumbBuddy.Services.ScriptApi;

public enum ComponentMessageType
{
    Announcement,
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
