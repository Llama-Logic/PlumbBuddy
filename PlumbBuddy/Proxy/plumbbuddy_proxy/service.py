from game_services import GameServiceManager
from plumbbuddy_proxy.utilities import inject_to
from plumbbuddy_proxy.ipc_client import ipc
from plumbbuddy_proxy import logger
from plumbbuddy_proxy.api import gateway
from sims4.service_manager import Service

class PlumbBuddyProxyService(Service):
    def save(self, *args, **kwargs):
        pass

    def pre_save(self, *args, **kwargs):
        pass

    def setup(self, *args, **kwargs):
        pass

    def load(self, *args, **kwargs):
        pass

    def start(self, *args, **kwargs):
        gateway._is_game_service_running = True

    def stop(self, *args, **kwargs):
        gateway._is_game_service_running = False
        ipc.send({
            "t": "control_message",
            "n": "game_services_stopped"
        })

    def on_zone_load(self, *args, **kwargs):
        pass

    def on_zone_unload(self, *args, **kwargs):
        pass

    def on_cleanup_zone_objects(self, *args, **kwargs):
        pass

    def on_all_households_and_sim_infos_loaded(self, *args, **kwargs):
        pass

@inject_to(GameServiceManager, "start_services")
def _start_services(original, self, *args, **kwargs):
    logger.debug('[Game Service] enter _start_services')
    try:
        self.register_service(PlumbBuddyProxyService(), is_init_critical = False)
        logger.debug('[Game Service] _start_services register_service success')
    except Exception as ex:
        logger.error("[Game Service] _start_services exception: %s", ex)
    original(self, *args, **kwargs)