# MathCore.HackRF

Библиотека управления SDR HackRF One для .NET

## Описание

MathCore.HackRF — это высокоуровневая C# обёртка для работы с SDR-устройством HackRF One. Библиотека предоставляет полный доступ к функциональности устройства через простой и безопасный API.

## Поддерживаемые платформы
- .NET 9
- .NET 8

## Основные возможности

### Управление устройством
- Инициализация и подключение к HackRF One
- Получение информации об устройстве (версия, серийный номер, идентификатор платы)
- Поддержка нескольких устройств одновременно
- Безопасное управление ресурсами через SafeHandle

### Настройка радиопараметров
- **Частота**: установка рабочей частоты (30 МГц - 6 ГГц)
- **Частота дискретизации**: 2-20 МГц с возможностью ручной настройки делителя
- **Полоса пропускания**: настройка базового фильтра (1.75-28 МГц)
- **Усиление**:
  - LNA (Low Noise Amplifier): 0-40 дБ с шагом 8 дБ
  - VGA (Variable Gain Amplifier): 0-62 дБ с шагом 2 дБ
  - TX VGA: 0-47 дБ с шагом 1 дБ

### Режимы работы
- **Приём (RX)**: потоковый приём данных с callback-обработкой
- **Передача (TX)**: потоковая передача данных
- **Развёртка (Sweep)**: автоматическое сканирование диапазонов частот
  - Линейная и псевдослучайная развёртка
  - Настраиваемые параметры шага и диапазонов

### Дополнительные функции
- Управление питанием антенны
- Настройка аппаратной синхронизации для нескольких устройств
- Выходной тактовый сигнал для синхронизации
- Доступ к низкоуровневым регистрам (MAX2837, SI5351C, RFFC5071)
- Работа с SPI флеш-памятью и CPLD

## Основные классы и структуры

### HackRFLib
Основной статический класс, содержащий все P/Invoke методы для работы с нативной библиотекой hackrf.dll.

### TransferInfo
Структура для передачи данных между устройством и приложением в callback-функциях.

### DeviceListSafeHandle
Безопасная обёртка для списка устройств с автоматическим освобождением ресурсов.

### Перечисления
- **HackRfError**: коды ошибок библиотеки
- **BoardType**: типы поддерживаемых плат (HackRF One, Jawbreaker, rad1o)
- **FilterType**: типы фильтров для явного задания частоты

## Пример использования

```csharp
using MathCore.HackRF;

// Инициализация библиотеки
var init_result = HackRFLib.Initialize();
if (init_result != HackRfError.Success)
    throw new Exception($"Ошибка инициализации: {init_result}");

try
{
    // Получение списка устройств
    using var device_list = HackRFLib.GetDeviceList();
    var serial_numbers = HackRFLib.GetSerialNumbers(device_list);
    
    if (serial_numbers.Length == 0)
        throw new Exception("HackRF устройства не найдены");
    
    // Открытие первого устройства
    var open_result = HackRFLib.Open(out var device);
    if (open_result != HackRfError.Success)
        throw new Exception($"Ошибка открытия устройства: {open_result}");
    
    try
    {
        // Настройка параметров
        HackRFLib.SetFreq(device, 100_000_000); // 100 МГц
        HackRFLib.SetSampleRate(device, 10_000_000); // 10 МГц
        HackRFLib.SetLnaGain(device, 16); // 16 дБ
        HackRFLib.SetVgaGain(device, 20); // 20 дБ
        
        // Запуск приёма
        HackRfDelegate callback = (ref TransferInfo transfer) =>
        {
            var data = transfer.RxBytes;
            // Обработка принятых данных
            return 0;
        };
        
        HackRFLib.StartRx(device, callback);
        
        // Работа с данными...
        
        HackRFLib.StopRx(device);
    }
    finally
    {
        HackRFLib.Close(device);
    }
}
finally
{
    HackRFLib.Exit();
}
```

## Установка

1. Скачайте и установите пакет через NuGet Package Manager
2. Убедитесь, что файл `hackrf.dll` находится в папке с исполняемым файлом или в PATH
3. Драйверы HackRF должны быть установлены в системе

## Технические требования

- HackRF One или совместимое устройство
- Windows (x64) с установленными драйверами HackRF
- Нативная библиотека hackrf.dll

## Лицензия
MIT

## Автор
Шмачилин Павел (shmachilin@yandex.ru)

## Ссылки
- [Репозиторий проекта](https://github.com/infarh/mathcore.hackrf)
- [Официальная документация HackRF](https://hackrf.readthedocs.io/)
- [Драйверы и ПО для HackRF](https://github.com/greatscottgadgets/hackrf)

