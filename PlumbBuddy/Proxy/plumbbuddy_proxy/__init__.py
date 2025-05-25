from plumbbuddy_proxy.utilities import get_mod_root, Logger

mod_root = get_mod_root(__file__, depth = 2)
logger = Logger("PlumbBuddy_Proxy", mod_root, "PlumbBuddy_Proxy.log", version="1.5.0")