# All of this shamelessly stolen from Lot 51

import functools
from functools import wraps
import inspect
import logging
import os
from sims4.utils import blueprintmethod, blueprintproperty

def get_mod_root(file, depth=2):
    """
    Get the path to the directory a ts4script is located. By default, assumes the file
    is located at a depth of 2 (in the root of a package inside a compiled ts4script).

    Increase the depth if you are fetching the mod root from another level deep
    inside your mod, or running the mod decompiled.

    For example:
        - Scripts/lot51_core/__init__.py is at a depth of 2
        - Scripts/lot51_core/lib/zone.py is at a depth of 3

    Usage: get_mod_root(__file__)

    :param file: The path to the current file, usually __file__
    :param depth: The depth `file` is located relative to Mods folder.
    :return: str
    """
    root = os.path.abspath(os.path.realpath(file))
    if '.ts4script' in root.lower():
        depth += 1

    for depth in range(depth):
        root = os.path.dirname(root)

    return root

def inject_to(target_object, target_function_name, force_flex=False, force_untuned_cls=False):
    """
    Decorator to inject a function into an existing function. The original function will be provided as the first
    argument in your decorated function, with the original args/kwargs following. Depending on your goals, you should
    call the original function and pass the args/kwargs. Return the original result if necessary.

    Based on TURBODRIVER's Injector
    https://turbodriver-sims.medium.com/basic-python-injecting-into-the-sims-4-cdc85a741b10

    :param target_object: The class or instance to inject to
    :param target_function_name: The name of the function to replace on the target_object
    :param force_flex: Set to True if the target function is a flex method but does not use "cls" and "inst" as names of the first 2 arguments.
    :param force_untuned_cls: Set to True to retrieve the raw untuned class as the "cls" argument in a class method.
        As of 1.16 class methods will now provide the tuned class as "cls". This property was added to return the original default functionality.
    """

    def _wrap_target(target_function, new_function):
        @wraps(target_function)
        def _wrapped_func(*args, **kwargs):
            if type(target_function) is blueprintmethod:
                return new_function(target_function.func, *args, **kwargs)
            elif type(target_function) is blueprintproperty or type(target_function) is property:
                return new_function(target_function.fget, *args, **kwargs)
            elif force_flex or is_flexmethod(target_function):
                def new_flex_function(original, *nargs, **nkwargs):
                    cls = original.args[0]
                    inst = next(iter(narg for narg in nargs if type(narg) is cls), None)
                    if inst is not None:
                        nargs = list(nargs)
                        nargs.remove(inst)
                    return new_function(original.func, cls, inst, *nargs, **nkwargs)
                return new_flex_function(target_function, *args, **kwargs)
            return new_function(target_function, *args, **kwargs)

        if type(target_function) is blueprintmethod:
            return blueprintmethod(_wrapped_func)
        elif type(target_function) is blueprintproperty:
            return blueprintproperty(_wrapped_func)
        elif type(target_function) is staticmethod:
            return staticmethod(_wrapped_func)
        elif inspect.ismethod(target_function):
            if hasattr(target_function, '__self__') and force_untuned_cls:
                return _wrapped_func.__get__(target_function.__self__, target_function.__self__.__class__)
            return classmethod(_wrapped_func)
        elif type(target_function) is property:
            return property(_wrapped_func)
        return _wrapped_func

    def _inject(new_function):
        target_function = getattr(target_object, target_function_name)
        setattr(target_object, target_function_name, _wrap_target(target_function, new_function))
        return new_function

    return _inject

def is_flexmethod(target_function):
    """
    Tests if a function is decorated with @flexmethod by checking if it was wrapped with functools.partial,
    and inspects the name of the first 2 arguments to see if they use "cls" and "inst". This is not guaranteed, but
    is a common pattern EA uses.

    :param target_function: The function to test
    :return: bool
    """
    if type(target_function) is functools.partial:
        spec = inspect.getfullargspec(target_function.func)
        return len(spec.args) >= 2 and spec.args[0] == 'cls' and spec.args[1] == 'inst'
    return False

def Logger(name, root, filename, prefix='', version='N/A', mode='development', **kwargs):
    path = os.path.join(root, filename)
    handler = logging.FileHandler(path, mode='w')
    log_mode = logging.DEBUG if mode == 'development' else logging.INFO

    formatter = logging.Formatter('[%(levelname)s] %(message)s')

    logger = logging.getLogger(name)
    logger.setLevel(log_mode)
    logger.addHandler(handler)
    handler.setFormatter(formatter)

    logger.info(
        '{prefix}[{name}] Version: {version}; Mode: {mode}'.format(
            prefix=prefix,
            name=name,
            version=version,
            mode=mode
        )
    )
    return logger