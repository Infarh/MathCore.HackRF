using System.Runtime.InteropServices;

namespace MathCore.HackRF;

/// <summary>SafeHandle для списка устройств HackRF</summary>
public sealed class DeviceListSafeHandle(nint handle = default) : SafeHandle(handle, true)
{
    public override bool IsInvalid => handle == default;

    protected override bool ReleaseHandle()
    {
        HackRFLib.DeviceListFree(handle); // Освобождение списка устройств
        return true;
    }

    public static implicit operator IntPtr(DeviceListSafeHandle handle) => handle.handle;

    public static implicit operator DeviceListSafeHandle(IntPtr handle) => new(handle);
}
