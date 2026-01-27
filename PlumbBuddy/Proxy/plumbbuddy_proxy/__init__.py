from plumbbuddy_proxy.deployment import BuildMode, SemVer
from plumbbuddy_proxy.utilities import get_mod_root, Logger
mod_root = get_mod_root(__file__, depth = 2)
plumbbuddy_version = SemVer("__VERSION__")
plumbbuddy_build_mode = BuildMode.Debug if "__BUILD_MODE__" == 'debug' else BuildMode.Release
logger = Logger("PlumbBuddy Proxy", mod_root, "PlumbBuddy_Proxy.log", version = plumbbuddy_version, build_mode = plumbbuddy_build_mode)
