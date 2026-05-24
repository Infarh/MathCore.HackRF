namespace MathCore.HackRF.Streaming;

/// <summary>Сессия ретрансляции потока между устройствами</summary>
public sealed class DeviceRelaySession : IDisposable
{
    private readonly DeviceRxSession _RxSession;
    private readonly DeviceTxSession _TxSession;

    private bool _Disposed;

    /// <summary>Создаёт и запускает ретрансляцию из RX устройства в TX устройство</summary>
    /// <param name="RxDevice">Устройство источника</param>
    /// <param name="TxDevice">Устройство приёмника данных для передачи</param>
    /// <param name="RxOptions">Опции RX-сессии</param>
    /// <param name="TxOptions">Опции TX-сессии</param>
    public DeviceRelaySession(Device RxDevice, Device TxDevice, RxSessionOptions? RxOptions = null, TxSessionOptions? TxOptions = null)
    {
        ArgumentNullException.ThrowIfNull(RxDevice);
        ArgumentNullException.ThrowIfNull(TxDevice);

        _TxSession = TxDevice.StartTxSession(Options: TxOptions);
        _RxSession = RxDevice.StartRxSession((rx_block, in metadata) => _ = _TxSession.Enqueue(rx_block), RxOptions);
    }

    /// <summary>Возвращает статистику текущего состояния ретрансляции</summary>
    public (RxSessionStatistics Rx, TxSessionStatistics Tx) GetStatistics() => (_RxSession.GetStatistics(), _TxSession.GetStatistics());

    /// <summary>Останавливает ретрансляцию и освобождает ресурсы</summary>
    public void Dispose()
    {
        if (_Disposed) return;
        _Disposed = true;

        _RxSession.Dispose();
        _TxSession.Dispose();
        GC.SuppressFinalize(this);
    }
}
