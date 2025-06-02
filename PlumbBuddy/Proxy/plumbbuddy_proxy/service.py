from game_services import GameServiceManager
from plumbbuddy_proxy.utilities import inject_to
from plumbbuddy_proxy.ipc_client import ipc
from plumbbuddy_proxy import logger
from plumbbuddy_proxy.api import gateway
import services
from sims4.service_manager import Service

class PlumbBuddyProxyService(Service):
    def save(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "save"
        })

    def pre_save(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "pre_save"
        })

    def setup(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "setup"
        })

    def load(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "load"
        })

    def start(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "start"
        })

    def stop(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "stop"
        })

    def on_zone_load(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "on_zone_load"
        })

    def on_zone_unload(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "on_zone_unload"
        })

    def on_cleanup_zone_objects(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "on_cleanup_zone_objects"
        })

    def on_all_households_and_sim_infos_loaded(self, *args, **kwargs):
        ipc.send({
            "type": "game_service_message",
            "name": "on_all_households_and_sim_infos_loaded"
        })

@inject_to(GameServiceManager, "start_services")
def _start_services(original, self, *args, **kwargs):
    try:
        self.register_service(PlumbBuddyProxyService(), is_init_critical = False)
        logger.debug('[Game Service] _start_services register_service success')
    except Exception as ex:
        logger.error("[Game Service] _start_services exception: %s", ex)
    original(self, *args, **kwargs)