using MathCore.HackRF;

Console.WriteLine("Инициализация HackRF...");

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

    // Переменные для обработки данных
    var samples_received = 0L;
    var start_time = DateTime.Now;
    var max_amplitude = 0.0;
    var min_amplitude = 0.0;

    // Callback для обработки принятых данных
    int RXCallback(ref TransferInfo transfer)
    {
        try
        {
            var rx_data = transfer.RxBytes; // Получаем принятые данные

            if (rx_data.Length == 0) return 0;

            // Подсчитываем статистику
            samples_received += rx_data.Length / 2; // IQ-данные (2 байта на отсчёт)

            // Вычисляем амплитуду сигнала (простой алгоритм)
            for (var i = 0; i < rx_data.Length; i += 2)
            {
                if (i + 1 >= rx_data.Length) break;

                var i_sample = (sbyte)rx_data[i]; // I компонента
                var q_sample = (sbyte)rx_data[i + 1]; // Q компонента

                var amplitude = Math.Sqrt(i_sample * i_sample + q_sample * q_sample);

                if (amplitude > max_amplitude) max_amplitude = amplitude;
                if (amplitude < min_amplitude || min_amplitude == 0) min_amplitude = amplitude;
            }

            // Выводим статистику каждые 1000 блоков
            if (samples_received % (HackRFLib.SamplesPerBlock * 1000) == 0)
            {
                var elapsed = DateTime.Now - start_time;
                var rate = samples_received / elapsed.TotalSeconds;

                Console.WriteLine($"Принято: {samples_received:N0} отсчётов | " + $"Скорость: {rate / 1_000_000:N2} МГц | " + $"Амплитуда: min={min_amplitude:N1}, max={max_amplitude:N1} | " + $"Время: {elapsed.TotalSeconds:N1}с");

                // Сброс статистики амплитуды
                max_amplitude = 0;
                min_amplitude = 0;
            }

            return 0; // Возвращаем 0 для продолжения приёма
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в callback: {ex.Message}");
            return -1; // Возвращаем -1 для остановки приёма
        }
    }

    Console.WriteLine("\nЗапуск приёма данных...");
    Console.WriteLine("Нажмите любую клавишу для остановки.");

    // Запускаем приём
    device.StartRX(RXCallback);

    // Ждём нажатия клавиши
    Console.ReadKey();

    Console.WriteLine("\nОстановка приёма...");

    // Останавливаем приём
    device.StopRX();

    var total_time = DateTime.Now - start_time;
    var avg_rate = samples_received / total_time.TotalSeconds;

    Console.WriteLine($"\nСтатистика приёма:");
    Console.WriteLine($"Общее время: {total_time.TotalSeconds:F1} секунд");
    Console.WriteLine($"Принято отсчётов: {samples_received:N0}");
    Console.WriteLine($"Средняя скорость: {avg_rate / 1_000_000:F2} МГц");
    Console.WriteLine($"Объём данных: {samples_received * 2:N0} байт ({(samples_received * 2) / (1024.0 * 1024.0):F1} МБ)");
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
