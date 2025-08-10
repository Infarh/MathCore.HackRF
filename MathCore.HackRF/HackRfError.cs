namespace MathCore.HackRF;

/// <summary>Перечисление кодов ошибок HackRF</summary>
public enum HackRfError
{
    Success = 0,
    True = 1,
    InvalidParam = -2,
    NotFound = -5,
    Busy = -6,
    NoMem = -11,
    LibUsb = -1000,
    Thread = -1001,
    StreamingThreadErr = -1002,
    StreamingStopped = -1003,
    StreamingExitCalled = -1004,
    UsbApiVersion = -1005,
    NotLastDevice = -2000,
    Other = -9999
}