import json
from plumbbuddy_proxy import logger
import queue
import socket
import struct
import threading
from typing import Dict, List

PORT = 7342

class InterProcessCommunicationClient():
    """
    The PlumbBuddy IPC client
    """

    def __init__(self):
        self._connection_state = 0
        self._last_exception = None
        self._socket = None
        self._thread = None
        self._messages_from_plumbbuddy = queue.Queue()
        self._messages_to_plumbbuddy = queue.Queue()
    
    @property
    def connection_state(self) -> int:
        """
        Gets the raw connection state of the IPC client

        :returns: One of the raw connection state values

        Raw connection state values:
        * `0`: disconnected
        * `1`: connecting
        * `2`: connected
        * `3`: disconnecting
        """
        return self._connection_state
    
    @property
    def is_connected(self) -> bool:
        """
        Gets whether the IPC client is connected

        :returns: True if the IPC client is connected; otherwise, False
        """
        return self._connection_state == 2
    
    @property
    def is_disconnected(self) -> bool:
        """
        Gets whether the IPC client is disconnected

        :returns: True if the IPC client is disconnected; otherwise, False
        """
        return self._connection_state == 0
    
    @property
    def last_exception(self) -> BaseException:
        """
        Gets the last exception encountered by the IPC client

        :returns: The last exception encountered by the IPC client; otherwise, None
        """
        return self._last_exception

    def _socket_work(self):
        connect_ex_result = self._socket.connect_ex(("127.0.0.1", PORT))
        logger.debug("[IPC Client] connect_ex_result: %d", connect_ex_result)
        if connect_ex_result != 0:
            self._last_exception = OSError(connect_ex_result, "connect_ex failed")
            self._socket = None
            self._thread = None
            self._connection_state = 0
            logger.debug("[IPC Client] disconnected")
            return
        self._connection_state = 2
        logger.debug("[IPC Client] connected")
        
        try:
            while self.is_connected:
                while not self._messages_to_plumbbuddy.empty():
                    message = self._messages_to_plumbbuddy.get()
                    serialized_message = json.dumps(message, separators=(",", ":")).encode()
                    self._socket.sendall(struct.pack(">I", len(serialized_message)) + serialized_message)
                    logger.debug("[IPC Client] sent: %s", message)
                self._socket.settimeout(0.1)
                try:
                    serialized_message_size_bytes = self._socket.recv(4)
                    if not serialized_message_size_bytes:
                        raise ConnectionResetError()
                    serialized_message_size = struct.unpack(">I", serialized_message_size_bytes)[0]
                    serialized_message_chunks = []
                    while serialized_message_size:
                        serialized_message_chunk = self._socket.recv(serialized_message_size)
                        if not serialized_message_chunk:
                            raise ConnectionResetError()
                        serialized_message_chunks.append(serialized_message_chunk)
                        serialized_message_size -= len(serialized_message_chunk)
                    message = json.loads(b"".join(serialized_message_chunks))
                    self._messages_from_plumbbuddy.put(message)
                    logger.debug("[IPC Client] received: %s", message)
                except socket.timeout:
                    continue
        except Exception as ex:
            self._last_exception = ex
        finally:
            self.disconnect()

    def connect(self):
        """
        Connects to PlumbBuddy and initialize message queue processing
        """
        if not self.is_disconnected:
            return
        
        self._last_exception = None
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._thread = threading.Thread(target = self._socket_work, daemon = True, name = "PlumbBuddy IPC")
        self._connection_state = 1
        logger.debug("[IPC Client] connecting")
        self._thread.start()

    def disconnect(self):
        """
        Disconnects from PlumbBuddy and shutdown message queue processing
        """
        if not self.is_connected:
            return
        
        self._connection_state = 3
        logger.debug("[IPC Client] disconnecting")

        if self._thread and self._thread.is_alive() and not threading.current_thread() is self._thread:
            self._thread.join(timeout = 0.5)
        self._thread = None

        if self._socket:
            try:
                self._socket.shutdown(socket.SHUT_RDWR)
            except OSError:
                pass
            self._socket.close()
            self._socket = None

        self._connection_state = 0
        logger.debug("[IPC Client] disconnected")
    
    def get_pending_messages(self) -> List[Dict]:
        """
        Gets the list of pending messages from PlumbBuddy
        
        :returns: The list of messages that were pending
        """
        messages: List[Dict] = []
        while not self._messages_from_plumbbuddy.empty():
            messages.append(self._messages_from_plumbbuddy.get())
        logger.debug("[IPC Client] delivered %d received messages", len(messages))
        return messages
    
    def send(self, message: Dict):
        """
        Enqueues a message to PlumbBuddy

        :param message: The message to be sent
        """
        self._messages_to_plumbbuddy.put_nowait(message)
        logger.debug("[IPC Client] enqueued %s", message)

ipc = InterProcessCommunicationClient()