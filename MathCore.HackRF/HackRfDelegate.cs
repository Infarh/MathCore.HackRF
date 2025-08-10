using System.Runtime.InteropServices;

namespace MathCore.HackRF;

/// <summary>Делегат для callback-функции HackRF</summary>
/// <param name="transfer">Структура передачи данных</param>
/// <returns>Код завершения</returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int HackRfDelegate(ref TransferInfo transfer);