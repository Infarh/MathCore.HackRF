using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace MathCore.HackRF;

/// <summary>Структура, представляющая передачу данных HackRF</summary>
/// <summary>Структура, представляющая передачу данных HackRF</summary>
/// <param name="DevicePtr">Указатель на устройство HackRF</param>
/// <param name="BufferPtr">Указатель на буфер данных</param>
/// <param name="BufferLength">Общий размер буфера</param>
/// <param name="ValidLength">Длина валидных данных в буфере</param>
/// <param name="RxContextPtr">Указатель на контекст приёма</param>
/// <param name="TxContextPtr">Указатель на контекст передачи</param>
public readonly record struct TransferInfo(
    nint DevicePtr,
    nint BufferPtr,
    int BufferLength,
    int ValidLength,
    nint RxContextPtr,
    nint TxContextPtr)
{
    public Span<byte> RxBytes => BufferPtr != 0 && ValidLength != 0
        ? MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), BufferPtr), ValidLength)
        : [];

    public Span<byte> BufferBytes => BufferPtr != 0 && BufferLength != 0
        ? MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), BufferPtr), BufferLength)
        : [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetRxContext<T>() where T : unmanaged => RxContextPtr != 0
        ? Marshal.PtrToStructure<T>(RxContextPtr)
        : default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetTxContext<T>() where T : unmanaged => TxContextPtr != 0
        ? Marshal.PtrToStructure<T>(TxContextPtr)
        : default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(byte[] buffer, int offset, int length) => Marshal.Copy(BufferPtr, buffer, offset, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(int PtrOffset, byte[] buffer, int offset, int length) =>
        Marshal.Copy(BufferPtr + PtrOffset, buffer, offset, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(byte[] buffer, int offset, int length) => Marshal.Copy(buffer, offset, BufferPtr, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(int PtrOffset, byte[] buffer, int offset, int length) =>
        Marshal.Copy(buffer, offset, BufferPtr + PtrOffset, length);
}
