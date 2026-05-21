using System.Buffers;
using System.Threading.Channels;

namespace MathCore.HackRF.Streaming;

/// <summary>Сессия потоковой передачи данных устройства с bounded-очередью</summary>
public sealed class DeviceTxSession : IDisposable
{
    private readonly Device _Device;
    private readonly TxSessionOptions _Options;
    private readonly Channel<QueuedBlock> _Queue;
    private readonly TxBlockProducer? _Producer;

    private long _DequeuedBlocks;
    private long _UnderrunBlocks;
    private long _EnqueuedBlocks;
    private long _DroppedBlocks;
    private long _DequeuedBytes;
    private int _CurrentQueueLength;
    private long _SequenceId;

    private bool _Disposed;

    /// <summary>Создаёт и запускает новую сессию передачи</summary>
    /// <param name="Device">Устройство передачи</param>
    /// <param name="Producer">Опциональный генератор данных при пустой очереди</param>
    /// <param name="Options">Параметры сессии</param>
    internal DeviceTxSession(Device Device, TxBlockProducer? Producer = null, TxSessionOptions? Options = null)
    {
        _Device = Device ?? throw new ArgumentNullException(nameof(Device));
        _Producer = Producer;
        _Options = Options ?? new();

        if (_Options.QueueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(Options), _Options.QueueCapacity, "QueueCapacity должен быть больше нуля");

        _Queue = Channel.CreateBounded<QueuedBlock>(new BoundedChannelOptions(_Options.QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite,
            AllowSynchronousContinuations = false
        });

        _Device.StartTX(OnTxCallback);
    }

    /// <summary>Пытается добавить блок передачи в очередь</summary>
    /// <param name="TxBlock">Данные блока для передачи</param>
    /// <returns>True, если блок принят в очередь</returns>
    public bool Enqueue(ReadOnlySpan<byte> TxBlock)
    {
        if (_Disposed) return false;
        if (TxBlock.Length == 0) return true;

        var buffer = ArrayPool<byte>.Shared.Rent(TxBlock.Length);
        TxBlock.CopyTo(buffer);

        var queued_block = new QueuedBlock(buffer, TxBlock.Length);
        if (_Queue.Writer.TryWrite(queued_block))
        {
            Interlocked.Increment(ref _EnqueuedBlocks);
            Interlocked.Increment(ref _CurrentQueueLength);
            return true;
        }

        ArrayPool<byte>.Shared.Return(buffer);
        Interlocked.Increment(ref _DroppedBlocks);
        return false;
    }

    /// <summary>Возвращает снимок статистики сессии</summary>
    public TxSessionStatistics GetStatistics() => new(
        Interlocked.Read(ref _DequeuedBlocks),
        Interlocked.Read(ref _UnderrunBlocks),
        Interlocked.Read(ref _EnqueuedBlocks),
        Interlocked.Read(ref _DroppedBlocks),
        Interlocked.Read(ref _DequeuedBytes),
        Volatile.Read(ref _CurrentQueueLength));

    private int OnTxCallback(ref TransferInfo Transfer)
    {
        if (_Disposed) return -1;

        var tx_buffer = Transfer.BufferBytes;
        if (tx_buffer.Length == 0) return 0;

        var sequence_id = Interlocked.Increment(ref _SequenceId);
        if (_Queue.Reader.TryRead(out var queued_block))
        {
            Interlocked.Decrement(ref _CurrentQueueLength);

            var copy_length = Math.Min(tx_buffer.Length, queued_block.Length);
            queued_block.Buffer.AsSpan(0, copy_length).CopyTo(tx_buffer);
            if (copy_length < tx_buffer.Length)
                tx_buffer[copy_length..].Clear();

            ArrayPool<byte>.Shared.Return(queued_block.Buffer);
            Interlocked.Increment(ref _DequeuedBlocks);
            Interlocked.Add(ref _DequeuedBytes, copy_length);
            return 0;
        }

        Interlocked.Increment(ref _UnderrunBlocks);

        try
        {
            if (_Producer is not null)
            {
                var metadata = new TxBlockMetadata(sequence_id, DateTime.UtcNow, tx_buffer.Length);
                _Producer(tx_buffer, in metadata);
                return 0;
            }
        }
        catch (Exception error)
        {
            _Options.OnProducerError?.Invoke(error);
        }

        if (_Options.UnderrunPolicy == TxUnderrunPolicy.FillZeros)
            tx_buffer.Clear();

        return 0;
    }

    /// <summary>Останавливает сессию передачи и освобождает ресурсы</summary>
    public void Dispose()
    {
        if (_Disposed) return;
        _Disposed = true;

        _Device.StopTX();
        _Queue.Writer.TryComplete();

        while (_Queue.Reader.TryRead(out var queued_block))
            ArrayPool<byte>.Shared.Return(queued_block.Buffer);

        GC.SuppressFinalize(this);
    }

    private readonly record struct QueuedBlock(byte[] Buffer, int Length);
}
