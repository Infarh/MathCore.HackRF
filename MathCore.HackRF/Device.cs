using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MathCore.HackRF;

/// <summary>Класс управления устройством HackRF</summary>
public partial class Device : IDisposable
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

    /* --------------------------------------------------------------------------------------- */

    /// <summary>Деструктор, освобождающий ресурсы устройства</summary>
    ~Device() => Dispose();

    /// <summary>Флаг, указывающий, был ли объект освобождён</summary>
    private bool _Disposed;

    /// <summary>Освобождение ресурсов устройства.</summary>
    public void Dispose()
    {
        if (_Disposed) return;

        lock (_SyncRoot)
        {
            if (_Disposed) return;

            _Disposed = true;
            GC.SuppressFinalize(this);

            var err_close = HackRFLib.Close(DevicePtr);
            if (err_close != HackRfError.Success)
                throw new InvalidOperationException($"Ошибка закрытия устройства: {err_close}")
                    .WithData(nameof(DevicePtr), DevicePtr)
                    .WithData(nameof(SerialNumber), SerialNumber ?? "")
                    .WithData(nameof(err_close), err_close);
            Trace.TraceInformation($"Закрыта плата HackRFOne sn:{SerialNumber} ptr:{DevicePtr:x}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_Disposed, this);


}
