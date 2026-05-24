namespace MathCore.HackRF.Streaming;

/// <summary>Параметры работы сессии приёма</summary>
public sealed class RxSessionOptions
{
    /// <summary>Размер очереди буферов между callback и обработчиком</summary>
    public int QueueCapacity { get; init; } = 128;

    /// <summary>Политика обработки переполнения очереди</summary>
    public RxQueueOverflowPolicy OverflowPolicy { get; init; } = RxQueueOverflowPolicy.DropNewest;

    /// <summary>Обработчик ошибок пользовательского процессора</summary>
    public Action<Exception>? OnProcessingError { get; init; }
}

/// <summary>Политика поведения очереди при переполнении</summary>
public enum RxQueueOverflowPolicy : byte
{
    /// <summary>Новые блоки отбрасываются</summary>
    DropNewest = 0,

    /// <summary>Старые блоки отбрасываются в пользу новых</summary>
    DropOldest = 1
}

/// <summary>Снимок статистики сессии приёма</summary>
/// <param name="ReceivedBlocks">Количество полученных блоков от устройства</param>
/// <param name="DroppedBlocks">Количество отброшенных блоков</param>
/// <param name="ProcessedBlocks">Количество обработанных блоков</param>
/// <param name="ReceivedBytes">Количество полученных байт</param>
/// <param name="ProcessedBytes">Количество обработанных байт</param>
/// <param name="CurrentQueueLength">Текущая длина очереди</param>
/// <param name="LastProcessingMilliseconds">Длительность обработки последнего блока в миллисекундах</param>
/// <param name="MaxProcessingMilliseconds">Максимальная длительность обработки блока в миллисекундах</param>
public readonly record struct RxSessionStatistics(
    long ReceivedBlocks,
    long DroppedBlocks,
    long ProcessedBlocks,
    long ReceivedBytes,
    long ProcessedBytes,
    int CurrentQueueLength,
    double LastProcessingMilliseconds,
    double MaxProcessingMilliseconds);
