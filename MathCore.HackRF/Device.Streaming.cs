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
}
