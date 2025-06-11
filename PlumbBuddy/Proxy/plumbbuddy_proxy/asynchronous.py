from typing import Callable, Generic, List, Optional, TypeVar
from plumbbuddy_proxy import logger

T = TypeVar('T')

class Event(Generic[T]):
    def __init__(self, receive_dispatch: Callable[[Callable[[T], None]], None]):
        self._listeners: List[Callable[[T], None]] = []
        def dispatch(data: T):
            for listener in self._listeners:
                try:
                    listener(data)
                except Exception as ex:
                    logger.warning('Naughty script mod threw %s while dispatching an event; swallowing to protect game stability', ex)
        receive_dispatch(dispatch)
    
    def add_listener(self, listener: Callable[[T], None]):
        self._listeners.append(listener)
    
    def remove_listener(self, listener: Callable[[T], None]) -> bool:
        try:
            self._listeners.remove(listener)
            return True
        except ValueError:
            return False

def listen_for(event: Event[T]):
    def factory(listener: Callable[[T], None]):
        event.add_listener(listener)
        return listener
    return factory

class Eventual(Generic[T]):
    """
    Represents an operation that will complete... eventually
    """

    def __init__(self):
        self._state = 0
        self._result: Optional[T] = None
        self._fault: Optional[BaseException] = None
        self._result_callbacks: List[Callable[[Optional[T]], None]] = []
        self._fault_callbacks: List[Callable[[BaseException], None]] = []
    
    def _set_result(self, result: Optional[T]):
        if self.is_complete:
            raise Exception('Eventual has already resolved')
        self._state = 1
        self._result = result
        for callback in self._result_callbacks:
            try:
                callback(result)
            except Exception as ex:
                logger.warning('Naughty script mod gave me a result callback that threw %s; swallowing to protect game stability', ex)
    
    def _set_fault(self, fault: BaseException):
        if self.is_complete:
            raise Exception('Eventual has already resolved')
        if fault is None:
            raise Exception('fault cannot be None')
        self._state = 2
        self._fault = fault
        for callback in self._fault_callbacks:
            try:
                callback(fault)
            except Exception as ex:
                logger.warning('Naughty script mod gave me a fault callback that threw %s; swallowing to protect game stability', ex)

    @property
    def fault(self) -> Optional[BaseException]:
        """
        Gets the exception encountered when attempting to resolve the eventual result

        :returns: The exception that was encountered if one was; otherwise, None
        """
        if not self.is_faulted:
            return None
        return self._fault
    
    @property
    def has_result(self) -> bool:
        """
        Gets whether the result has been resolved
        
        :returns: True if the result has been resolved; otherwise, False
        """
        return self._state == 1
    
    @property
    def is_complete(self) -> bool:
        """
        Gets whether the operation is complete
        
        :returns: True if the operation is complete; otherwise, False
        """
        return self._state != 0
    
    @property
    def is_faulted(self) -> bool:
        """
        Gets whether the operation has faulted
        
        :returns: True if the operation has faulted; otherwise, False
        """
        return self._state == 2
    
    @property
    def result(self) -> Optional[T]:
        """
        Gets the eventual result

        :returns: The result if it has been determined at this point; otherwise, None
        """
        if not self.has_result:
            return None
        return self._result
    
    def then(self, result_callback: Callable[[Optional[T]], None], fault_callback: Callable[[BaseException], None] = None):
        """
        Specifies callbacks to be executed when the operation is eventually complete
        
        :result_callback: The callback to be invoked when the operation is complete and has a result
        :fault_callback: (Optional) The callback to be invoked when the operation has faulted
        """
        if result_callback is None:
            raise Exception("result_callback cannot be None")
        if not self.is_complete:
            self._result_callbacks.append(result_callback)
            if fault_callback is not None:
                self._fault_callbacks.append(fault_callback)
        elif self.has_result:
            result_callback(self._result)
        elif self.is_faulted and fault_callback is not None:
            fault_callback(self._fault)