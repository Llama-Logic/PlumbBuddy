from typing import Callable, Dict, List, Optional, Tuple
from plumbbuddy_proxy.asynchronous import Event, Eventual
from distributor.shared_messages import IconInfoData
from plumbbuddy_proxy.utilities import inject_to
from plumbbuddy_proxy.ipc_client import ipc
from sims4.localization import LocalizationHelperTuning
from plumbbuddy_proxy import logger
import os
from clock import ServerClock
import services
import subprocess
import sys
from ui.ui_dialog_notification import UiDialogNotification
from uuid import UUID
import webbrowser

def _try_to_foreground_plumbbuddy():
    if os.name == 'nt':
        webbrowser.open('plumbbuddy://focus')
    elif os.name == 'posix' and sys.platform == 'darwin':
        subprocess.run(['/usr/bin/osascript', '-e', 'tell application id "com.llamalogic.plumbbuddy" to activate'])

def _show_notification(text: str, title: str = None):
    client = services.client_manager().get_first_client()
    if title is None:
        title = 'PlumbBuddy Script API'
    localized_text = lambda **_: LocalizationHelperTuning.get_raw_text(text)
    localized_title = lambda **_: LocalizationHelperTuning.get_raw_text(title)
    notification = UiDialogNotification.TunableFactory().default(client.active_sim, text = localized_text, title = localized_title)
    notification.show_dialog(icon_override = IconInfoData(obj_instance = client.active_sim))

class BridgedUi:
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
        return self._annoucement
    
    @property
    def destroyed(self) -> Event[any]:
        return self._destroyed
    
    def close(self):
        ipc.send({
            'type': 'close_bridged_ui',
            'unique-id': str(self._unique_id)
        })

    def focus(self) -> Eventual[bool]:
        if self._eventual_focus is not None:
            return self._eventual_focus
        eventual = Eventual[bool]()
        ipc.send({
            'type': 'focus_bridged_ui',
            'unique_id': str(self._unique_id)
        })
        self._eventual_focus = eventual
        return eventual
    
    def send_data(self, data):
        ipc.send({
            'type': 'send_data_to_bridged_ui',
            'recipient': str(self._unique_id),
            'data': data
        })

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

class BridgedUiNotFoundError(Exception):
    def __init__(self):
        super().__init__('The referenced bridged UI is not currently loaded')

class Gateway:
    def __init__(self):
        self._reset_cache()
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
                self._reset_cache()
        ipc.connection_state_changed.add_listener(handle_ipc_connection_state_changed)

    def _get_bridged_ui(self, unique_id: UUID):
        bridged_ui = None
        try:
            bridged_ui = self._bridged_uis[unique_id][0]
        except KeyError:
            pass
        return bridged_ui

    def _process_message_from_plumbbuddy(self, message: dict):
        message_type = message['type']
        if message_type == 'bridged_ui_announcement':
            unique_id = UUID(message['unique_id'])
            bridged_ui_tuple = None
            try:
                bridged_ui_tuple = self._bridged_uis[unique_id]
            except KeyError:
                return
            bridged_ui_tuple[1]['announcement'](message['announcement'])
            return
        if message_type == 'bridged_ui_destroyed':
            unique_id = UUID(message['unique_id'])
            bridged_ui_tuple = None
            try:
                bridged_ui_tuple = self._bridged_uis.pop(unique_id)
            except KeyError:
                return
            bridged_ui_tuple[1]['destroyed'](None)
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
        if message_type == 'show_notification':
            notification_text = None
            try:
                notification_text = message['text']
            except KeyError:
                pass
            if notification_text is not None:
                notification_title = None
                try:
                    notification_title = message['notification_title']
                except KeyError:
                    pass
                _show_notification(notification_text, notification_title)
            return

    def _reset_cache(self):
        self._requested_bridged_uis: Dict[UUID, List[Eventual[BridgedUi]]] = {}
        self._bridged_ui_look_ups: Dict[UUID, List[Eventual[BridgedUi]]] = {}
        self._bridged_uis: Dict[UUID, Tuple[BridgedUi, dict]] = {}
        
    def close_bridged_ui(self, unique_id: UUID):
        """
        Attempts to close a loaded bridged UI
        
        :unique_id: the UUID for the tab of the bridged UI
        """
        if not ipc.is_connected:
            raise PlumbBuddyNotConnectedError()
        if unique_id is None:
            raise Exception('unique_id is not optional')
        ipc.send({
            'type': 'close_bridged_ui',
            'unique_id': str(unique_id)
        })
    
    def look_up_bridged_ui(self, unique_id: UUID) -> Eventual[BridgedUi]:
        """
        Attempts to look up a loaded bridged UI
        
        :unique_id: the UUID for the tab of the bridged UI
        :returns: an Eventual that will resolve with the bridged UI or a fault that it is not currently loaded
        """
        if unique_id is None:
            raise Exception('unique_id is not optional')
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
    
    def request_bridged_ui(self, script_mod: str, ui_root: str, unique_id: UUID, requestor_name: str, request_reason: str, tab_name: str, tab_icon_path: Optional[str] = None) -> Eventual[BridgedUi]:
        """
        Requests a bridged UI from PlumbBuddy

        :script_mod: either a Mods folder relative path to the `.ts4script` file containing the bridged UI's files *-or-* the hex of the SHA 256 calculated hash of the `.ts4script` file if it is manifested
        :ui_root: the path inside the `.ts4script` file to the root of the bridged UI's files (this is where `index.html` should be located)
        :unique_id: the UUID you are assigning to this tab to identify it to other gateway participants
        :requestor_name: the name of party making the request, to be presented to the player
        :request_reason: the reason the party is making the request, to be presented to the player
        :tab_name: the name of the tab for the bridged UI in PlumbBuddy's interface if the request is approved
        :tab_icon_path: (optional) a path to an icon to be displayed on the bridged UI's tab in PlumbBuddy's interface, inside the `.ts4script` file, relative to `ui_root`
        :returns: an Eventual that will resolve with the bridged UI or a fault indicating why your request was denied (e.g. `ScriptModNotFoundError`, `IndexNotFoundError`, `PlayerDeniedRequestError`, etc.)
        """
        if ui_root is None or len(ui_root) == 0:
            raise Exception('ui_root is not optional')
        if unique_id is None:
            raise Exception('unique_id is not optional')
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
            'tab_icon_path': tab_icon_path
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