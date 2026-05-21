namespace MathCore.HackRF.Streaming;

/// <summary>Метод обработки одного блока принятых IQ-данных</summary>
/// <param name="RxBlock">Блок данных приёма</param>
/// <param name="Metadata">Метаданные блока</param>
public delegate void RxBlockProcessor(ReadOnlySpan<byte> RxBlock, in RxBlockMetadata Metadata);

/// <summary>Метаданные принятого блока данных</summary>
/// <param name="SequenceId">Порядковый номер блока</param>
/// <param name="TimestampUtc">Время приёма блока UTC</param>
/// <param name="ValidLength">Длина валидных данных блока</param>
public readonly record struct RxBlockMetadata(long SequenceId, DateTime TimestampUtc, int ValidLength);
