# MathCore.HackRF

Высокоуровневая C# библиотека для управления SDR-устройством HackRF One.

## 🚀 Быстрый старт

### Установка

```bash
dotnet add package MathCore.HackRF
```

### Базовый пример

```csharp
using MathCore.HackRF;

// Инициализация
HackRFLib.Initialize();

try
{
    // Открытие устройства
    var result = HackRFLib.Open(out var device);
    if (result != HackRfError.Success)
        throw new Exception($"Ошибка: {result}");

    // Настройка
    HackRFLib.SetFreq(device, 100_000_000);      // 100 МГц
    HackRFLib.SetSampleRate(device, 10_000_000); // 10 МГц
    HackRFLib.SetLnaGain(device, 16);            // 16 дБ
    HackRFLib.SetVgaGain(device, 20);            // 20 дБ

    // Приём данных
    HackRfDelegate callback = (ref TransferInfo transfer) =>
    {
        var data = transfer.RxBytes;
        // Обработка данных
        return 0;
    };

    HackRFLib.StartRx(device, callback);
    
    // Ваша логика обработки...
    
    HackRFLib.StopRx(device);
    HackRFLib.Close(device);
}
finally
{
    HackRFLib.Exit();
}
```

## 📋 Основные возможности

### Управление устройством
- Инициализация и подключение к HackRF One
- Получение информации об устройстве 
- Поддержка нескольких устройств одновременно
- Безопасное управление ресурсами

### Настройка радиопараметров
- **Частота**: 30 МГц - 6 ГГц
- **Частота дискретизации**: 2-20 МГц
- **Полоса пропускания**: 1.75-28 МГц
- **Усиление LNA**: 0-40 дБ (шаг 8 дБ)
- **Усиление VGA**: 0-62 дБ (шаг 2 дБ)
- **Усиление TX VGA**: 0-47 дБ (шаг 1 дБ)

### Режимы работы
- **Приём (RX)**: потоковый приём с callback-обработкой
- **Передача (TX)**: потоковая передача данных
- **Развёртка**: автоматическое сканирование частот

## 🔧 API Reference

### Основные методы

```csharp
// Инициализация библиотеки
HackRfError Initialize()
void Exit()

// Управление устройствами
HackRfError Open(out IntPtr device)
HackRfError Close(IntPtr device)
DeviceListSafeHandle GetDeviceList()

// Настройка параметров
HackRfError SetFreq(IntPtr device, ulong freq_hz)
HackRfError SetSampleRate(IntPtr device, double freq_hz)
HackRfError SetLnaGain(IntPtr device, uint value)
HackRfError SetVgaGain(IntPtr device, uint value)
HackRfError SetBasebandFilterBandwidth(IntPtr device, uint bandwidth_hz)

// Приём и передача
HackRfError StartRx(IntPtr device, HackRfDelegate callback)
HackRfError StopRx(IntPtr device)
HackRfError StartTx(IntPtr device, HackRfDelegate callback)
HackRfError StopTx(IntPtr device)

// Развёртка частот
HackRfError InitSweep(IntPtr device, ushort[] frequency_list, int num_ranges, 
                      uint num_bytes, uint step_width, uint offset, int style)
```

### Структуры и перечисления

```csharp
// Информация о передаче данных
public struct TransferInfo
{
    public IntPtr Device { get; set; }
    public byte[] Buffer { get; set; }
    public int BufferLength { get; set; }
    public int ValidLength { get; set; }
    public byte[] RxBytes => Buffer[..ValidLength];
}

// Коды ошибок
public enum HackRfError
{
    Success = 0,
    InvalidParam = -2,
    NotFound = -5,
    Busy = -6,
    NoMem = -11,
    LibUsb = -1000,
    Thread = -1001,
    StreamingThreadErr = -1002,
    StreamingStopped = -1003,
    Other = -9999
}

// Типы плат
public enum BoardType : byte
{
    JawBreaker = 0,
    HackRfOne = 1,
    Rad1o = 2,
    Unknown = 0xFF
}
```

## 💻 Системные требования

- **.NET**: 8.0 или 9.0
- **ОС**: Windows x64
- **Оборудование**: HackRF One или совместимое устройство
- **Драйверы**: установленные драйверы HackRF
- **Библиотеки**: hackrf.dll, libusb-1.0.dll, pthreadVC2.dll (включены в пакет)

## 📚 Дополнительные примеры

### Получение информации об устройстве

```csharp
// Получение списка устройств
using var device_list = HackRFLib.GetDeviceList();
var serials = HackRFLib.GetSerialNumbers(device_list);

Console.WriteLine($"Найдено устройств: {serials.Length}");
foreach (var serial in serials)
    Console.WriteLine($"Серийный номер: {serial}");

// Информация о плате
if (HackRFLib.Open(out var device) == HackRfError.Success)
{
    var board_id = HackRFLib.GetBoardId(device);
    var version = HackRFLib.GetVersion(device);
    Console.WriteLine($"Плата: {board_id}, Версия: {version}");
    
    HackRFLib.Close(device);
}
```

### Развёртка частот

```csharp
// Настройка сканирования
ushort[] frequencies = [100, 200, 300, 400, 500]; // МГц
uint bytes_per_step = 8192;
uint step_width = 1000; // мкс

HackRFLib.InitSweep(device, frequencies, frequencies.Length,
                    bytes_per_step, step_width, 0, 0);

// Запуск развёртки
HackRfDelegate sweep_callback = (ref TransferInfo transfer) =>
{
    // Обработка данных развёртки
    return 0;
};

HackRFLib.StartRx(device, sweep_callback);
```

## 🔗 Полезные ссылки

- [Исходный код](https://github.com/infarh/mathcore.hackrf)
- [Документация HackRF](https://hackrf.readthedocs.io/)
- [Драйверы HackRF](https://github.com/greatscottgadgets/hackrf)

## 📝 Лицензия

MIT License - Шмачилин Павел (shmachilin@yandex.ru)

