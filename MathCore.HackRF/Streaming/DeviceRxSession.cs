using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;

namespace MathCore.HackRF.Streaming;

/// <summary>Сессия потокового приёма данных устройства с bounded-очередью</summary>
public sealed class DeviceRxSession : IDisposable
{
    private readonly Device _Device;
    private readonly RxBlockProcessor _Processor;
    private readonly RxSessionOptions _Options;
    private readonly Channel<QueuedBlock> _Queue;
    private readonly CancellationTokenSource _Cts = new();
    private readonly Task _ConsumerTask;

    private long _ReceivedBlocks;
    private long _DroppedBlocks;
    private long _ProcessedBlocks;
    private long _ReceivedBytes;
    private long _ProcessedBytes;
    private int _CurrentQueueLength;
    private long _LastProcessingTicks;
    private long _MaxProcessingTicks;
    private long _SequenceId;

    private bool _Disposed;

    /// <summary>Создаёт и запускает новую сессию приёма</summary>
    /// <param name="Device">Устройство источника данных</param>
    /// <param name="Processor">Пользовательский обработчик блоков</param>
    /// <param name="Options">Параметры сессии</param>
    internal DeviceRxSession(Device Device, RxBlockProcessor Processor, RxSessionOptions? Options = null)
    {
        _Device = Device ?? throw new ArgumentNullException(nameof(Device));
        _Processor = Processor ?? throw new ArgumentNullException(nameof(Processor));
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

        _ConsumerTask = Task.Run(ProcessQueueAsync);
        _Device.StartRX(OnRxCallback);
    }

    /// <summary>Возвращает снимок статистики сессии</summary>
    public RxSessionStatistics GetStatistics() => new(
        Interlocked.Read(ref _ReceivedBlocks),
        Interlocked.Read(ref _DroppedBlocks),
        Interlocked.Read(ref _ProcessedBlocks),
        Interlocked.Read(ref _ReceivedBytes),
        Interlocked.Read(ref _ProcessedBytes),
        Volatile.Read(ref _CurrentQueueLength),
        TimeSpan.FromTicks(Interlocked.Read(ref _LastProcessingTicks)).TotalMilliseconds,
        TimeSpan.FromTicks(Interlocked.Read(ref _MaxProcessingTicks)).TotalMilliseconds);

    private int OnRxCallback(ref TransferInfo Transfer)
    {
        if (_Disposed) return -1;

        var rx_bytes = Transfer.RxBytes;
        if (rx_bytes.Length == 0) return 0;

        var sequence_id = Interlocked.Increment(ref _SequenceId);
        var buffer = ArrayPool<byte>.Shared.Rent(rx_bytes.Length);
        rx_bytes.CopyTo(buffer);

        var queued_block = new QueuedBlock(
            buffer,
            rx_bytes.Length,
            new RxBlockMetadata(sequence_id, DateTime.UtcNow, rx_bytes.Length));

        Interlocked.Increment(ref _ReceivedBlocks);
        Interlocked.Add(ref _ReceivedBytes, rx_bytes.Length);

        if (_Queue.Writer.TryWrite(queued_block))
        {
            Interlocked.Increment(ref _CurrentQueueLength);
            return 0;
        }

        ArrayPool<byte>.Shared.Return(buffer);
        Interlocked.Increment(ref _DroppedBlocks);

        if (_Options.OverflowPolicy == RxQueueOverflowPolicy.DropNewest)
            return 0;

        return 0;
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var queued_block in _Queue.Reader.ReadAllAsync(_Cts.Token).ConfigureAwait(false))
            {
                Interlocked.Decrement(ref _CurrentQueueLength);

                var processing_timer = Stopwatch.GetTimestamp();
                try
                {
                    var metadata = queued_block.Metadata;
                    _Processor(queued_block.Buffer.AsSpan(0, queued_block.Length), in metadata);

                    Interlocked.Increment(ref _ProcessedBlocks);
                    Interlocked.Add(ref _ProcessedBytes, queued_block.Length);
                }
                catch (Exception error)
                {
                    _Options.OnProcessingError?.Invoke(error);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(queued_block.Buffer);
                    var elapsed_ticks = Stopwatch.GetTimestamp() - processing_timer;
                    var elapsed = elapsed_ticks * TimeSpan.TicksPerSecond / Stopwatch.Frequency;

                    Interlocked.Exchange(ref _LastProcessingTicks, elapsed);

                    long current_max;
                    do
                    {
                        current_max = Interlocked.Read(ref _MaxProcessingTicks);
                        if (elapsed <= current_max) break;
                    } while (Interlocked.CompareExchange(ref _MaxProcessingTicks, elapsed, current_max) != current_max);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // штатное завершение по отмене
        }
    }

    /// <summary>Останавливает сессию приёма и освобождает ресурсы</summary>
    public void Dispose()
    {
        if (_Disposed) return;
        _Disposed = true;

        _Device.StopRX();
        _Queue.Writer.TryComplete();
        _Cts.Cancel();

        try
        {
            _ConsumerTask.Wait();
        }
        catch (AggregateException error) when (error.InnerExceptions.All(e => e is TaskCanceledException or OperationCanceledException))
        {
            // штатное завершение worker-задачи
        }

        while (_Queue.Reader.TryRead(out var queued_block))
            ArrayPool<byte>.Shared.Return(queued_block.Buffer);

        _Cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly record struct QueuedBlock(byte[] Buffer, int Length, RxBlockMetadata Metadata);
}
