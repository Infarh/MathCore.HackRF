namespace MathCore.HackRF.Streaming;

/// <summary>Метод формирования блока данных для передачи</summary>
/// <param name="TxBlock">Буфер блока передачи для заполнения</param>
/// <param name="Metadata">Метаданные блока</param>
public delegate void TxBlockProducer(Span<byte> TxBlock, in TxBlockMetadata Metadata);

/// <summary>Метаданные блока передачи</summary>
/// <param name="SequenceId">Порядковый номер блока</param>
/// <param name="TimestampUtc">Время выдачи блока UTC</param>
/// <param name="BufferLength">Длина буфера блока</param>
public readonly record struct TxBlockMetadata(long SequenceId, DateTime TimestampUtc, int BufferLength);
