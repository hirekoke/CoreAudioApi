using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    public enum AudioClientError : uint
    {
        NotInitialized = 0x88890001,
        AlreadyInitialized = 0x88890002,
        WrongEndpointType = 0x88890003,
        DeviceInvalidated = 0x88890004,
        NotStopped = 0x88890005,
        BufferTooLarge = 0x88890006,
        OutOfOrder = 0x88890007,
        UnsupportedFormat = 0x88890008,
        InvalidSize = 0x88890009,
        DeviceInUse = 0x8889000a,
        BufferOperationPending = 0x8889000b,
        ThreadNotRegistered = 0x8889000c,
        NoSingleProcess = 0x8889000d,
        ExclusiveModeNotAllowed = 0x8889000e,
        EndpointCreateFailed = 0x8889000f,
        ServiceNotRunning = 0x88890010,
        EventHandleNotExpected = 0x88890011,
        ExclusiveModeOnly = 0x88890012,
        BufDurationPeriodNotEqual = 0x88890013,
        EventHandleNotSet = 0x88890014,
        IncorrectBufferSize = 0x88890015,
        BufferSizeError = 0x88890016,
        CpuUsageExceeded = 0x88890017,
        BufferError = 0x88890018,
        BufferSizeNotAligned = 0x88890019,
        InvalidDevicePeriod = 0x88890020,
    }
    public enum AudioClientSuccess : uint
    {
        BufferEmpty = 0x08890001,
        ThreadAlreadyRegistered = 0x08890002,
        PositionStalled = 0x08890003,
    }
}
