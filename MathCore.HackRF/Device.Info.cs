using System.Diagnostics;

namespace MathCore.HackRF;

public partial class Device
{
    /// <summary>Информация об устройстве HackRF</summary>
    /// <param name="SerialNumber">Серийный номер устройства</param>
    /// <param name="Type">Идентификатор платы</param>
    public readonly record struct Info(string SerialNumber, BoardType Type)
    {
        /// <summary>Указывает, что устройство существует и имеет валидный серийный номер</summary>
        public bool Exists => SerialNumber?.Length > 0;

        /// <summary>Открывает устройство по серийному номеру и возвращает объект управления</summary>
        public Device Open()
        {
            var err = HackRFLib.OpenBySerial(SerialNumber, out var device_ptr);
            if (err != HackRfError.Success) throw new InvalidOperationException($"Ошибка открытия устройства (sn:{SerialNumber}): {err}")
                .WithData(nameof(SerialNumber), SerialNumber);

            Trace.TraceInformation("Открыта плата HackRfOne sn:{0} ptr:{1:x}", SerialNumber, device_ptr);

            return new(device_ptr, SerialNumber);
        }
    }
}
