from plumbbuddy_proxy.utilities import inject_to
from plumbbuddy_proxy.ipc_client import ipc
from plumbbuddy_proxy import logger
from clock import ServerClock

class Gateway:
    def __init__(self):
        self._is_game_service_running = False

gateway = Gateway()

@inject_to(ServerClock, "tick_server_clock")
def _tick_server_clock(original, *args, **kwargs):
    original(*args, **kwargs)
    if gateway._is_game_service_running:
        try:
            ipc.connect()
        except Exception as ex:
            logger.error("[Gateway] _tick_server_clock ipc.connect() exception: %s", ex)