using MathCore.HackRF.Streaming;

namespace MathCore.HackRF;

public partial class Device
{
    /// <summary>Запускает потоковую сессию приёма с очередью и обработчиком блоков</summary>
    /// <param name="Processor">Пользовательский обработчик входных блоков</param>
    /// <param name="Options">Параметры сессии</param>
    /// <returns>Объект управления сессией</returns>
    public DeviceRxSession StartRxSession(RxBlockProcessor Processor, RxSessionOptions? Options = null) =>
        new(this, Processor, Options);

    /// <summary>Запускает потоковую сессию передачи с очередью блоков</summary>
    /// <param name="Producer">Опциональный генератор данных при пустой очереди</param>
    /// <param name="Options">Параметры сессии</param>
    /// <returns>Объект управления сессией</returns>
    public DeviceTxSession StartTxSession(TxBlockProducer? Producer = null, TxSessionOptions? Options = null) =>
        new(this, Producer, Options);

    /// <summary>Запускает ретрансляцию потока между устройствами</summary>
    /// <param name="RxDevice">Устройство источника потока</param>
    /// <param name="TxDevice">Устройство передатчика</param>
    /// <param name="RxOptions">Параметры RX-сессии</param>
    /// <param name="TxOptions">Параметры TX-сессии</param>
    /// <returns>Объект управления ретрансляцией</returns>
    public static DeviceRelaySession StartRelaySession(
        Device RxDevice,
        Device TxDevice,
        RxSessionOptions? RxOptions = null,
        TxSessionOptions? TxOptions = null) =>
        new(RxDevice, TxDevice, RxOptions, TxOptions);
}
