using System.Runtime.InteropServices;

namespace MathCore.HackRF;

/// <summary>Список устройств</summary>
/// <param name="SerialNumbersPtr">Указатель на массив указателей на строки с серийными номерами устройств</param>
/// <param name="UsbBoardIdsPtr">Указатель на массив идентификаторов плат устройств</param>
/// <param name="UsbDeviceIndexPtr">Указатель на массив индексов USB-устройств</param>
/// <param name="DeviceCount">Количество обнаруженных устройств</param>
/// <param name="UsbDevicesPtr">Указатель на массив структур USB-устройств</param>
/// <param name="UsbDeviceCount">Количество USB-устройств</param>
internal readonly record struct DeviceList(
    nint SerialNumbersPtr,
    nint UsbBoardIdsPtr,
    nint UsbDeviceIndexPtr,
    int DeviceCount,
    nint UsbDevicesPtr,
    int UsbDeviceCount)
{
    /// <summary>Возвращает массив серийных номеров обнаруженных устройств</summary>
    /// <returns>Массив строк с серийными номерами устройств</returns>
    public string[] GetSerialNumbers()
    {
        if (DeviceCount == 0)
            return [];

        var serials = new string[DeviceCount];
        for (var i = 0; i < DeviceCount; i++)
        {
#if NET7_0_OR_GREATER
            var str_ptr = Marshal.ReadIntPtr(SerialNumbersPtr + i * nint.Size);
#else
            var str_ptr = Marshal.ReadIntPtr(SerialNumbersPtr + i * IntPtr.Size);
#endif
            serials[i] = Marshal.PtrToStringAnsi(str_ptr) ?? string.Empty;
        }

        return serials;
    }

    /// <summary>Возвращает массив идентификаторов плат обнаруженных устройств</summary>
    /// <returns>Массив идентификаторов плат устройств</returns>
    public BoardType[] GetBoardIds()
    {
        if (UsbDeviceCount == 0)
            return [];

        var ids = new BoardType[DeviceCount];
        for (var i = 0; i < DeviceCount; i++)
            ids[i] = (BoardType)Marshal.ReadInt32(UsbBoardIdsPtr + i * sizeof(int));

        return ids;
    }
}