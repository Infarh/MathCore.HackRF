namespace MathCore.HackRF;

public partial class Device
{
    /// <summary>Режим работы устройства.</summary>
    private volatile TransceiverMode _Mode = TransceiverMode.OFF;

    /// <summary>Режим работы устройства.</summary>
    public TransceiverMode Mode { get => _Mode; internal set => _Mode = value; }

    /// <summary>Текущая центральная частота (Гц).</summary>
    public ulong Frequency
    {
        get { lock (_SyncRoot) return field; }
        set
        {
            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetFreq(DevicePtr, value);
                if (err != HackRfError.Success) throw new InvalidOperationException($"Ошибка установки частоты: {err}")
                    .WithData(nameof(Frequency), value)
                    .WithData(nameof(err), err);
                field = value;
            }
        }
    }

    /// <summary>
    /// Текущая полоса фильтра (Гц).<br/>
    /// Значения: 1_750_000, 2_500_000, 3_500_000, 5_000_000, 5_500_000, 6_000_000, 7_000_000,
    /// 8_000_000, 9_000_000, 10_000_000, 12_000_000, 14_000_000, 15_000_000, 20_000_000, 28_000_000 (Гц).
    /// </summary>
    public uint FilterBandwidth
    {
        get;
        set
        {
            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetBasebandFilterBandwidth(DevicePtr, value);
                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка установки полосы фильтра: {err}")
                        .WithData(nameof(FilterBandwidth), value)
                        .WithData(nameof(err), err);
                field = value;
            }
        }
    }

    /// <summary>Текущая частота дискретизации (Гц). От 2_000_000 до 20_000_000 Гц.</summary>
    public double SampleRate
    {
        get { lock (_SyncRoot) return field; }
        set
        {
            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetSampleRate(DevicePtr, value);
                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка установки частоты дискретизации: {err}")
                        .WithData(nameof(SampleRate), value)
                        .WithData(nameof(err), err);
                field = value;
            }
        }
    }

    /// <summary>Усиление LNA (дБ). Значения 0, 8, 16, 24, 32, 40 дБ. По умолчанию 16 дБ.</summary>
    public uint LnaGain
    {
        get;
        set
        {
            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetLnaGain(DevicePtr, value);
                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка установки LNA Gain: {err}")
                        .WithData(nameof(LnaGain), value)
                        .WithData(nameof(err), err);
                field = value;
            }
        }
    } = 16;

    /// <summary>Усиление VGA (дБ). Значения 0, 2, .. 62 дБ. По умолчанию 20 дБ.</summary>
    public uint VgaGain
    {
        get { lock (_SyncRoot) return field; }
        set
        {
            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetVgaGain(DevicePtr, value);
                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка установки VGA Gain: {err}")
                        .WithData(nameof(VgaGain), value)
                        .WithData(nameof(err), err);
                field = value;
            }
        }
    } = 20;

    /// <summary>Усиление TX VGA (дБ). Значения 0, 1, .. 47 дБ</summary>
    public uint TxVgaGain
    {
        get { lock (_SyncRoot) return field; }
        set
        {
            if (value is < 0 or > 47)
                throw new ArgumentOutOfRangeException(nameof(value), "TX VGA Gain должен быть в диапазоне от 0 до 47 дБ");

            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetTxVgaGain(DevicePtr, value);
                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка установки TX VGA Gain: {err}")
                        .WithData(nameof(TxVgaGain), value)
                        .WithData(nameof(err), err);
                field = value;
            }
        }
    }

    /// <summary>Включение/выключение питания антенны.</summary>
    public bool AntennaPowerSupliEnable
    {
        get { lock (_SyncRoot) return field; }
        set
        {
            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetAntennaPowerSupliEnable(DevicePtr, value);
                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка управления антенной: {err}")
                        .WithData(nameof(AntennaPowerSupliEnable), value)
                        .WithData(nameof(err), err);
                field = value;
            }
        }
    }

    /// <summary>Включение/выключение LNA усилителя.</summary>
    public bool EnableLNA
    {
        get { lock (_SyncRoot) return field; }
        set
        {
            lock (_SyncRoot)
            {
                if (Equals(field, value)) return;
                ThrowIfDisposed();
                var err = HackRFLib.SetLnaEnable(DevicePtr, value);
                if (err != HackRfError.Success)
                    throw new InvalidOperationException($"Ошибка управления LNA Amp: {err}")
                        .WithData(nameof(EnableLNA), value)
                        .WithData(nameof(err), err);
                field = value;
            }
        }
    }
}