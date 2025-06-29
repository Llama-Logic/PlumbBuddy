import base64
from typing import Callable, Dict, List, Optional, Sequence, Tuple
from plumbbuddy_proxy.asynchronous import listen_for, Event, Eventual
from distributor.shared_messages import IconInfoData
from plumbbuddy_proxy.utilities import inject_to
from plumbbuddy_proxy.ipc_client import ipc
from plumbbuddy_proxy import logger
import os
from clock import ServerClock
from datetime import timedelta
import services
import subprocess
import sys
from ui.ui_dialog_notification import UiDialogNotification
from uuid import uuid4, UUID
import webbrowser

def _attach_save_characteristics(ipc_message: dict) -> dict:
    try:
        get_persistence_service = getattr(services, 'get_persistence_service', None)
        get_time_service = getattr(services, 'time_service', None)
        if not callable(get_persistence_service) or not callable(get_time_service):
            return ipc_message
        persistence_service = get_persistence_service()
        account = persistence_service.get_account_proto_buff()
        nucleus_id = getattr(account, 'nucleus_id', None)
        if nucleus_id is None:
            return ipc_message
        created = getattr(account, 'created', None)
        if created is None:
            return ipc_message
        time_service = None
        try:
            time_service = get_time_service()
        except:
            pass
        if time_service is None:
            return ipc_message
        sim_now = getattr(time_service, 'sim_now', None)
        if sim_now is None:
            return ipc_message
        save_slot = persistence_service.get_save_slot_proto_buff()
        slot_id = getattr(save_slot, 'slot_id', None)
        if slot_id is None:
            return ipc_message
        return {
            **ipc_message,
            'nucleus_id': nucleus_id,
            'created': created,
            'sim_now': int(sim_now),
            'slot_id': slot_id
        }
    except Exception as ex:
        logger.exception(ex)
        return ipc_message

def _try_to_foreground_plumbbuddy():
    if os.name == 'nt':
        webbrowser.open('plumbbuddy://focus')
    elif os.name == 'posix' and sys.platform == 'darwin':
        subprocess.run(['/usr/bin/osascript', '-e', 'tell application id "com.llamalogic.plumbbuddy" to activate'])

class BridgedUi:
    """
    A PlumbBuddy Runtime Mod Integration Bridged UI
    """

    def __init__(self, unique_id: UUID, receive_dispatches: Callable[[dict], None]):
        self._unique_id = unique_id
        self._eventual_focus: Eventual[bool] = None
        dispatches = {}
        def set_dispatch_announcement(dispatch: Callable[[any], None]):
            dispatches['announcement'] = dispatch
        self._annoucement: Event[any] = Event(set_dispatch_announcement)
        def set_dispatch_destroyed(dispatch: Callable[[any], None]):
            dispatches['destroyed'] = dispatch
        self._destroyed: Event[any] = Event(set_dispatch_destroyed)
        receive_dispatches(dispatches)
    
    @property
    def announcement(self) -> Event[any]:
        """
        Gets the event which is dispatched when this bridged UI makes an announcement; event data will be what was announced
        """

        return self._annoucement
    
    @property
    def destroyed(self) -> Event[any]:
        """
        Gets the event which is dispatched when this bridged UI has been destroyed for any reason
        """

        return self._destroyed
    
    def close(self) -> None:
        """
        Closes the bridged UI
        """

        ipc.send({
            'type': 'close_bridged_ui',
            'unique-id': str(self._unique_id)
        })

    def focus(self) -> Eventual[bool]:
        """
        Attempts to focus the bridged UI

        :returns: an Eventual that will resolve with a bool indicating whether the bridged UI was focused
        """

        if self._eventual_focus is not None:
            return self._eventual_focus
        eventual = Eventual[bool]()
        ipc.send({
            'type': 'focus_bridged_ui',
            'unique_id': str(self._unique_id)
        })
        self._eventual_focus = eventual
        return eventual
    
    def send_data(self, data) -> None:
        """
        Sends data to the bridged UI

        :param data: the data to be send
        """

        ipc.send({
            'type': 'send_data_to_bridged_ui',
            'recipient': str(self._unique_id),
            'data': data
        })

class RelationalDataStorageQueryRecordSet:
    """
    A PlumbBuddy Runtime Mod Integration Relational Data Storage Query Record Set
    """

    def __init__(self, record_set_message_excerpt: dict):
        self._field_names = tuple(record_set_message_excerpt['field_names'])
        records = []
        for record in record_set_message_excerpt['records']:
            values = []
            for value in record:
                if isinstance(value, dict) and 'base64' in value:
                    value = base64.b64decode(value['base64'])
                values.append(value)
            records.append(tuple(values))
        self._records = tuple(records)
    
    @property
    def field_names(self) -> Sequence[str]:
        """
        Gets the names of the fields of the record set
        """

        return self._field_names
    
    @property
    def records(self) -> Sequence[Tuple]:
        """
        Gets the records of the record set
        """

        return self._records

class RelationalDataStorageQueryCompletedEventData:
    """
    Data for the query_completed event of a PlumbBuddy Runtime Mod Integration Relational Data Storage Connection
    """

    def __init__(self, response_message: dict):
        self._error_code: int = response_message['error_code']
        self._error_message: str = response_message['error_message']
        self._execution_time = timedelta(response_message['execution_seconds'])
        self._extended_error_code: int = response_message['extended_error_code']
        self._query_id = UUID(response_message['query_id'])
        record_sets = []
        for response_record_set in response_message['record_sets']:
            record_sets.append(RelationalDataStorageQueryRecordSet(response_record_set))
        self._record_sets = tuple(record_sets)
        self._tag: str = response_message['tag']
    
    @property
    def error_code(self) -> int:
        """
        Gets the SQLite error code raised by the query (see: https://www.sqlite.org/rescode.html); -1 if another type of error occured; otherwise, 0
        """

        return self._error_code
    
    @property
    def error_message(self) -> str:
        """
        Gets the message of the error if one was raised; otherwise, None
        """

        return self._error_message
    
    @property
    def execution_time(self) -> timedelta:
        """
        Gets the duration of time for which the query was executing
        """

        return self._execution_time
    
    @property
    def extended_error_code(self) -> int:
        """
        Gets the SQLite error code raised by the query (see: https://www.sqlite.org/rescode.html#extrc); -1 if another type of error occured; otherwise, 0
        """

        return self._extended_error_code
    
    @property
    def query_id(self) -> UUID:
        """
        Gets the UUID which was assigned to the query when execute was called on the connection
        """

        return self._query_id
    
    @property
    def record_sets(self) -> Sequence[RelationalDataStorageQueryRecordSet]:
        """
        Gets the record sets resulting from the query
        """

        return self._record_sets
    
    @property
    def tag(self) -> str:
        """
        Gets the tag string if one was provided when execute was called on the connection; otherwise, None
        """

        return self._tag

class RelationalDataStorage:
    """
    A PlumbBuddy Runtime Mod Integration Relational Data Storage Connection
    """

    def __init__(self, unique_id: UUID, is_save_specific: bool, receive_dispatches: Callable[[dict], None]):
        self._unique_id = unique_id
        self._is_save_specific = is_save_specific
        dispatches = {}
        def set_dispatch_query_completed(dispatch: Callable[[RelationalDataStorageQueryCompletedEventData], None]):
            dispatches['query_completed'] = dispatch
        self._query_completed: Event[RelationalDataStorageQueryCompletedEventData] = Event(set_dispatch_query_completed)
        receive_dispatches(dispatches)

    @property
    def unique_id(self) -> UUID:
        """
        Gets the UUID of the relational data
        """

        return self._unique_id
    
    @property
    def is_save_specific(self) -> bool:
        """
        Gets whether the relational data is specific to the currently open save file
        """

        return self._is_save_specific
    
    @property
    def query_completed(self) -> Event[RelationalDataStorageQueryCompletedEventData]:
        """
        Gets the event which is dispatched when a query for this connection has been completed
        """

        return self._query_completed
    
    def execute(self, sql: str, tag: str = None, parameters: dict = None) -> UUID:
        """
        Executes a query with this connection

        :param sql: SQLite query
        :param tag: (optional) a tag to associate with the query, making its results easier to identify by other components
        :param parameters: the parameters to replace instances of '?' with in sql
        :returns: the UUID which has been assigned to the query
        """

        if not sql:
            raise ValueError('sql must not be empty')
        if not isinstance(sql, str):
            raise TypeError('sql must be a str')
        if not ipc.is_connected:
            raise PlumbBuddyNotConnectedError()
        query_id = uuid4()
        serialization_safe_parameters = {}
        if parameters and isinstance(parameters, dict):
            for key, value in parameters.items():
                serialization_safe_parameters[key] = { 'base64': base64.b64encode(value).decode('utf-8') } if isinstance(value, bytes) else value
        ipc.send({
            'is_save_specific': self.is_save_specific,
            'parameters': serialization_safe_parameters,
            'query': sql,
            'query_id': str(query_id),
            'tag': str(tag) if tag else None,
            'type': 'query_relational_data_storage',
            'unique_id': str(self.unique_id)
        })
        return query_id

class PlumbBuddyNotConnectedError(Exception):
    def __init__(self):
        super().__init__('PlumbBuddy is either not running or having some sort of connection problem')

class ScriptModNotFoundError(Exception):
    def __init__(self):
        super().__init__('The script mod specified could not be found')

class IndexNotFoundError(Exception):
    def __init__(self):
        super().__init__('The required index.html file could not be found in that script mod at the specified UI root location')

class PlayerDeniedRequestError(Exception):
    def __init__(self):
        super().__init__('The player has denied your request to display that bridged UI')

class InvalidHostNameError(Exception):
    def __init__(self):
        super().__init__('The host name you specified is not legal by the DNS standard - use only letters from the Latin alphabet, Arabic numerals, and the dash (-)')

class BridgedUiNotFoundError(Exception):
    def __init__(self):
        super().__init__('The referenced bridged UI is not currently loaded')

class Gateway:
    """
    The PlumbBuddy Runtime Mod Integration Gateway
    """

    def __init__(self):
        self._global_relational_data_stores: Dict[UUID, Tuple[RelationalDataStorage, dict]] = {}
        self._save_specific_relational_data_stores: Dict[UUID, Tuple[RelationalDataStorage, dict]] = {}
        self._reset_bridged_ui_cache()

        self._dispatch_is_connected_changed: Callable[[bool], None] = lambda _: None
        def set_dispatch_is_connected_changed(dispatch: Callable[[bool], None]):
            self._dispatch_is_connected_changed = dispatch
        self._is_connected_changed: Event[bool] = Event(set_dispatch_is_connected_changed)

        @listen_for(ipc.connection_state_changed)
        def handle_ipc_connection_state_changed(connection_state):
            if connection_state == 0:
                requested_bridged_uis = getattr(self, '_requested_bridged_uis', None)
                if requested_bridged_uis is not None:
                    for eventuals_list in requested_bridged_uis.values():
                        for eventual in eventuals_list:
                            eventual._set_fault(PlumbBuddyNotConnectedError())
                bridged_ui_look_ups = getattr(self, '_bridged_ui_look_ups', None)
                if bridged_ui_look_ups is not None:
                    for eventuals_list in bridged_ui_look_ups.values():
                        for eventual in eventuals_list:
                            eventual._set_fault(PlumbBuddyNotConnectedError())
                bridged_uis = getattr(self, '_bridged_uis', None)
                if bridged_uis is not None:
                    for bridged_ui_tuple in bridged_uis.values():
                        bridged_ui_tuple[1]['destroyed'](None)
                self._reset_bridged_ui_cache()
                self._dispatch_is_connected_changed(False)
            elif connection_state == 2:
                self._dispatch_is_connected_changed(True)

    def _get_bridged_ui(self, unique_id: UUID):
        try:
            return self._bridged_uis[unique_id][0]
        except KeyError:
            return None

    def _process_message_from_plumbbuddy(self, message: dict):
        message_type = message['type']
        if message_type == 'bridged_ui_announcement':
            try:
                self._bridged_uis[UUID(message['unique_id'])][1]['announcement'](message['announcement'])
            except KeyError:
                pass
            return
        if message_type == 'bridged_ui_destroyed':
            try:
                self._bridged_uis.pop(UUID(message['unique_id']))[1]['destroyed'](None)
            except KeyError:
                pass
            return
        if message_type == 'bridged_ui_look_up_response':
            unique_id = UUID(message['unique_id'])
            bridged_ui = None
            is_loaded: bool = message['is_loaded']
            if is_loaded:
                bridged_ui = self._get_bridged_ui(unique_id)
                if bridged_ui is None:
                    self._dispatches = {}
                    def receive_dispatches(received_dispatches: dict):
                        self._dispatches = received_dispatches
                    bridged_ui = BridgedUi(unique_id, receive_dispatches)
                    self._bridged_uis[unique_id] = (bridged_ui, self._dispatches)
                    del self._dispatches
            eventuals_list = None
            try:
                eventuals_list = self._bridged_ui_look_ups.pop(unique_id)
            except KeyError:
                return
            if bridged_ui is not None:
                for eventual in eventuals_list:
                    eventual._set_result(bridged_ui)
                return
            fault = BridgedUiNotFoundError()
            for eventual in eventuals_list:
                eventual._set_fault(fault)
            return
        if message_type == 'bridged_ui_request_response':
            unique_id = UUID(message['unique_id'])
            bridged_ui = None
            denial_reason: int = message['denial_reason']
            if denial_reason == 0:
                bridged_ui = self._get_bridged_ui(unique_id)
                if bridged_ui is None:
                    self._dispatches = {}
                    def receive_dispatches(received_dispatches: dict):
                        self._dispatches = received_dispatches
                    bridged_ui = BridgedUi(unique_id, receive_dispatches)
                    self._bridged_uis[unique_id] = (bridged_ui, self._dispatches)
                    del self._dispatches
            eventuals_list = None
            try:
                eventuals_list = self._requested_bridged_uis.pop(unique_id)
            except KeyError:
                return
            if bridged_ui is not None:
                for eventual in eventuals_list:
                    eventual._set_result(bridged_ui)
                return
            fault = None
            if denial_reason == 1:
                fault = ScriptModNotFoundError()
            elif denial_reason == 2:
                fault = IndexNotFoundError()
            elif denial_reason == 3:
                fault = PlayerDeniedRequestError()
            elif denial_reason == 4:
                fault = InvalidHostNameError()
            else:
                fault = Exception('Unknown denial reason')
            for eventual in eventuals_list:
                eventual._set_fault(fault)
            return
        if message_type == 'focus_bridged_ui_response':
            unique_id = UUID(message['unique_id'])
            bridged_ui = self._get_bridged_ui(unique_id)
            if bridged_ui is None:
                return
            eventual_focus = bridged_ui._eventual_focus
            if eventual_focus is None:
                return
            focus_succeeded = message['success']
            eventual_focus._set_result(focus_succeeded)
            bridged_ui._eventual_focus = None
            if focus_succeeded:
                _try_to_foreground_plumbbuddy()
            return
        if message_type == 'foreground_plumbbuddy':
            _try_to_foreground_plumbbuddy()
            return
        if message_type == 'relational_data_storage_query_results':
            unique_id = UUID(message['unique_id'])
            data_stores = self._save_specific_relational_data_stores if message['is_save_specific'] else self._global_relational_data_stores
            dispatches = None
            try:
                _, dispatches = data_stores[unique_id]
            except KeyError:
                return
            dispatches['query_completed'](RelationalDataStorageQueryCompletedEventData(message))
            return
        if message_type == 'send_loaded_save_identifiers':
            ipc.send(_attach_save_characteristics({
                'type': 'send_loaded_save_identifiers_response'
            }))
            return

    def _reset_bridged_ui_cache(self):
        self._requested_bridged_uis: Dict[UUID, List[Eventual[BridgedUi]]] = {}
        self._bridged_ui_look_ups: Dict[UUID, List[Eventual[BridgedUi]]] = {}
        self._bridged_uis: Dict[UUID, Tuple[BridgedUi, dict]] = {}

    @property
    def is_connected() -> bool:
        """
        Gets whether PlumbBuddy is currently connected

        :returns: True if PlumbBuddy is connected; otherwise, False
        """

        return ipc.is_connected
    
    @property
    def is_connected_changed(self) -> Event[bool]:
        """
        Gets the event dispatched when is_connected is changed

        :returns: The Event[bool] representing the is_connected_changed event
        """

        return self._is_connected_changed

    def get_relational_data_storage(self, unique_id: UUID, is_save_specific: bool) -> RelationalDataStorage:
        """
        Gets a Relational Data Storage connection

        :param unique_id: the UUID for the Relational Data Storage instance
        :param is_save_specific: True if the Relational Data Storage instance should be tied to the currently open save game; otherwise, False
        :returns: a Relational Data Storage connection
        """

        if unique_id is None:
            raise Exception('unique_id is not optional')
        if not isinstance(unique_id, UUID):
            raise TypeError('unique_id must be UUID')
        data_stores = self._save_specific_relational_data_stores if is_save_specific else self._global_relational_data_stores
        try:
            return data_stores[unique_id][0]
        except KeyError:
            pass
        self._dispatches = {}
        def receive_dispatches(received_dispatches: dict):
            self._dispatches = received_dispatches
        relational_data_storage = RelationalDataStorage(unique_id, is_save_specific, receive_dispatches)
        data_stores[unique_id] = (relational_data_storage, self._dispatches)
        del self._dispatches
        return relational_data_storage
    
    def look_up_bridged_ui(self, unique_id: UUID) -> Eventual[BridgedUi]:
        """
        Attempts to look up a loaded bridged UI
        
        :param unique_id: the UUID for the tab of the bridged UI
        :returns: an Eventual that will resolve with the bridged UI or a fault that it is not currently loaded
        """

        if unique_id is None:
            raise Exception('unique_id is not optional')
        if not isinstance(unique_id, UUID):
            raise TypeError('unique_id must be UUID')
        eventual = Eventual[BridgedUi]()
        already_loaded = self._get_bridged_ui(unique_id)
        if already_loaded is not None:
            eventual._set_result(already_loaded)
            return eventual
        if not ipc.is_connected:
            eventual._set_fault(PlumbBuddyNotConnectedError())
            return eventual
        eventuals_list = None
        try:
            eventuals_list = self._bridged_ui_look_ups[unique_id]
        except KeyError:
            pass
        if eventuals_list is None:
            eventuals_list: List[Eventual[BridgedUi]] = []
            self._bridged_ui_look_ups[unique_id] = eventuals_list
        eventuals_list.append(eventual)
        ipc.send({
            'type': 'bridged_ui_look_up',
            'unique_id': str(unique_id)
        })
        return eventual
    
    def request_bridged_ui(self, script_mod: str, ui_root: str, unique_id: UUID, requestor_name: str, request_reason: str, tab_name: str, tab_icon_path: Optional[str] = None, host_name: Optional[str] = None) -> Eventual[BridgedUi]:
        """
        Requests a bridged UI from PlumbBuddy

        :param script_mod: either a Mods folder relative path to the `.ts4script` file containing the bridged UI's files *-or-* the hex of the SHA 256 calculated hash of the `.ts4script` file if it is manifested
        :param ui_root: the path inside the `.ts4script` file to the root of the bridged UI's files (this is where `index.html` should be located)
        :param unique_id: the UUID you are assigning to this tab to identify it to other gateway participants
        :param requestor_name: the name of party making the request, to be presented to the player
        :param request_reason: the reason the party is making the request, to be presented to the player
        :param tab_name: the name of the tab for the bridged UI in PlumbBuddy's interface if the request is approved
        :param tab_icon_path: (optional) a path to an icon to be displayed on the bridged UI's tab in PlumbBuddy's interface, inside the `.ts4script` file, relative to `ui_root`
        :param host_name: (optional) the host name for the simulated web server to use when displaying your bridged UI, which matters to common browser services like local storage and IndexedDB (this will be your UI's `unique_id` if ommitted)
        :returns: an Eventual that will resolve with the bridged UI or a fault indicating why your request was denied (e.g. `ScriptModNotFoundError`, `IndexNotFoundError`, `PlayerDeniedRequestError`, `InvalidHostNameError`, etc.)
        """

        if ui_root is None or len(ui_root) == 0:
            raise Exception('ui_root is not optional')
        if unique_id is None:
            raise Exception('unique_id is not optional')
        if not isinstance(unique_id, UUID):
            raise TypeError('unique_id must be UUID')
        if requestor_name is None or len(requestor_name) == 0:
            raise Exception('requestor_name is not optional')
        if request_reason is None or len(request_reason) == 0:
            raise Exception('request_reason is not optional')
        if tab_name is None or len(tab_name) == 0:
            raise Exception('tab_name is not optional')
        eventual = Eventual[BridgedUi]()
        already_loaded = self._get_bridged_ui(unique_id)
        if already_loaded is not None:
            eventual._set_result(already_loaded)
            _try_to_foreground_plumbbuddy()
            return eventual
        if not ipc.is_connected:
            eventual._set_fault(PlumbBuddyNotConnectedError())
            return eventual
        eventuals_list = None
        try:
            eventuals_list = self._requested_bridged_uis[unique_id]
        except KeyError:
            pass
        if eventuals_list is None:
            eventuals_list: List[Eventual[BridgedUi]] = []
            self._requested_bridged_uis[unique_id] = eventuals_list
        eventuals_list.append(eventual)
        ipc.send({
            'type': 'bridged_ui_request',
            'script_mod': script_mod,
            'ui_root': ui_root,
            'unique_id': str(unique_id),
            'requestor_name': requestor_name,
            'request_reason': request_reason,
            'tab_name': tab_name,
            'tab_icon_path': tab_icon_path,
            'host_name': host_name
        })
        _try_to_foreground_plumbbuddy()
        return eventual

gateway = Gateway()
ipc.connect()

@inject_to(ServerClock, 'tick_server_clock')
def _tick_server_clock(original, *args, **kwargs):
    original(*args, **kwargs)
    try:
        ipc.connect()
    except Exception as ex:
        logger.exception(ex)
    while not ipc._messages_from_plumbbuddy.empty():
        gateway._process_message_from_plumbbuddy(ipc._messages_from_plumbbuddy.get())
