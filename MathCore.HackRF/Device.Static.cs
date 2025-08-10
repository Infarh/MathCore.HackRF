using System.Diagnostics;

namespace MathCore.HackRF;

public partial class Device
{
    #region Инициализация библиотеки

    private class DeviceInitializer : IDisposable
    {
#if NET8_0
        private static readonly object __InitLock = new(); 
#else
        private static readonly Lock __InitLock = new();
#endif
        private static DeviceInitializer? __Instance;

        private static bool __IsDisposed;

        public static DeviceInitializer GetOrCreate()
        {
            if (__IsDisposed)
                throw new ObjectDisposedException(nameof(DeviceInitializer), "Библиотека HackRF была завершена");

            if (__Instance is not null) return __Instance;

            lock (__InitLock)
            {
                if (__Instance is not null) return __Instance;

                var timer = Stopwatch.StartNew();
                var err = HackRFLib.Initialize();
                timer.Stop();

                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка инициализации HackRF: {err}");

                Trace.TraceInformation("HackRF инициализирован успешно. {0} мс", timer.ElapsedMilliseconds);

                __Instance = new();
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                return __Instance;
            }
        }

        private DeviceInitializer() { } // Приватный конструктор для синглтона

        private static void OnProcessExit(object? sender, EventArgs e) => __Instance?.Dispose();

        public void Dispose()
        {
            if (__IsDisposed) return;

            lock (__InitLock)
            {
                if (__IsDisposed) return;

                var timer = Stopwatch.StartNew();
                var err = HackRFLib.Exit();
                timer.Stop();

                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка финализации HackRF: {err}");

                Trace.TraceInformation("HackRF завершён успешно. {0} мс", timer.ElapsedMilliseconds);

                __IsDisposed = true;
                __Instance = null;
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            }
        }
    }

    private static DeviceInitializer? __Initializer;

    /// <summary>Инициализирует библиотеку HackRF. Безопасен для многократного вызова</summary>
    public static void Initialize() => __Initializer = DeviceInitializer.GetOrCreate();

    /// <summary>Завершает работу с библиотекой HackRF</summary>
    public static void Shutdown() => __Initializer?.Dispose();

    #endregion

    /// <summary>Возвращает список всех подключённых устройств HackRF</summary>
    /// <returns>Массив информации о подключённых устройствах</returns>
    public static Info[] GetDevices()
    {
        using var list_handle = HackRFLib.GetDeviceList();
        var serials = HackRFLib.GetSerialNumbers(list_handle);
        var boards = HackRFLib.GetUsbBoardIds(list_handle);
        var count = serials.Length;

        var result = new Info[count];
        for (var i = 0; i < count; i++)
            result[i] = new() { SerialNumber = serials[i], Type = boards[i] };

        return result;
    }

    /// <summary>Возвращает список всех подключённых устройств HackRF заданного типа</summary>
    /// <param name="type">Тип платы для фильтрации устройств</param>
    /// <returns>Перечисление устройств указанного типа</returns>
    public static IEnumerable<Info> GetDevices(BoardType type) => GetDevices().Where(d => d.Type == type);
}
