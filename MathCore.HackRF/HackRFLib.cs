using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace MathCore.HackRF;

public static class HackRFLib
{
    private const string __DllName = "hackrf.dll";

    public const int SamplesPerBlock = 8192;

    /// <summary>Инициализирует библиотеку HackRF</summary>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_init")]
    public static extern HackRfError Initialize();

    /// <summary>Завершает работу с библиотекой HackRF</summary>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_exit")]
    public static extern HackRfError Exit();

    /// <summary>Возвращает версию библиотеки HackRF</summary>
    /// <returns>Указатель на строку с версией</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_library_version")]
    public static extern nint LibraryVersion();

    /// <summary>Возвращает релиз библиотеки HackRF</summary>
    /// <returns>Указатель на строку с релизом</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_library_release")]
    public static extern nint LibraryRelease();

    /// <summary>Открывает устройство из списка по индексу</summary>
    /// <param name="list">Указатель на список устройств</param>
    /// <param name="Index">Индекс устройства в списке</param>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_device_list_open")]
    public static extern HackRfError DeviceListOpen(nint list, int Index, out nint device);

    /// <summary>Освобождает список устройств</summary>
    /// <param name="list">Указатель на список устройств</param>
    [DllImport(__DllName, EntryPoint = "hackrf_device_list_free")]
    public static extern void DeviceListFree(nint list);

    [DllImport(__DllName, EntryPoint = "hackrf_device_list")]
    private static extern nint GetDeviceListRaw(); // Внутренний P/Invoke

    /// <summary>Возвращает SafeHandle для списка устройств HackRF</summary>
    public static DeviceListSafeHandle GetDeviceList() => GetDeviceListRaw();

    /// <summary>Открывает устройство по серийному номеру</summary>
    /// <param name="DesiredSerialNumber">Желаемый серийный номер</param>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_open_by_serial")]
    public static extern HackRfError OpenBySerial(string DesiredSerialNumber, out nint device);

    /// <summary>Закрывает устройство</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_close")]
    public static extern HackRfError Close(nint device);

    /// <summary>Устанавливает полосу пропускания базового фильтра</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="BandwidthHz">Полоса пропускания в Гц в диапазоне от 1 750 000 Гц (1,75 МГц) до 28 000 000 Гц (28 МГц).</param>
    /// <remarks>
    /// •  Обычно используются значения: 1_750_000, 2_500_000, 3_500_000, 5_000_000, 5_500_000, 6_000_000, 7_000_000,
    /// 8_000_000, 9_000_000, 10_000_000, 12_000_000, 14_000_000, 15_000_000, 20_000_000, 28_000_000 (Гц).<br/>
    /// •  Если указать неподдерживаемое значение, библиотека выберет ближайшее допустимое.
    /// </remarks>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_baseband_filter_bandwidth")]
    public static extern HackRfError SetBasebandFilterBandwidth(nint device, uint BandwidthHz);

    /// <summary>Устанавливает рабочую частоту захватываемого диапазона</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="FreqHz">Центральная частота рабочего диапазона в Гц</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_freq")]
    public static extern HackRfError SetFreq(nint device, ulong FreqHz);

    /// <summary>Устанавливает частоту дискретизации</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="FreqHz">Частота в герцах в диапазоне от 2 000 000 (2 МГц) до 20 000 000 (20 МГц).</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_sample_rate")]
    public static extern HackRfError SetSampleRate(nint device, double FreqHz);

    /// <summary>Включает или выключает малошумящий усилитель LNA (Low Noise Amplifier)</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="value">Флаг включения/отключения усилителя</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_amp_enable")]
    public static extern HackRfError SetLnaEnable(nint device, [MarshalAs(UnmanagedType.U1)] bool value);

    /// <summary>Устанавливает усиление LNA (Low Noise Amplifier) - первый усилитель, усиливает слабый сигнал с минимальным шумом.</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="gain">Значение усиления в дБ (0, 8, 16, 24, 32, 40). По умолчанию 16 дБ.</param>
    /// <remarks>
    /// LNA (Low Noise Amplifier):<br/>
    /// •  Это малошумящий усилитель, который стоит на самом входе радиотракта.<br/>
    /// •  Его задача — усилить очень слабый сигнал с антенны, добавляя при этом минимальный собственный шум.<br/>
    /// •  Усиление LNA важно для повышения чувствительности приёма, особенно при слабых сигналах.<br/>
    /// Слишком большое усиление LNA может привести к перегрузке тракта и появлению искажений, а недостаточное — к потере слабых сигналов.<br/>
    /// </remarks>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_lna_gain")]
    public static extern HackRfError SetLnaGain(nint device, uint gain);

    /// <summary>Устанавливает усиление VGA (Variable Gain Amplifier) - регулируемый усилитель после LNA, позволяет точно подстроить уровень сигнала для дальнейшей обработки.</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="gain">Значение усиления дБ допустимые значения: от 0 до 62 дБ включительно. Шаг изменения: 2 дБ (0, 2, 4, 6, ..., 62). По умолчанию 20 дБ.</param>
    /// <remarks>
    /// VGA (Variable Gain Amplifier) — это усилитель с регулируемым коэффициентом усиления. В SDR-устройствах,
    /// таких, как HackRF, VGA используется для управления уровнем сигнала на входе или выходе радиотракта.
    /// Это позволяет:<br/>
    /// •  Подстраивать усиление под уровень принимаемого сигнала, чтобы избежать перегрузки или, наоборот,
    /// повысить слабый сигнал.<br/>
    /// •  Гибко управлять динамическим диапазоном устройства.<br/>
    /// •  Его задача — гибко изменять усиление в зависимости от уровня входного сигнала, чтобы не перегружать последующие каскады и АЦП.<br/>
    /// •  VGA позволяет адаптировать тракт под разные условия сигнала, расширяя динамический диапазон устройства.<br/>
    /// Усиление VGA нужно подбирать так, чтобы не перегружать АЦП, но и не терять динамический диапазон.<br/>
    /// </remarks>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_vga_gain")]
    public static extern HackRfError SetVgaGain(nint device, uint gain);

    /// <summary>Устанавливает усиление TX VGA (Variable Gain Amplifier) - усилитель с регулируемым коэффициентом усиления.</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="gain">
    /// Значение усиления в дБ. Диапазон значений от 0 до 47 дБ включительно с шагом 1 дБ.<br/>
    /// Если указать значение вне диапазона, библиотека выберет ближайшее допустимое.
    /// </param>
    /// <remarks>
    /// TX VGA — это регулируемый усилитель, расположенный на выходе передатчика.<br/>
    /// Он позволяет:<br/>
    /// •  Точно настраивать мощность выходного сигнала;<br/>
    /// •  Избегать перегрузки или, наоборот, повысить уровень слабого сигнала;<br/>
    /// •  Гибко управлять динамическим диапазоном передатчика.<br/>
    /// </remarks>
    /// <returns>Код ошибки HackRF</returns>
    // ReSharper disable once StringLiteralTypo
    [DllImport(__DllName, EntryPoint = "hackrf_set_txvga_gain")]
    public static extern HackRfError SetTxVgaGain(nint device, uint gain);

    /// <summary>Включает или выключает антенну</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="value">Флаг включения антенны</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// Включение или отключение антенны позволяет управлять подачей питания на активные антенны или согласующие цепи, 
    /// что может быть необходимо для работы с внешними антеннами, требующими питания по коаксиальному кабелю (например, активные GPS-антенны).
    /// Отключение антенны может потребоваться для предотвращения подачи питания на пассивные антенны или при необходимости снизить энергопотребление устройства.
    /// Такой функционал востребован в сценариях, когда к устройству подключаются разные типы антенн, либо требуется временно отключить антенну для тестирования или защиты оборудования.
    /// </remarks>
    [DllImport(__DllName, EntryPoint = "hackrf_set_antenna_enable")]
    public static extern HackRfError SetAntennaPowerSupliEnable(nint device, [MarshalAs(UnmanagedType.U1)] bool value);

    /// <summary>Читает идентификатор платы</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="BoardId">Идентификатор платы</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_board_id_read")]
    public static extern HackRfError ReadBoardId(nint device, out byte BoardId);

    /// <summary>Читает строку версии</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="version">Указатель на буфер для версии</param>
    /// <param name="length">Длина буфера (должна быть 32 байта)</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_version_string_read")]
    public static extern HackRfError ReadVersionString(nint device, nint version, byte length);

    /// <summary>Читает версию USB API</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="UsbApiVersion">Версия USB API</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_usb_api_version_read")]
    public static extern HackRfError ReadUsbApiVersion(nint device, out ushort UsbApiVersion);

    /// <summary>Запускает прием данных</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="callback">Callback-функция, осуществляющая обработку принятых данных</param>
    /// <param name="RXCtx">Указатель на пользовательский контекст данных, передаваемый в callback-функцию приёма</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_start_rx")]
    public static extern HackRfError StartRx(nint device, HackRfDelegate callback, nint RXCtx = 0);

    /// <summary>Останавливает прием данных</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_stop_rx")]
    public static extern HackRfError StopRx(nint device);

    /// <summary>Запускает передачу данных</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="callback">Callback-функция, осуществляющая подготовку данных для передачи</param>
    /// <param name="TXCtx">Указатель на пользовательский контекст данных, передаваемый в callback-функцию передачи</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_start_tx")]
    public static extern HackRfError StartTx(nint device, HackRfDelegate callback, nint TXCtx = 0);

    /// <summary>Останавливает передачу данных</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_stop_tx")]
    public static extern HackRfError StopTx(nint device);

    /// <summary>Проверяет, выполняется ли потоковая передача</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_is_streaming")]
    public static extern HackRfError IsStreaming(nint device);

    /// <summary>Сбрасывает устройство</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_reset")]
    public static extern HackRfError Reset(nint device);

    /// <summary>Включает или выключает выходной тактовый сигнал</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="value">Флаг включения/выключения выходного тактового сигнала</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// Выходной тактовый сигнал (clock output) — это сигнал, генерируемый устройством HackRF и выводимый на специальный пин, который может использоваться в качестве источника синхронизации для других устройств или внешних схем.<br/>
    /// Такой сигнал позволяет синхронизировать работу нескольких SDR-устройств или внешних модулей, чтобы они работали с одной и той же тактовой частотой.<br/>
    /// <br/>
    /// Сценарии использования:<br/>
    /// • Синхронизация нескольких SDR-устройств для одновременного приёма/передачи (например, в системах MIMO или фазированных антеннах).<br/>
    /// • Использование HackRF как источника стабильного тактового сигнала для внешних радиочастотных модулей или генераторов.<br/>
    /// • Проведение экспериментов, где требуется согласованная работа нескольких устройств по одной частоте.<br/>
    /// <br/>
    /// Включение этой опции может быть необходимо, если требуется обеспечить синхронную работу нескольких устройств или внешних схем, а также для тестирования и калибровки оборудования.<br/>
    /// </remarks>
    // ReSharper disable once StringLiteralTypo
    [DllImport(__DllName, EntryPoint = "hackrf_set_clkout_enable")]
    // ReSharper disable once IdentifierTypo
    public static extern HackRfError SetClkoutEnable(nint device, [MarshalAs(UnmanagedType.U1)] bool value);

    /// <summary>Читает регистр MAX2837</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="RegisterNumber">Номер регистра</param>
    /// <param name="value">Значение регистра</param>
    /// <remarks>
    /// Регистр MAX2837 относится к радиочастотному приёмнику MAX2837,
    /// который используется в HackRF для приёма и обработки радиосигналов в диапазоне 30–6000 МГц.<br/>
    /// Данный чип отвечает за преобразование радиосигнала в промежуточную частоту, усиление, фильтрацию и демодуляцию сигнала.<br/>
    /// Доступ к его регистрам позволяет настраивать параметры работы радиотракта: усиление, частотные фильтры, режимы работы и калибровку.<br/>
    /// Обычно регистры MAX2837 используются при инициализации устройства, настройке приёма, а также для низкоуровневой отладки и оптимизации радиотракта.<br/>
    /// </remarks>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_max2837_read")]
    public static extern HackRfError Max2837Read(nint device, byte RegisterNumber, out ushort value);

    /// <summary>Записывает в регистр MAX2837</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="RegisterNumber">Номер регистра</param>
    /// <param name="value">Значение для записи</param>
    /// <remarks>
    /// Регистр MAX2837 относится к радиочастотному приёмнику MAX2837,
    /// который используется в HackRF для приёма и обработки радиосигналов в диапазоне 30–6000 МГц.<br/>
    /// Данный чип отвечает за преобразование радиосигнала в промежуточную частоту, усиление, фильтрацию и демодуляцию сигнала.<br/>
    /// Доступ к его регистрам позволяет настраивать параметры работы радиотракта: усиление, частотные фильтры, режимы работы и калибровку.<br/>
    /// Обычно регистры MAX2837 используются при инициализации устройства, настройке приёма, а также для низкоуровневой отладки и оптимизации радиотракта.<br/>
    /// </remarks>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_max2837_write")]
    public static extern HackRfError Max2837Write(nint device, byte RegisterNumber, ushort value);

    /// <summary>Читает регистр SI5351C</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="RegisterNumber">Номер регистра</param>
    /// <param name="value">Значение регистра</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// Регистр SI5351C относится к программируемому генератору тактовых сигналов SI5351C, который используется в устройстве HackRF
    /// для формирования различных частотных сигналов, необходимых для работы радиотракта.<br/>
    /// SI5351C позволяет генерировать несколько независимых тактовых частот с высокой точностью и гибкой настройкой.<br/>
    /// <br/>
    /// Основные задачи регистра SI5351C:<br/>
    /// • Управление параметрами генерации тактовых сигналов (частота, фаза, делители и т.д.);<br/>
    /// • Настройка выходных каналов генератора для синхронизации различных блоков устройства;<br/>
    /// • Обеспечение согласованной работы радиочастотных и цифровых компонентов HackRF.<br/>
    /// <br/>
    /// Использование:<br/>
    /// • Регистры SI5351C читаются и настраиваются при инициализации устройства, смене частоты дискретизации,
    /// а также при необходимости синхронизации с внешними устройствами;<br/>
    /// • Доступ к этим регистрам необходим для низкоуровневой настройки и отладки работы генератора тактовых сигналов;<br/>
    /// • В некоторых сценариях (например, при работе с несколькими SDR-устройствами или при необходимости нестандартных частот)
    /// требуется ручная настройка регистров SI5351C.<br/>
    /// </remarks>
    [DllImport(__DllName, EntryPoint = "hackrf_si5351c_read")]
    public static extern HackRfError Si5351CRead(nint device, ushort RegisterNumber, out ushort value);

    /// <summary>Записывает в регистр SI5351C</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="RegisterNumber">Номер регистра</param>
    /// <param name="value">Значение для записи</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_si5351c_write")]
    public static extern HackRfError Si5351CWrite(nint device, ushort RegisterNumber, ushort value);

    /// <summary>Читает регистр RFFC5071</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="RegisterNumber">Номер регистра</param>
    /// <param name="value">Значение регистра</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// Регистр RFFC5071 относится к программируемому смесителю/синтезатору частоты RFFC5071,
    /// который используется в устройстве HackRF для преобразования частоты радиосигнала (up/down conversion)
    /// и генерации локального осциллятора (LO).<br/>
    /// <br/>
    /// Основные задачи RFFC5071:<br/>
    /// • Генерация и настройка частоты LO для смесителя;<br/>
    /// • Переключение диапазонов частот;<br/>
    /// • Управление режимами работы смесителя и PLL.<br/>
    /// <br/>
    /// Использование регистров RFFC5071 необходимо при:<br/>
    /// • Инициализации устройства HackRF;<br/>
    /// • Настройке частоты приёма/передачи;<br/>
    /// • Низкоуровневой отладке и оптимизации радиотракта;<br/>
    /// • Проведении экспериментов с нестандартными частотами или режимами работы.<br/>
    /// <br/>
    /// Обычно прямой доступ к этим регистрам требуется разработчикам ПО SDR, инженерам и исследователям
    /// для тонкой настройки работы радиотракта или при создании специализированных приложений.
    /// </remarks>
    [DllImport(__DllName, EntryPoint = "hackrf_rffc5071_read")]
    public static extern HackRfError Rffc5071Read(nint device, byte RegisterNumber, out ushort value);

    /// <summary>Записывает в регистр RFFC5071</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="RegisterNumber">Номер регистра</param>
    /// <param name="value">Значение для записи</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_rffc5071_write")]
    public static extern HackRfError Rffc5071Write(nint device, byte RegisterNumber, ushort value);

    /// <summary>Стирает SPI флеш-память</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    // ReSharper disable once StringLiteralTypo
    [DllImport(__DllName, EntryPoint = "hackrf_spiflash_erase")]
    public static extern HackRfError SpiFlashErase(nint device);

    /// <summary>Записывает данные в SPI флеш-память</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="address">Адрес для записи</param>
    /// <param name="length">Длина данных</param>
    /// <param name="data">Указатель на данные</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// SPI флеш-память — это энергонезависимая память, подключаемая к микроконтроллеру или другому устройству
    /// по последовательному интерфейсу SPI (Serial Peripheral Interface).<br/>
    /// Она используется для хранения прошивки, пользовательских настроек, калибровочных данных,
    /// таблиц, а также других данных, которые должны сохраняться при выключении питания.<br/>
    /// <br/>
    /// В SDR-устройствах, таких, как HackRF, SPI флеш-память обычно содержит:<br/>
    /// • Загрузчик (bootloader) и основную прошивку устройства;<br/>
    /// • Калибровочные коэффициенты и параметры, необходимые для корректной работы радиотракта;<br/>
    /// • Пользовательские настройки, которые должны сохраняться между перезагрузками;<br/>
    /// • В некоторых случаях — пользовательские данные, например, таблицы частот или профили работы.<br/>
    /// <br/>
    /// Использование SPI флеш-памяти позволяет обновлять прошивку устройства без замены аппаратных компонентов,
    /// а также хранить критически важные данные, необходимые для функционирования устройства.<br/>
    /// Операции записи и чтения в SPI флеш-память обычно выполняются при обновлении прошивки, изменении настроек или калибровке устройства.
    /// </remarks>
    // ReSharper disable once StringLiteralTypo
    [DllImport(__DllName, EntryPoint = "hackrf_spiflash_write")]
    public static extern HackRfError SpiFlashWrite(nint device, uint address, ushort length, nint data);



    /// <summary>Читает данные из SPI флеш-памяти</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="address">Адрес для чтения</param>
    /// <param name="length">Длина данных</param>
    /// <param name="data">Указатель на буфер для данных</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// // Открываем устройство
    /// var err = HackRFLib.Open(out var device);
    /// if (err != HackRfError.Success) throw new Exception("Ошибка открытия устройства");
    /// 
    /// // Размер флеш-памяти (например, 16 * 1024 * 1024 для 16 МБ)
    /// const int flash_size = 16 * 1024 * 1024;
    /// var buffer = new byte[flash_size];
    /// const int chunk = 4096; // Размер блока для чтения
    /// for (var addr = 0; addr < flash_size; addr += chunk)
    /// {
    ///     var len = (ushort)Math.Min(chunk, flash_size - addr);
    ///     var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
    ///     try
    ///     {
    ///         var ptr = handle.AddrOfPinnedObject() + addr;
    ///         var res = HackRFLib.SpiFlashRead(device, (uint)addr, len, ptr);
    ///         if (res != HackRfError.Success) throw new Exception($"Ошибка чтения по адресу {addr}");
    ///     }
    ///     finally { handle.Free(); }
    /// }
    /// File.WriteAllBytes("hack_rf_spi_flash.bin", buffer);
    /// HackRFLib.Close(device);
    /// ]]>
    /// </code>
    /// </example>
    // ReSharper disable once StringLiteralTypo
    [DllImport(__DllName, EntryPoint = "hackrf_spiflash_read")]
    public static extern HackRfError SpiFlashRead(nint device, uint address, ushort length, nint data);

    /// <summary>Читает статус SPI флеш-памяти</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="data">Указатель на буфер для статуса</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// Статус SPI флеш-памяти — это набор флагов, отражающих текущее состояние памяти
    /// и результаты последних операций (чтение, запись, стирание).<br/>
    /// Обычно статус содержит информацию о занятости памяти, наличии ошибок, завершении операций,
    /// состоянии защиты от записи и других внутренних событиях.<br/>
    /// <br/>
    /// Зачем нужен статус:<br/>
    /// • Позволяет определить, завершилась ли предыдущая операция записи/стирания и можно ли начинать новую.<br/>
    /// • Помогает выявить ошибки (например, попытку записи в защищённую область или аппаратные сбои).<br/>
    /// • Используется для контроля последовательности операций и предотвращения повреждения данных.<br/>
    /// <br/>
    /// Где применяется:<br/>
    /// • При работе с флеш-памятью в низкоуровневых драйверах и прошивках.<br/>
    /// • В процедурах обновления прошивки, калибровки, сохранения пользовательских настроек.<br/>
    /// • Для диагностики и восстановления после сбоев, а также при инициализации устройства.<br/>
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// // Открываем устройство
    /// var err = HackRFLib.Open(out var device);
    /// if (err != HackRfError.Success) throw new Exception("Ошибка открытия устройства");
    /// 
    /// // Буфер для статуса (обычно 1 байт)
    /// var status = new byte[1];
    /// var handle = GCHandle.Alloc(status, GCHandleType.Pinned);
    /// try
    /// {
    ///     var ptr = handle.AddrOfPinnedObject();
    ///     var res = HackRFLib.SpiFlashStatus(device, ptr);
    ///     if (res != HackRfError.Success) throw new Exception("Ошибка чтения статуса SPI флеш-памяти");
    ///     Console.WriteLine($"Статус SPI флеш-памяти: 0x{status[0]:X2}");
    /// }
    /// finally { handle.Free(); }
    /// 
    /// HackRFLib.Close(device);
    /// ]]>
    /// </code>
    /// </example>
    // ReSharper disable once StringLiteralTypo
    [DllImport(__DllName, EntryPoint = "hackrf_spiflash_status")]
    public static extern HackRfError SpiFlashStatus(nint device, nint data);

    /// <summary>Очищает статус SPI флеш-памяти</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <remarks>
    /// Очищение статуса SPI флеш-памяти необходимо для сброса флагов ошибок и завершения предыдущих операций записи или чтения.<br/>
    /// Это позволяет подготовить флеш-память к новым операциям, избежать зависания устройства из-за неочищенных ошибок
    /// и обеспечить корректную работу с памятью. Обычно используется после нештатных ситуаций или при инициализации работы с флеш-памятью.
    /// </remarks>
    // ReSharper disable once StringLiteralTypo
    [DllImport(__DllName, EntryPoint = "hackrf_spiflash_clear_status")]
    public static extern HackRfError SpiFlashClearStatus(nint device);

    /// <summary>Записывает данные в CPLD (Complex Programmable Logic Device)</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="data">Указатель на данные</param>
    /// <param name="TotalLength">Общая длина данных</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// CPLD (Complex Programmable Logic Device) — это программируемое логическое устройство,
    /// используемое для реализации специализированных цифровых схем, которые нецелесообразно или невозможно реализовать
    /// на стандартных микроконтроллерах или микропроцессорах.<br/>
    /// В устройстве HackRF CPLD применяется для управления высокоскоростными интерфейсами, коммутацией сигналов,
    /// синхронизацией и другой специфической логикой, необходимой для работы SDR.<br/>
    /// <br/>
    /// Где используется:<br/>
    /// • Для обновления логики CPLD при изменении аппаратных возможностей или исправлении ошибок;<br/>
    /// • При производстве и тестировании устройств;<br/>
    /// • В процессе обновления прошивки, если требуется изменить поведение цифровых трактов.<br/>
    /// <br/>
    /// Как используется:<br/>
    /// • В память CPLD загружается специальный бинарный файл (прошивка CPLD), который определяет его поведение;<br/>
    /// • Для записи данных в CPLD используется данный метод, передавая указатель на буфер с бинарными данными и их размер;<br/>
    /// • После успешной записи CPLD начинает работать по новой логике без необходимости замены аппаратных компонентов.<br/>
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// // Пример записи прошивки CPLD в HackRF
    /// var err = HackRFLib.Open(out var device);
    /// if (err != HackRfError.Success) throw new Exception("Ошибка открытия устройства");
    /// var cpld_data = File.ReadAllBytes("hackrf_cpld.bin");
    /// var handle = GCHandle.Alloc(cpld_data, GCHandleType.Pinned);
    /// try
    /// {
    ///     var ptr = handle.AddrOfPinnedObject();
    ///     var res = HackRFLib.CpldWrite(device, ptr, (uint)cpld_data.Length);
    ///     if (res != HackRfError.Success) throw new Exception("Ошибка записи CPLD");
    /// }
    /// finally { handle.Free(); }
    /// HackRFLib.Close(device);
    /// ]]>
    /// </code>
    /// </example>
    [DllImport(__DllName, EntryPoint = "hackrf_cpld_write")]
    public static extern HackRfError CpldWrite(nint device, nint data, uint TotalLength);

    /// <summary>Инициализирует развертку частотного диапазона (sweep)</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="FrequencyList">Указатель на список частот (Гц), массив пар [начало, конец] для каждого диапазона</param>
    /// <param name="NumRanges">Количество диапазонов (шт.), диапазон: 1–32</param>
    /// <param name="NumBytes">Количество байт данных для одного шага (байт), диапазон: 1–262144</param>
    /// <param name="StepWidth">Ширина шага (Гц), диапазон: 1–20_000_000, шаг: 1 Гц</param>
    /// <param name="offset">Смещение (Гц) — дополнительное смещение частоты относительно начала диапазона, диапазон: 0–(StepWidth-1), шаг: 1 Гц</param>
    /// <param name="style">Стиль развертки — определяет способ обхода диапазонов: 0 — линейная (Linear), 1 — псевдослучайная (Random)</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// Развёртка (sweep) — это автоматический последовательный обход заданного диапазона частот с заданным шагом
    /// и сбором данных на каждой частоте.<br/>
    /// Применяется для спектрального анализа, поиска сигналов, мониторинга радиочастотной обстановки,
    /// измерения мощности и других задач, где требуется быстро просканировать широкий диапазон частот.<br/>
    /// Развёртка позволяет получать спектр сигнала или находить активные частоты
    /// без необходимости вручную переключать частоту приёма.<br/>
    /// Используется в анализаторах спектра, системах радиомониторинга,
    /// поиске источников помех, тестировании антенн и радиоустройств.<br/>
    /// </remarks>
    [DllImport(__DllName, EntryPoint = "hackrf_init_sweep")]
    public static extern HackRfError InitSweep(
        nint device,
        nint FrequencyList,
        uint NumRanges,
        uint NumBytes,
        uint StepWidth,
        uint offset,
        uint style);

    /// <summary>Запускает развертку приема</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="callback">Callback-функция</param>
    /// <param name="RXCtx">Контекст приема</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_start_rx_sweep")]
    public static extern HackRfError StartRxSweep(nint device, HackRfDelegate callback, nint RXCtx);

    // Вспомогательные методы

    /// <summary>Получает версию библиотеки HackRF как строку</summary>
    /// <returns>Версия библиотеки</returns>
    public static string GetLibraryVersion()
    {
        var version_ptr = LibraryVersion();
        return Marshal.PtrToStringAnsi(version_ptr) ?? string.Empty;
    }

    /// <summary>Получает релиз библиотеки HackRF как строку</summary>
    /// <returns>Релиз библиотеки</returns>
    public static string GetLibraryRelease()
    {
        var release_ptr = LibraryRelease();
        return Marshal.PtrToStringAnsi(release_ptr) ?? string.Empty;
    }

    /// <summary>Получает массив серийных номеров из списка устройств</summary>
    /// <param name="ListHandle">SafeHandle для списка устройств</param>
    /// <returns>Массив серийных номеров</returns>
    public static string[] GetSerialNumbers(DeviceListSafeHandle ListHandle) =>
        Marshal.PtrToStructure<DeviceList>(ListHandle) is { DeviceCount: > 0 } list
            ? list.GetSerialNumbers()
            : [];

    /// <summary>Получает массив идентификаторов USB-плат из списка устройств</summary>
    /// <param name="ListHandle">SafeHandle для списка устройств</param>
    /// <returns>Массив идентификаторов USB-плат</returns>
    public static BoardType[] GetUsbBoardIds(DeviceListSafeHandle ListHandle) =>
        Marshal.PtrToStructure<DeviceList>(ListHandle) is { DeviceCount: > 0 } list
            ? list.GetBoardIds()
            : [];

    // ---------------------------------------------------------------

    /// <summary>Открывает первое доступное устройство HackRF</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_open")]
    public static extern HackRfError Open(out nint device);

    /// <summary>Устанавливает частоту с явным указанием IF, LO и типа фильтра</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="IfFreqHz">
    /// Промежуточная частота (IF - Intermediate Frequency) в Гц<br/>
    /// Это частота, на которую преобразуется принимаемый или передаваемый сигнал
    /// перед дальнейшей обработкой.<br/>
    /// Используется для упрощения фильтрации и усиления сигнала.
    /// </param>
    /// <param name="LoFreqHz">
    /// Частота локального генератора (LO - Local Oscillator) в Гц<br/>
    /// Это частота, генерируемая устройством для преобразования входного сигнала в IF
    /// с помощью смесителя.<br/>
    /// LO определяет, на какую частоту будет "сдвинут" сигнал.
    /// </param>
    /// <param name="path">
    /// Тип фильтра<br/>
    /// Это параметр, определяющий, какой фильтр будет использоваться в тракте сигнала:<br/>
    /// •  Bypass — фильтр не используется, сигнал проходит напрямую.<br/>
    /// •  LowPass — используется низкочастотный фильтр, пропускает только частоты
    /// ниже определённого порога.<br/>
    /// •  HighPass — используется высокочастотный фильтр, пропускает только частоты
    /// выше определённого порога.
    /// </param>
    /// <remarks>
    /// В SDR (программно-определяемом радио) важно точно управлять преобразованием частот и фильтрацией,
    /// чтобы корректно принимать или передавать нужные сигналы и минимизировать помехи.
    /// Этот метод позволяет явно указать параметры преобразования и фильтрации, что даёт гибкость
    /// для сложных радиоприложений.<br/>
    /// <br/>
    /// •  Неправильная настройка IF и LO может привести к приёму не того диапазона частот или к появлению зеркальных каналов.
    /// •  Неправильный выбор фильтра может ухудшить качество сигнала или привести к пропуску нужных частот.
    /// </remarks>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_freq_explicit")]
    public static extern HackRfError SetFreqExplicit(nint device, ulong IfFreqHz, ulong LoFreqHz, FilterType path);

    /// <summary>Устанавливает частоту дискретизации вручную</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="FreqHz">Частота в Гц. Допустимые значения: от 2_000_000 до 20_000_000 Гц включительно.</param>
    /// <param name="FreqDivider">Делитель частоты. Допустимые значения: от 1 до 32 включительно.</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_set_sample_rate_manual")]
    public static extern HackRfError SetSampleRateManual(nint device, uint FreqHz, uint FreqDivider);

    /// <summary>Читает part ID и серийный номер платы</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="info">Структура с part ID и серийным номером</param>
    /// <returns>Код ошибки HackRF</returns>
    [DllImport(__DllName, EntryPoint = "hackrf_board_partid_serialno_read")]
    public static extern HackRfError ReadPartIdSerialNoStruct(nint device, out ReadPartIdSerialNo info);

    /// <summary>Устанавливает режим аппаратной синхронизации</summary>
    /// <param name="device">Указатель на устройство</param>
    /// <param name="value">Флаг включения/отключения синхронизации для устройства</param>
    /// <returns>Код ошибки HackRF</returns>
    /// <remarks>
    /// Аппаратная синхронизация необходима для согласованной работы нескольких SDR-устройств HackRF,
    /// когда требуется обеспечить точное совпадение времени и частоты между ними.<br/>
    /// Это важно в следующих случаях:<br/>
    /// • Проведение экспериментов с фазированными антенными решётками (MIMO), где требуется одновременный приём или передача;<br/>
    /// • Синхронизация приёмников для одновременного анализа широкого спектра частот;<br/>
    /// • Проведение радиолокационных и навигационных экспериментов, где критична временная точность;<br/>
    /// • Любые задачи, где требуется минимизировать рассогласование по фазе и времени между несколькими устройствами.<br/>
    /// Включение аппаратной синхронизации позволяет использовать внешний тактовый сигнал
    /// или специальные сигналы синхронизации для обеспечения одновременного старта, передачи или приёма данных.
    /// </remarks>
    [DllImport(__DllName, EntryPoint = "hackrf_set_hw_sync_mode")]
    public static extern HackRfError SetHwSyncMode(nint device, [MarshalAs(UnmanagedType.U1)] bool value);

    /// <summary>Тип фильтра для явного задания частоты</summary>
    public enum FilterType { Bypass = 0, LowPass = 1, HighPass = 2 }

    /// <summary>Структура part ID и серийного номера платы</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ReadPartIdSerialNo
    {
        /// <summary>Массив идентификаторов части платы (2 элемента)</summary>
        [InlineArray(2), SuppressMessage("ReSharper", "InconsistentNaming")]
        public struct PartIdArray { public uint _element0; }
        public PartIdArray PartId;

        /// <summary>Массив серийного номера платы (4 элемента)</summary>
        [InlineArray(4), SuppressMessage("ReSharper", "InconsistentNaming")]
        public struct SerialNoArray { public uint _element0; }
        public SerialNoArray SerialNo;

        public string SerialNumber
        {
            get
            {
                var serial_arr = SerialNo;
                var serial_number = string.Concat(
                    serial_arr[0].ToString("x8"),
                    serial_arr[1].ToString("x8"),
                    serial_arr[2].ToString("x8"),
                    serial_arr[3].ToString("x8"));

                return serial_number;
            }
        }
    }
}