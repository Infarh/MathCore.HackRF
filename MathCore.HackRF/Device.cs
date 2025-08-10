namespace MathCore.HackRF;

/// <summary>Класс управления устройством HackRF</summary>
public partial class Device
{
    /// <summary>Создаёт объект управления устройством HackRF. Открывает первое доступное устройство и получает его серийный номер.</summary>
    public Device()
    {
        var err = HackRFLib.Open(out var ptr);
        if (err != HackRfError.Success) throw new InvalidOperationException($"Ошибка открытия устройства: {err}");
        DevicePtr = ptr;

        // Получаем серийный номер устройства
        err = HackRFLib.ReadPartIdSerialNoStruct(DevicePtr, out var info);
        if (err != HackRfError.Success) throw new InvalidOperationException($"Ошибка получения серийного номера устройства: {err}");

        SerialNumber = info.SerialNumber;
    }

    /// <summary>Создаёт объект управления для уже открытого устройства HackRF.</summary>
    /// <param name="DevicePtr">Указатель на открытое устройство HackRF</param>
    /// <param name="SerialNumber">Серийный номер устройства</param>
    internal Device(nint DevicePtr, string SerialNumber) => (this.DevicePtr, this.SerialNumber) = (DevicePtr, SerialNumber);

    /* --------------------------------------------------------------------------------------- */

    /// <summary>Указатель на устройство HackRF.</summary>
    public nint DevicePtr { get; }

    /// <summary>Серийный номер устройства</summary>
    public string? SerialNumber { get; }

#if NET8_0
    private readonly object _SyncRoot = new();
#else
    private readonly Lock _SyncRoot = new();
#endif
}
