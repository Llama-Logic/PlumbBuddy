from enum import IntEnum
from functools import total_ordering
import re

_VERSION_RE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")

@total_ordering
class SemVer:
    """
    Represents an equatable, comparable semantic version number
    """

    __slots__ = ("_major", "_minor", "_patch")

    def __init__(self, value):
        m = _VERSION_RE.match(value)
        if not m:
            raise ValueError("Invalid version: %r" % value)

        self._major = int(m.group(1))
        self._minor = int(m.group(2))
        self._patch = int(m.group(3))

    @property
    def major(self):
        """
        Gets the major version number
        """

        return self._major

    @property
    def minor(self):
        """
        Gets the minor version number
        """

        return self._minor

    @property
    def patch(self):
        """
        Gets the patch version number
        """
        
        return self._patch

    def _key(self):
        return (self._major, self._minor, self._patch)

    def __eq__(self, other):
        if not isinstance(other, SemVer):
            return NotImplemented
        return self._key() == other._key()

    def __lt__(self, other):
        if not isinstance(other, SemVer):
            return NotImplemented
        return self._key() < other._key()

    def __repr__(self):
        return "SemVer(%d.%d.%d)" % self._key()
    
    def __str__(self):
        return "%d.%d.%d" % (self._major, self._minor, self._patch)

class BuildMode(IntEnum):
    Unknown = 0
    Debug = 1
    Release = 2