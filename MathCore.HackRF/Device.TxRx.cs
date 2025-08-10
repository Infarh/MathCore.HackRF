using System.Diagnostics;

namespace MathCore.HackRF;

public partial class Device
{
    /// <summary>Запускает режим передачи на устройстве HackRF</summary>
    /// <param name="TxCallback">Делегат обратного вызова для передачи данных</param>
    /// <exception cref="InvalidOperationException">Выбрасывается, если устройство уже находится в режиме передачи</exception>
    /// <exception cref="InvalidOperationException">Выбрасывается, если произошла ошибка при запуске передачи</exception>
    public void StartTX(HackRfDelegate TxCallback)
    {
        lock (_SyncRoot)
        {
            ThrowIfDisposed();
            if (Mode == TransceiverMode.TX)
                throw new InvalidOperationException("Устройство уже в режиме передачи");

            Mode = TransceiverMode.TX;

            var err = HackRFLib.StartTx(DevicePtr, TxCallback);
            if (err != HackRfError.Success)
            {
                Mode = TransceiverMode.OFF;
                throw new InvalidOperationException("Ошибка запуска передачи")
                    .WithData(nameof(err), err);
            }

            Trace.TraceInformation($"HackRF sn:{SerialNumber} ptr:{DevicePtr:x} переведён в режим передачи");
        }
    }

    /// <summary>Останавливает режим передачи данных на устройстве HackRF</summary>
    /// <remarks>Если устройство не находится в режиме передачи, метод ничего не делает</remarks>
    /// <exception cref="InvalidOperationException">
    /// <para>Выбрасывается, если произошла ошибка при остановке передачи</para>
    /// </exception>
    public void StopTX()
    {
        lock (_SyncRoot)
        {
            ThrowIfDisposed();
            if (Mode != TransceiverMode.TX) return; // Если не в режиме передачи — ничего не делаем

            var err = HackRFLib.StopTx(DevicePtr);
            if (err != HackRfError.Success)
            {
                Mode = TransceiverMode.OFF;
                throw new InvalidOperationException("Ошибка остановки передачи")
                    .WithData(nameof(err), err);
            }

            Mode = TransceiverMode.OFF;
            Trace.TraceInformation($"HackRF sn:{SerialNumber} ptr:{DevicePtr:x} остановлен режим передачи");
        }
    }

    /// <summary>Запускает режим приёма на устройстве HackRF</summary>
    /// <param name="RxCallback">Делегат обратного вызова для приёма данных</param>
    /// <exception cref="InvalidOperationException">Выбрасывается, если устройство уже находится в режиме приёма</exception>
    /// <exception cref="InvalidOperationException">Выбрасывается, если произошла ошибка при запуске приёма</exception>
    public void StartRX(HackRfDelegate RxCallback)
    {
        lock (_SyncRoot)
        {
            ThrowIfDisposed();
            if (Mode == TransceiverMode.RX)
                throw new InvalidOperationException("Устройство уже в режиме приёма");

            Mode = TransceiverMode.RX;

            var err = HackRFLib.StartRx(DevicePtr, RxCallback);
            if (err != HackRfError.Success)
            {
                Mode = TransceiverMode.OFF;
                throw new InvalidOperationException("Ошибка запуска приёма")
                    .WithData(nameof(err), err);
            }

            Trace.TraceInformation($"HackRF sn:{SerialNumber} ptr:{DevicePtr:x} переведён в режим приёма");
        }
    }

    /// <summary>Останавливает режим приёма данных на устройстве HackRF</summary>
    /// <remarks>Если устройство не находится в режиме приёма, метод ничего не делает</remarks>
    /// <exception cref="InvalidOperationException">
    /// <para>Выбрасывается, если произошла ошибка при остановке приёма</para>
    /// </exception>
    public void StopRX()
    {
        lock (_SyncRoot)
        {
            ThrowIfDisposed();

            if (Mode != TransceiverMode.RX) return; // Если не в режиме приёма — ничего не делаем

            var err = HackRFLib.StopRx(DevicePtr);
            if (err != HackRfError.Success)
            {
                Mode = TransceiverMode.OFF;
                throw new InvalidOperationException("Ошибка остановки приёма")
                    .WithData(nameof(err), err);
            }

            Mode = TransceiverMode.OFF;
            Trace.TraceInformation($"HackRF sn:{SerialNumber} ptr:{DevicePtr:x} остановлен режим приёма");
        }
    }
}
