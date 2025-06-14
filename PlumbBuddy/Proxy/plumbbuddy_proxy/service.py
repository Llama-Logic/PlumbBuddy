from game_services import GameServiceManager
from plumbbuddy_proxy.utilities import inject_to
from plumbbuddy_proxy.ipc_client import ipc
from plumbbuddy_proxy import logger
from plumbbuddy_proxy.api import _attach_save_characteristics
from sims4.service_manager import Service

class PlumbBuddyProxyService(Service):
    def save(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'save'
        })

    def pre_save(self, *args, **kwargs):
        ipc.send(_attach_save_characteristics({
            'type': 'game_service_event',
            'event': 'pre_save'
        }))

    def setup(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'setup'
        })

    def load(self, *args, **kwargs):
        ipc.send(_attach_save_characteristics({
            'type': 'game_service_event',
            'event': 'load'
        }))

    def start(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'start'
        })

    def stop(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'stop'
        })

    def on_zone_load(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'on_zone_load'
        })

    def on_zone_unload(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'on_zone_unload'
        })

    def on_cleanup_zone_objects(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'on_cleanup_zone_objects'
        })

    def on_all_households_and_sim_infos_loaded(self, *args, **kwargs):
        ipc.send({
            'type': 'game_service_event',
            'event': 'on_all_households_and_sim_infos_loaded'
        })

@inject_to(GameServiceManager, 'start_services')
def _start_services(original, self, *args, **kwargs):
    try:
        self.register_service(PlumbBuddyProxyService(), is_init_critical = False)
        logger.debug('[Game Service] _start_services register_service success')
    except Exception as ex:
        logger.exception(ex)
    original(self, *args, **kwargs)