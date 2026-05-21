namespace MathCore.HackRF.Streaming;

/// <summary>Параметры работы сессии передачи</summary>
public sealed class TxSessionOptions
{
    /// <summary>Размер очереди блоков передачи</summary>
    public int QueueCapacity { get; init; } = 128;

    /// <summary>Политика формирования данных при пустой очереди</summary>
    public TxUnderrunPolicy UnderrunPolicy { get; init; } = TxUnderrunPolicy.FillZeros;

    /// <summary>Обработчик ошибок пользовательского генератора</summary>
    public Action<Exception>? OnProducerError { get; init; }
}

/// <summary>Политика поведения при пустой очереди передачи</summary>
public enum TxUnderrunPolicy : byte
{
    /// <summary>Заполнять буфер нулями</summary>
    FillZeros = 0
}

/// <summary>Снимок статистики сессии передачи</summary>
/// <param name="DequeuedBlocks">Количество успешно отправленных блоков из очереди</param>
/// <param name="UnderrunBlocks">Количество случаев пустой очереди в callback передачи</param>
/// <param name="EnqueuedBlocks">Количество добавленных блоков в очередь</param>
/// <param name="DroppedBlocks">Количество блоков, не добавленных из-за переполнения очереди</param>
/// <param name="DequeuedBytes">Количество отправленных байт из очереди</param>
/// <param name="CurrentQueueLength">Текущая длина очереди передачи</param>
public readonly record struct TxSessionStatistics(
    long DequeuedBlocks,
    long UnderrunBlocks,
    long EnqueuedBlocks,
    long DroppedBlocks,
    long DequeuedBytes,
    int CurrentQueueLength);
