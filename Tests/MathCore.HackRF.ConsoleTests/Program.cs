using MathCore.HackRF;
using MathCore.HackRF.Streaming;

Console.WriteLine("Инициализация HackRF...");

var cmd_args = Environment.GetCommandLineArgs();
var mode = GetStringArg(cmd_args, "--mode", "rx").Trim().ToLowerInvariant();
var run_seconds = GetIntArg(cmd_args, "--seconds", 5);
var processing_delay_ms = GetIntArg(cmd_args, "--processing-delay-ms", 0);
var queue_capacity = GetIntArg(cmd_args, "--queue", 256);
var use_drop_oldest = cmd_args.Any(a => string.Equals(a, "--drop-oldest", StringComparison.OrdinalIgnoreCase));
var overflow_policy = use_drop_oldest ? RxQueueOverflowPolicy.DropOldest : RxQueueOverflowPolicy.DropNewest;
var tx_use_producer = cmd_args.Any(a => string.Equals(a, "--tx-use-producer", StringComparison.OrdinalIgnoreCase));
var tx_feed_delay_ms = GetIntArg(cmd_args, "--tx-feed-delay-ms", 0);
var tx_block_bytes = GetIntArg(cmd_args, "--tx-block-bytes", HackRFLib.SamplesPerBlock);
var tx_vga_gain = (uint)Math.Max(0, GetIntArg(cmd_args, "--tx-vga", 0));

Console.WriteLine($"Параметры теста: mode={mode}, seconds={run_seconds}, processing-delay-ms={processing_delay_ms}, queue={queue_capacity}, overflow={overflow_policy}, tx-use-producer={tx_use_producer}, tx-feed-delay-ms={tx_feed_delay_ms}, tx-block-bytes={tx_block_bytes}, tx-vga={tx_vga_gain}");

// Инициализируем библиотеку
Device.Initialize();

try
{
    if (Device.GetDevices(BoardType.HackRfOne).FirstOrDefault() is not { Exists: true } device_info)
    {
        Console.WriteLine("Устройство HackRF не найдено.");
        return;
    }

    // Открываем устройство
    using var device = device_info.Open();

    Console.WriteLine($"Устройство открыто. Серийный номер: {device.SerialNumber}");

    if (mode == "rx")
    {
        await RunRxScenario(device, run_seconds, processing_delay_ms, queue_capacity, overflow_policy);
    }
    else if (mode == "tx")
    {
        await RunTxScenario(device, run_seconds, queue_capacity, tx_use_producer, tx_feed_delay_ms, tx_block_bytes, tx_vga_gain);
    }
    else
    {
        throw new ArgumentOutOfRangeException(nameof(mode), mode, "Поддерживаемые режимы: rx, tx");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
finally
{
    // Завершаем работу с библиотекой
    Device.Shutdown();
    Console.WriteLine("Библиотека HackRF завершена.");
}

Console.WriteLine("Конец программы.");

static int GetIntArg(string[] Args, string Name, int DefaultValue)
{
    for (var i = 0; i < Args.Length - 1; i++)
        if (string.Equals(Args[i], Name, StringComparison.OrdinalIgnoreCase) && int.TryParse(Args[i + 1], out var value))
            return value;

    return DefaultValue;
}

static string GetStringArg(string[] Args, string Name, string DefaultValue)
{
    for (var i = 0; i < Args.Length - 1; i++)
        if (string.Equals(Args[i], Name, StringComparison.OrdinalIgnoreCase))
            return Args[i + 1];

    return DefaultValue;
}

static async Task RunRxScenario(Device Device, int RunSeconds, int ProcessingDelayMs, int QueueCapacity, RxQueueOverflowPolicy OverflowPolicy)
{
    Device.Frequency = 433_000_000;
    Device.SampleRate = 10_000_000;
    Device.FilterBandwidth = 10_000_000;
    Device.LnaGain = 32;
    Device.VgaGain = 40;
    Device.EnableLNA = true;

    Console.WriteLine($"Частота: {Device.Frequency / 1_000_000:F1} МГц");
    Console.WriteLine($"Частота дискретизации: {Device.SampleRate / 1_000_000:N1} МГц");
    Console.WriteLine($"Полоса фильтра: {Device.FilterBandwidth / 1_000_000:N1} МГц");
    Console.WriteLine($"Усиление LNA: {Device.LnaGain} дБ");
    Console.WriteLine($"Усиление VGA: {Device.VgaGain} дБ");
    Console.WriteLine($"LNA включён: {Device.EnableLNA}");

    var samples_received = 0L;
    var max_amplitude = 0.0;
    var min_amplitude = 0.0;

    using var rx_session = Device.StartRxSession((rx_block, in metadata) =>
    {
        samples_received += rx_block.Length / 2;

        if (ProcessingDelayMs > 0)
        {
            var start_time = DateTime.UtcNow;
            while ((DateTime.UtcNow - start_time).TotalMilliseconds < ProcessingDelayMs)
                Thread.SpinWait(256); // Эмуляция тяжёлой DSP обработки
        }

        for (var i = 0; i + 1 < rx_block.Length; i += 2)
        {
            var i_sample = (sbyte)rx_block[i];
            var q_sample = (sbyte)rx_block[i + 1];
            var amplitude = Math.Sqrt(i_sample * i_sample + q_sample * q_sample);

            if (amplitude > max_amplitude) max_amplitude = amplitude;
            if (amplitude < min_amplitude || min_amplitude == 0) min_amplitude = amplitude;
        }
    }, new RxSessionOptions
    {
        QueueCapacity = QueueCapacity,
        OverflowPolicy = OverflowPolicy,
        OnProcessingError = error => Console.WriteLine($"Ошибка обработки RX блока: {error.Message}")
    });

    Console.WriteLine("\nЗапуск приёма данных через DeviceRxSession...");
    Console.WriteLine($"Сбор статистики {RunSeconds} секунд...");

    var start_time_rx = DateTime.UtcNow;
    for (var i = 0; i < RunSeconds; i++)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        var stats = rx_session.GetStatistics();
        Console.WriteLine(
            $"t={i + 1}s | recv={stats.ReceivedBlocks:N0} | proc={stats.ProcessedBlocks:N0} | drop={stats.DroppedBlocks:N0} | q={stats.CurrentQueueLength:N0} | last={stats.LastProcessingMilliseconds:N3}ms | max={stats.MaxProcessingMilliseconds:N3}ms");
    }

    var elapsed = DateTime.UtcNow - start_time_rx;
    var avg_rate = samples_received / Math.Max(elapsed.TotalSeconds, 0.001);
    var final_stats = rx_session.GetStatistics();

    Console.WriteLine("\nСтатистика приёма:");
    Console.WriteLine($"Общее время: {elapsed.TotalSeconds:F1} секунд");
    Console.WriteLine($"Принято отсчётов: {samples_received:N0}");
    Console.WriteLine($"Средняя скорость: {avg_rate / 1_000_000:F2} МГц");
    Console.WriteLine($"Амплитуда: min={min_amplitude:N1}, max={max_amplitude:N1}");
    Console.WriteLine($"RX blocks: recv={final_stats.ReceivedBlocks:N0}, proc={final_stats.ProcessedBlocks:N0}, drop={final_stats.DroppedBlocks:N0}");
}

static async Task RunTxScenario(Device Device, int RunSeconds, int QueueCapacity, bool UseProducer, int FeedDelayMs, int BlockBytes, uint TxVgaGain)
{
    Device.Frequency = 433_000_000;
    Device.SampleRate = 10_000_000;
    Device.FilterBandwidth = 10_000_000;
    Device.TxVgaGain = TxVgaGain;

    Console.WriteLine($"Частота: {Device.Frequency / 1_000_000:F1} МГц");
    Console.WriteLine($"Частота дискретизации: {Device.SampleRate / 1_000_000:N1} МГц");
    Console.WriteLine($"Полоса фильтра: {Device.FilterBandwidth / 1_000_000:N1} МГц");
    Console.WriteLine($"Усиление TX VGA: {Device.TxVgaGain} дБ");

    var phase = 0d;

    using var tx_session = UseProducer
        ? Device.StartTxSession((tx_block, in metadata) => FillTone(tx_block, ref phase), new TxSessionOptions
        {
            QueueCapacity = QueueCapacity,
            OnProducerError = error => Console.WriteLine($"Ошибка генератора TX блока: {error.Message}")
        })
        : Device.StartTxSession(Options: new TxSessionOptions { QueueCapacity = QueueCapacity });

    using var cts = new CancellationTokenSource();
    var feed_enqueued = 0L;
    var feed_dropped = 0L;

    var feeder_task = Task.CompletedTask;
    if (!UseProducer)
    {
        feeder_task = Task.Run(async () =>
        {
            var tx_block = new byte[Math.Max(256, BlockBytes)];

            while (!cts.IsCancellationRequested)
            {
                FillTone(tx_block, ref phase);
                if (tx_session.Enqueue(tx_block))
                    Interlocked.Increment(ref feed_enqueued);
                else
                    Interlocked.Increment(ref feed_dropped);

                if (FeedDelayMs > 0)
                    await Task.Delay(FeedDelayMs, cts.Token);
            }
        }, cts.Token);
    }

    Console.WriteLine("\nЗапуск передачи через DeviceTxSession...");
    Console.WriteLine($"Сбор статистики {RunSeconds} секунд...");

    for (var i = 0; i < RunSeconds; i++)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        var stats = tx_session.GetStatistics();
        Console.WriteLine(
            $"t={i + 1}s | dq={stats.DequeuedBlocks:N0} | underrun={stats.UnderrunBlocks:N0} | enq={stats.EnqueuedBlocks:N0} | drop={stats.DroppedBlocks:N0} | q={stats.CurrentQueueLength:N0}");
    }

    cts.Cancel();
    try
    {
        await feeder_task;
    }
    catch (OperationCanceledException)
    {
        // штатная остановка feeder-задачи
    }

    var final_stats = tx_session.GetStatistics();

    Console.WriteLine("\nСтатистика передачи:");
    Console.WriteLine($"TX blocks: dequeued={final_stats.DequeuedBlocks:N0}, underrun={final_stats.UnderrunBlocks:N0}, enqueued={final_stats.EnqueuedBlocks:N0}, dropped={final_stats.DroppedBlocks:N0}");
    Console.WriteLine($"Feeder: enqueued={feed_enqueued:N0}, dropped={feed_dropped:N0}");
}

static void FillTone(Span<byte> Buffer, ref double Phase)
{
    const double phase_step = 2 * Math.PI * 1000 / 10_000_000; // Тон 1 кГц при Fs=10 МГц

    for (var i = 0; i + 1 < Buffer.Length; i += 2)
    {
        var i_sample = (sbyte)(Math.Sin(Phase) * 100);
        var q_sample = (sbyte)(Math.Cos(Phase) * 100);

        Buffer[i] = unchecked((byte)i_sample);
        Buffer[i + 1] = unchecked((byte)q_sample);

        Phase += phase_step;
        if (Phase > 2 * Math.PI)
            Phase -= 2 * Math.PI;
    }
}
