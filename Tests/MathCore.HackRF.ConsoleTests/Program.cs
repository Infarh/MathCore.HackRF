using MathCore.HackRF;
using MathCore.HackRF.Streaming;
using System.Numerics;

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
var switch_cycles = GetIntArg(cmd_args, "--cycles", 5);
var switch_rx_ms = GetIntArg(cmd_args, "--rx-ms", 400);
var switch_tx_ms = GetIntArg(cmd_args, "--tx-ms", 400);
var switch_max_capture_blocks = GetIntArg(cmd_args, "--max-capture-blocks", 256);
var scan_start_mhz = GetDoubleArg(cmd_args, "--f-start-mhz", 70);
var scan_stop_mhz = GetDoubleArg(cmd_args, "--f-stop-mhz", 110);
var scan_step_mhz = GetDoubleArg(cmd_args, "--f-step-mhz", 2);
var scan_bin_khz = GetDoubleArg(cmd_args, "--f-bin-khz", 25);
var scan_threshold_db = GetDoubleArg(cmd_args, "--f-threshold-db", 12);
var scan_blocks_per_center = GetIntArg(cmd_args, "--f-blocks", 8);
var scan_lna = (uint)Math.Max(0, GetIntArg(cmd_args, "--f-lna", 32));
var scan_vga = (uint)Math.Max(0, GetIntArg(cmd_args, "--f-vga", 16));
var scan_dc_reject_khz = GetDoubleArg(cmd_args, "--f-dc-reject-khz", 250);

Console.WriteLine($"Параметры теста: mode={mode}, seconds={run_seconds}, processing-delay-ms={processing_delay_ms}, queue={queue_capacity}, overflow={overflow_policy}, tx-use-producer={tx_use_producer}, tx-feed-delay-ms={tx_feed_delay_ms}, tx-block-bytes={tx_block_bytes}, tx-vga={tx_vga_gain}, cycles={switch_cycles}, rx-ms={switch_rx_ms}, tx-ms={switch_tx_ms}, max-capture-blocks={switch_max_capture_blocks}, f-start-mhz={scan_start_mhz}, f-stop-mhz={scan_stop_mhz}, f-step-mhz={scan_step_mhz}, f-bin-khz={scan_bin_khz}, f-threshold-db={scan_threshold_db}, f-blocks={scan_blocks_per_center}, f-lna={scan_lna}, f-vga={scan_vga}, f-dc-reject-khz={scan_dc_reject_khz}");

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
    else if (mode is "switch" or "rxtx")
    {
        await RunRxTxSwitchScenario(device, switch_cycles, switch_rx_ms, switch_tx_ms, queue_capacity, overflow_policy, switch_max_capture_blocks, tx_vga_gain);
    }
    else if (mode is "fmscan" or "fm")
    {
        await RunFmScanScenario(
            device,
            scan_start_mhz,
            scan_stop_mhz,
            scan_step_mhz,
            scan_bin_khz,
            scan_threshold_db,
            scan_blocks_per_center,
            scan_lna,
            scan_vga,
            scan_dc_reject_khz,
            queue_capacity,
            overflow_policy);
    }
    else
    {
        throw new ArgumentOutOfRangeException(nameof(mode), mode, "Поддерживаемые режимы: rx, tx, switch, fmscan");
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

static double GetDoubleArg(string[] Args, string Name, double DefaultValue)
{
    for (var i = 0; i < Args.Length - 1; i++)
        if (string.Equals(Args[i], Name, StringComparison.OrdinalIgnoreCase)
            && double.TryParse(Args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;

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

static async Task RunFmScanScenario(
    Device Device,
    double StartMHz,
    double StopMHz,
    double StepMHz,
    double BinKHz,
    double ThresholdDb,
    int BlocksPerCenter,
    uint LnaGain,
    uint VgaGain,
    double DcRejectKHz,
    int QueueCapacity,
    RxQueueOverflowPolicy OverflowPolicy)
{
    const double sample_rate_hz = 10_000_000;
    const int fft_size = 4096;

    var start_hz = StartMHz * 1_000_000;
    var stop_hz = StopMHz * 1_000_000;
    var step_hz = StepMHz * 1_000_000;
    var bin_hz = BinKHz * 1_000;
    var dc_reject_hz = DcRejectKHz * 1_000;

    if (start_hz >= stop_hz)
        throw new ArgumentOutOfRangeException(nameof(StartMHz), "Начальная частота должна быть меньше конечной");
    if (step_hz <= 0)
        throw new ArgumentOutOfRangeException(nameof(StepMHz), "Шаг сканирования должен быть больше нуля");
    if (bin_hz <= 0)
        throw new ArgumentOutOfRangeException(nameof(BinKHz), "Шаг частотной сетки должен быть больше нуля");

    Device.SampleRate = sample_rate_hz;
    Device.FilterBandwidth = 10_000_000;
    Device.LnaGain = LnaGain;
    Device.VgaGain = VgaGain;
    Device.EnableLNA = true;

    Console.WriteLine($"FM scan: {StartMHz:F1}-{StopMHz:F1} МГц, Fs=10 МГц, step={StepMHz:F2} МГц, bin={BinKHz:F1} кГц");
    Console.WriteLine($"Усиления: LNA={Device.LnaGain} дБ, VGA={Device.VgaGain} дБ");

    var bins_count = (int)Math.Floor((stop_hz - start_hz) / bin_hz) + 1;
    var power_sum = new double[bins_count];
    var power_count = new int[bins_count];
    var window = BuildHannWindow(fft_size);

    for (var center_hz = start_hz; center_hz <= stop_hz; center_hz += step_hz)
    {
        Device.Frequency = (ulong)Math.Round(center_hz);

        var processed_blocks = 0;
        using var blocks_done = new ManualResetEventSlim(false);

        using var rx_session = Device.StartRxSession((rx_block, in metadata) =>
        {
            ProcessSpectrumBlock(rx_block, fft_size, center_hz, sample_rate_hz, start_hz, stop_hz, bin_hz, dc_reject_hz, window, power_sum, power_count);

            if (Interlocked.Increment(ref processed_blocks) >= BlocksPerCenter)
                blocks_done.Set();
        }, new RxSessionOptions
        {
            QueueCapacity = Math.Max(32, QueueCapacity),
            OverflowPolicy = OverflowPolicy,
            OnProcessingError = error => Console.WriteLine($"Ошибка обработки блока спектра: {error.Message}")
        });

        _ = blocks_done.Wait(TimeSpan.FromSeconds(2));

        var stats = rx_session.GetStatistics();
        Console.WriteLine($"center={center_hz / 1_000_000:F2} МГц | blocks={stats.ProcessedBlocks:N0} drop={stats.DroppedBlocks:N0}");
    }

    var power_db = new double[bins_count];
    for (var i = 0; i < bins_count; i++)
    {
        if (power_count[i] <= 0)
        {
            power_db[i] = double.NegativeInfinity;
            continue;
        }

        var avg = power_sum[i] / power_count[i];
        power_db[i] = 10 * Math.Log10(avg + 1e-20);
    }

    var finite_values = power_db.Where(double.IsFinite).OrderBy(v => v).ToArray();
    if (finite_values.Length == 0)
    {
        Console.WriteLine("Не удалось собрать спектральные данные");
        return;
    }

    var noise_floor = finite_values[finite_values.Length / 2];
    var detected = DetectStationFrequencies(power_db, start_hz, bin_hz, noise_floor + ThresholdDb);

    Console.WriteLine();
    Console.WriteLine($"Шумовой порог (median): {noise_floor:N2} dB");
    Console.WriteLine($"Порог детекции: {noise_floor + ThresholdDb:N2} dB");

    if (detected.Count == 0)
    {
        Console.WriteLine("FM станции не обнаружены");
        return;
    }

    Console.WriteLine("Найденные FM станции:");
    foreach (var (frequency_hz, power_level_db) in detected)
        Console.WriteLine($"  {frequency_hz / 1_000_000:N3} МГц | level={power_level_db:N2} dB");
}

static double[] BuildHannWindow(int Size)
{
    var result = new double[Size];
    for (var i = 0; i < Size; i++)
        result[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (Size - 1)));
    return result;
}

static void ProcessSpectrumBlock(
    ReadOnlySpan<byte> RxBlock,
    int FftSize,
    double CenterHz,
    double SampleRateHz,
    double StartHz,
    double StopHz,
    double BinHz,
    double DcRejectHz,
    double[] Window,
    double[] PowerSum,
    int[] PowerCount)
{
    var iq_count = Math.Min(FftSize, RxBlock.Length / 2);
    if (iq_count < 256) return;

    var spectrum = new Complex[FftSize];
    for (var i = 0; i < iq_count; i++)
    {
        var i_sample = (sbyte)RxBlock[i * 2] / 128.0;
        var q_sample = (sbyte)RxBlock[i * 2 + 1] / 128.0;
        spectrum[i] = new Complex(i_sample * Window[i], q_sample * Window[i]);
    }

    for (var i = iq_count; i < FftSize; i++)
        spectrum[i] = Complex.Zero;

    FftInPlace(spectrum);

    for (var k = 0; k < FftSize; k++)
    {
        var freq_offset = (k < FftSize / 2 ? k : k - FftSize) * SampleRateHz / FftSize;
        if (Math.Abs(freq_offset) < DcRejectHz) continue; // Подавление DC/LO артефакта
        var absolute_hz = CenterHz + freq_offset;

        if (absolute_hz < StartHz || absolute_hz > StopHz) continue;

        var bin_index = (int)Math.Round((absolute_hz - StartHz) / BinHz);
        if ((uint)bin_index >= (uint)PowerSum.Length) continue;

        var mag2 = spectrum[k].Magnitude * spectrum[k].Magnitude;
        PowerSum[bin_index] += mag2;
        PowerCount[bin_index]++;
    }
}

static void FftInPlace(Complex[] Buffer)
{
    var n = Buffer.Length;
    var bits = (int)Math.Log2(n);

    for (var i = 0; i < n; i++)
    {
        var j = ReverseBits(i, bits);
        if (j <= i) continue;
        (Buffer[i], Buffer[j]) = (Buffer[j], Buffer[i]);
    }

    for (var len = 2; len <= n; len <<= 1)
    {
        var half_len = len >> 1;
        var theta = -2 * Math.PI / len;
        var w_len = new Complex(Math.Cos(theta), Math.Sin(theta));

        for (var i = 0; i < n; i += len)
        {
            var w = Complex.One;
            for (var j = 0; j < half_len; j++)
            {
                var u = Buffer[i + j];
                var v = Buffer[i + j + half_len] * w;
                Buffer[i + j] = u + v;
                Buffer[i + j + half_len] = u - v;
                w *= w_len;
            }
        }
    }
}

static int ReverseBits(int Value, int Bits)
{
    var result = 0;
    for (var i = 0; i < Bits; i++)
    {
        result = (result << 1) | (Value & 1);
        Value >>= 1;
    }

    return result;
}

static List<(double FrequencyHz, double PowerDb)> DetectStationFrequencies(double[] PowerDb, double StartHz, double BinHz, double ThresholdDb)
{
    const double merge_distance_hz = 250_000;
    var candidates = new List<(double FrequencyHz, double PowerDb)>();

    for (var i = 2; i < PowerDb.Length - 2; i++)
    {
        var p = PowerDb[i];
        if (!double.IsFinite(p)) continue;
        if (p < ThresholdDb) continue;

        if (p >= PowerDb[i - 1] && p >= PowerDb[i + 1] && p >= PowerDb[i - 2] && p >= PowerDb[i + 2])
        {
            var f = StartHz + i * BinHz;
            candidates.Add((f, p));
        }
    }

    candidates.Sort((a, b) => a.FrequencyHz.CompareTo(b.FrequencyHz));

    var merged = new List<(double FrequencyHz, double PowerDb)>();
    foreach (var candidate in candidates)
    {
        if (merged.Count == 0)
        {
            merged.Add(candidate);
            continue;
        }

        var last = merged[^1];
        if (Math.Abs(candidate.FrequencyHz - last.FrequencyHz) <= merge_distance_hz)
        {
            if (candidate.PowerDb > last.PowerDb)
                merged[^1] = candidate;
        }
        else
            merged.Add(candidate);
    }

    return merged;
}

static async Task RunRxTxSwitchScenario(
    Device Device,
    int Cycles,
    int RxMilliseconds,
    int TxMilliseconds,
    int QueueCapacity,
    RxQueueOverflowPolicy OverflowPolicy,
    int MaxCaptureBlocks,
    uint TxVgaGain)
{
    Device.Frequency = 433_000_000;
    Device.SampleRate = 10_000_000;
    Device.FilterBandwidth = 10_000_000;
    Device.LnaGain = 32;
    Device.VgaGain = 40;
    Device.EnableLNA = true;
    Device.TxVgaGain = TxVgaGain;

    Console.WriteLine($"Частота: {Device.Frequency / 1_000_000:F1} МГц");
    Console.WriteLine($"Частота дискретизации: {Device.SampleRate / 1_000_000:N1} МГц");
    Console.WriteLine($"Полоса фильтра: {Device.FilterBandwidth / 1_000_000:N1} МГц");
    Console.WriteLine($"LNA={Device.LnaGain} дБ, VGA={Device.VgaGain} дБ, TX VGA={Device.TxVgaGain} дБ");

    var total_rx_blocks = 0L;
    var total_tx_underrun = 0L;
    var total_rx_drop = 0L;
    var total_avg_power = 0d;

    Console.WriteLine("\nСтарт цикла быстрого переключения RX -> TX...");

    for (var cycle = 1; cycle <= Cycles; cycle++)
    {
        var captured_blocks = new List<byte[]>(Math.Max(16, MaxCaptureBlocks));
        var capture_lock = new object();
        var power_sum = 0d;
        var iq_count = 0L;

        RxSessionStatistics rx_stats;

        using (var rx_session = Device.StartRxSession((rx_block, in metadata) =>
        {
            var local_power = 0d;
            var local_iq_count = 0;

            for (var i = 0; i + 1 < rx_block.Length; i += 2)
            {
                var i_sample = (sbyte)rx_block[i];
                var q_sample = (sbyte)rx_block[i + 1];
                local_power += i_sample * i_sample + q_sample * q_sample; // Мгновенная мощность IQ
                local_iq_count++;
            }

            lock (capture_lock)
            {
                power_sum += local_power;
                iq_count += local_iq_count;

                if (captured_blocks.Count >= MaxCaptureBlocks) return;

                var copy = new byte[rx_block.Length];
                rx_block.CopyTo(copy);
                captured_blocks.Add(copy);
            }
        }, new RxSessionOptions
        {
            QueueCapacity = QueueCapacity,
            OverflowPolicy = OverflowPolicy,
            OnProcessingError = error => Console.WriteLine($"Ошибка обработки RX блока: {error.Message}")
        }))
        {
            await Task.Delay(Math.Max(50, RxMilliseconds));
            rx_stats = rx_session.GetStatistics();
        }

        var avg_power = iq_count > 0 ? power_sum / iq_count : 0;
        total_avg_power += avg_power;
        total_rx_blocks += rx_stats.ReceivedBlocks;
        total_rx_drop += rx_stats.DroppedBlocks;

        Console.WriteLine($"cycle={cycle} RX | recv={rx_stats.ReceivedBlocks:N0} proc={rx_stats.ProcessedBlocks:N0} drop={rx_stats.DroppedBlocks:N0} captured={captured_blocks.Count:N0} avg_power={avg_power:N2}");

        if (captured_blocks.Count == 0)
        {
            Console.WriteLine($"cycle={cycle} TX | пропуск: нет захваченных блоков");
            continue;
        }

        var replay_block_index = 0;
        var replay_offset = 0;
        TxSessionStatistics tx_stats;

        using (var tx_session = Device.StartTxSession((tx_block, in metadata) =>
        {
            var written = 0;
            while (written < tx_block.Length)
            {
                var current_block = captured_blocks[replay_block_index];
                var available = current_block.Length - replay_offset;
                var to_copy = Math.Min(available, tx_block.Length - written);

                current_block.AsSpan(replay_offset, to_copy).CopyTo(tx_block[written..]);

                written += to_copy;
                replay_offset += to_copy;

                if (replay_offset >= current_block.Length)
                {
                    replay_offset = 0;
                    replay_block_index++;
                    if (replay_block_index >= captured_blocks.Count)
                        replay_block_index = 0;
                }
            }
        }, new TxSessionOptions
        {
            QueueCapacity = QueueCapacity,
            OnProducerError = error => Console.WriteLine($"Ошибка генератора TX блока: {error.Message}")
        }))
        {
            await Task.Delay(Math.Max(50, TxMilliseconds));
            tx_stats = tx_session.GetStatistics();
        }

        total_tx_underrun += tx_stats.UnderrunBlocks;
        Console.WriteLine($"cycle={cycle} TX | dequeued={tx_stats.DequeuedBlocks:N0} underrun={tx_stats.UnderrunBlocks:N0} enqueued={tx_stats.EnqueuedBlocks:N0} drop={tx_stats.DroppedBlocks:N0}");
    }

    var power_over_cycles = Cycles > 0 ? total_avg_power / Cycles : 0;

    Console.WriteLine("\nИтог быстрого переключения:");
    Console.WriteLine($"cycles={Cycles}, total_rx_blocks={total_rx_blocks:N0}, total_rx_drop={total_rx_drop:N0}, total_tx_underrun={total_tx_underrun:N0}, avg_power={power_over_cycles:N2}");
}
