using MathCore.HackRF;
using MathCore.HackRF.Streaming;

Console.WriteLine("Инициализация HackRF...");

var cmd_args = Environment.GetCommandLineArgs();
var run_seconds = GetIntArg(cmd_args, "--seconds", 5);
var processing_delay_ms = GetIntArg(cmd_args, "--processing-delay-ms", 0);
var queue_capacity = GetIntArg(cmd_args, "--queue", 256);
var use_drop_oldest = cmd_args.Any(a => string.Equals(a, "--drop-oldest", StringComparison.OrdinalIgnoreCase));
var overflow_policy = use_drop_oldest ? RxQueueOverflowPolicy.DropOldest : RxQueueOverflowPolicy.DropNewest;

Console.WriteLine($"Параметры теста: seconds={run_seconds}, processing-delay-ms={processing_delay_ms}, queue={queue_capacity}, overflow={overflow_policy}");

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

    // Настраиваем параметры приёма
    device.Frequency = 433_000_000; // 433 МГц
    device.SampleRate = 10_000_000;  // 10 МГц
    device.FilterBandwidth = 10_000_000; // 10 МГц полоса фильтра
    device.LnaGain = 32;      // Усиление LNA 32 дБ
    device.VgaGain = 40;      // Усиление VGA 40 дБ
    device.EnableLNA = true;  // Включаем LNA

    Console.WriteLine($"Частота: {device.Frequency / 1_000_000:F1} МГц");
    Console.WriteLine($"Частота дискретизации: {device.SampleRate / 1_000_000:N1} МГц");
    Console.WriteLine($"Полоса фильтра: {device.FilterBandwidth / 1_000_000:N1} МГц");
    Console.WriteLine($"Усиление LNA: {device.LnaGain} дБ");
    Console.WriteLine($"Усиление VGA: {device.VgaGain} дБ");
    Console.WriteLine($"LNA включён: {device.EnableLNA}");

    var samples_received = 0L;
    var max_amplitude = 0.0;
    var min_amplitude = 0.0;

    using var rx_session = device.StartRxSession((rx_block, in metadata) =>
    {
        samples_received += rx_block.Length / 2;

        if (processing_delay_ms > 0)
        {
            var start_time = DateTime.UtcNow;
            while ((DateTime.UtcNow - start_time).TotalMilliseconds < processing_delay_ms)
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
        QueueCapacity = queue_capacity,
        OverflowPolicy = overflow_policy,
        OnProcessingError = error => Console.WriteLine($"Ошибка обработки RX блока: {error.Message}")
    });

    Console.WriteLine("\nЗапуск приёма данных через DeviceRxSession...");
    Console.WriteLine($"Сбор статистики {run_seconds} секунд...");

    var start_time = DateTime.UtcNow;
    for (var i = 0; i < run_seconds; i++)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        var stats = rx_session.GetStatistics();
        Console.WriteLine(
            $"t={i + 1}s | recv={stats.ReceivedBlocks:N0} | proc={stats.ProcessedBlocks:N0} | drop={stats.DroppedBlocks:N0} | q={stats.CurrentQueueLength:N0} | last={stats.LastProcessingMilliseconds:N3}ms | max={stats.MaxProcessingMilliseconds:N3}ms");
    }

    var elapsed = DateTime.UtcNow - start_time;
    var avg_rate = samples_received / Math.Max(elapsed.TotalSeconds, 0.001);
    var final_stats = rx_session.GetStatistics();

    Console.WriteLine("\nСтатистика приёма:");
    Console.WriteLine($"Общее время: {elapsed.TotalSeconds:F1} секунд");
    Console.WriteLine($"Принято отсчётов: {samples_received:N0}");
    Console.WriteLine($"Средняя скорость: {avg_rate / 1_000_000:F2} МГц");
    Console.WriteLine($"Амплитуда: min={min_amplitude:N1}, max={max_amplitude:N1}");
    Console.WriteLine($"RX blocks: recv={final_stats.ReceivedBlocks:N0}, proc={final_stats.ProcessedBlocks:N0}, drop={final_stats.DroppedBlocks:N0}");
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
