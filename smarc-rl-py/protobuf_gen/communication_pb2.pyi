from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class Observations(_message.Message):
    __slots__ = ("observations",)
    OBSERVATIONS_FIELD_NUMBER: _ClassVar[int]
    observations: _containers.RepeatedCompositeFieldContainer[Observation]
    def __init__(self, observations: _Optional[_Iterable[_Union[Observation, _Mapping]]] = ...) -> None: ...

class Reset(_message.Message):
    __slots__ = ("envsToReset", "reloadScene")
    ENVSTORESET_FIELD_NUMBER: _ClassVar[int]
    RELOADSCENE_FIELD_NUMBER: _ClassVar[int]
    envsToReset: _containers.RepeatedCompositeFieldContainer[ResetParameters]
    reloadScene: bool
    def __init__(self, envsToReset: _Optional[_Iterable[_Union[ResetParameters, _Mapping]]] = ..., reloadScene: bool = ...) -> None: ...

class Step(_message.Message):
    __slots__ = ("actions", "stepCount", "timeScale")
    ACTIONS_FIELD_NUMBER: _ClassVar[int]
    STEPCOUNT_FIELD_NUMBER: _ClassVar[int]
    TIMESCALE_FIELD_NUMBER: _ClassVar[int]
    actions: _containers.RepeatedCompositeFieldContainer[Action]
    stepCount: int
    timeScale: float
    def __init__(self, actions: _Optional[_Iterable[_Union[Action, _Mapping]]] = ..., stepCount: _Optional[int] = ..., timeScale: _Optional[float] = ...) -> None: ...

class Action(_message.Message):
    __slots__ = ("continuous", "discrete")
    CONTINUOUS_FIELD_NUMBER: _ClassVar[int]
    DISCRETE_FIELD_NUMBER: _ClassVar[int]
    continuous: _containers.RepeatedScalarFieldContainer[float]
    discrete: _containers.RepeatedScalarFieldContainer[float]
    def __init__(self, continuous: _Optional[_Iterable[float]] = ..., discrete: _Optional[_Iterable[float]] = ...) -> None: ...

class ResetParameters(_message.Message):
    __slots__ = ("index",)
    INDEX_FIELD_NUMBER: _ClassVar[int]
    index: int
    def __init__(self, index: _Optional[int] = ...) -> None: ...

class Observation(_message.Message):
    __slots__ = ("index", "transforms", "floats", "ints", "strings", "booleans")
    INDEX_FIELD_NUMBER: _ClassVar[int]
    TRANSFORMS_FIELD_NUMBER: _ClassVar[int]
    FLOATS_FIELD_NUMBER: _ClassVar[int]
    INTS_FIELD_NUMBER: _ClassVar[int]
    STRINGS_FIELD_NUMBER: _ClassVar[int]
    BOOLEANS_FIELD_NUMBER: _ClassVar[int]
    index: int
    transforms: _containers.RepeatedScalarFieldContainer[str]
    floats: _containers.RepeatedScalarFieldContainer[float]
    ints: _containers.RepeatedScalarFieldContainer[int]
    strings: _containers.RepeatedScalarFieldContainer[str]
    booleans: _containers.RepeatedScalarFieldContainer[bool]
    def __init__(self, index: _Optional[int] = ..., transforms: _Optional[_Iterable[str]] = ..., floats: _Optional[_Iterable[float]] = ..., ints: _Optional[_Iterable[int]] = ..., strings: _Optional[_Iterable[str]] = ..., booleans: _Optional[_Iterable[bool]] = ...) -> None: ...

class Transform(_message.Message):
    __slots__ = ("position", "euler", "orientation")
    POSITION_FIELD_NUMBER: _ClassVar[int]
    EULER_FIELD_NUMBER: _ClassVar[int]
    ORIENTATION_FIELD_NUMBER: _ClassVar[int]
    position: Vector3
    euler: Vector3
    orientation: Quaternion
    def __init__(self, position: _Optional[_Union[Vector3, _Mapping]] = ..., euler: _Optional[_Union[Vector3, _Mapping]] = ..., orientation: _Optional[_Union[Quaternion, _Mapping]] = ...) -> None: ...

class Vector3(_message.Message):
    __slots__ = ("x", "y", "z")
    X_FIELD_NUMBER: _ClassVar[int]
    Y_FIELD_NUMBER: _ClassVar[int]
    Z_FIELD_NUMBER: _ClassVar[int]
    x: float
    y: float
    z: float
    def __init__(self, x: _Optional[float] = ..., y: _Optional[float] = ..., z: _Optional[float] = ...) -> None: ...

class Quaternion(_message.Message):
    __slots__ = ("x", "y", "z", "w")
    X_FIELD_NUMBER: _ClassVar[int]
    Y_FIELD_NUMBER: _ClassVar[int]
    Z_FIELD_NUMBER: _ClassVar[int]
    W_FIELD_NUMBER: _ClassVar[int]
    x: float
    y: float
    z: float
    w: float
    def __init__(self, x: _Optional[float] = ..., y: _Optional[float] = ..., z: _Optional[float] = ..., w: _Optional[float] = ...) -> None: ...
