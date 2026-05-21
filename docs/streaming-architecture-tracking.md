# Трекинг рефакторинга потоковой архитектуры MathCore.HackRF

Дата старта: 2026-05-21
Ветка: Device
Базовая ветка: dev

## Цель
Сформировать высокопроизводительную объектную модель для real-time RX/TX, скрывающую сырой pInvoke/указатели за Span/Memory API, и поддержать сценарии:
- RX -> обработка -> TX между двумя устройствами
- IQ -> фильтрация -> децимация -> фазовая демодуляция -> аудио
- запись потока в UDP/файл
- математический анализ потока (поиск пиков, спектральный анализ)
- синтез и передача математически сформированного сигнала

## Зафиксированные риски (до рефакторинга)
P0:
- Потеря ссылки на callback-делегат RX/TX (риск GC и падения в unmanaged callback)
- Закрытие устройства в Dispose без гарантированной остановки активного стриминга

P1:
- Смешение high-level API и низкоуровневых деталей в одном классе Device
- Нет явного конвейера backpressure для защиты от overrun/underrun при длительной обработке
- Нет телеметрии реального времени (drop, latency, queue depth)

P2:
- Ограниченная тестируемость real-time поведения (почти нет unit/integration тестов для RX/TX)
- Нет удобного composable API для DSP-конвейеров

## Сделано в этой сессии
- Внедрён новый high-level API `Device.StartRxSession(...)` для безопасного потокового приёма
- Добавлен `DeviceRxSession` с bounded-очередью (Channel) между callback и обработчиком
- Добавлена телеметрия сессии (`RxSessionStatistics`): received/dropped/processed, bytes, queue length, last/max processing time
- Горячий путь обработки переводится через `ReadOnlySpan<byte>` в пользовательский процессор
- Миграция всех проектов решения на .NET 10.0 (библиотека, unit-тесты, console-тесты)
- Обновление требований .NET в README репозитория и README пакета
- Успешная сборка решения после миграции (net10.0)
- Удержание callback-делегатов в полях Device для исключения преждевременного GC
- Запрет запуска RX/TX при любом активном режиме, кроме OFF
- Допуск кодов корректной остановки стриминга (StreamingStopped, StreamingExitCalled)
- Обязательная остановка RX/TX в Dispose перед Close
- Очистка callback-ссылок после остановки/освобождения

## Целевая модель (этапы)

### Этап 1. Слой безопасного транспорта
- Частично выполнено: реализован `DeviceRxSession` для RX
- В работе: вынесение общего `StreamSession` для RX/TX
- В работе: буферные политики (сейчас `DropNewest`, далее `DropOldest`/ring-buffer)
- Частично выполнено: `RxSessionStatistics`
- Частично выполнено: контролируемая остановка через `Dispose`

### Этап 2. Конвейер обработки
- Ввести интерфейсы:
  - IIqSource
  - IIqProcessor
  - IIqSink
- Реализовать адаптеры:
  - DeviceRxSource
  - DeviceTxSink
  - UdpSink
  - FileSink
- Ввести zero-copy контракт на основе ReadOnlySpan<byte>/Span<byte> для горячего пути

### Этап 3. High-level API для сценариев
- RelaySession (RX устройства A -> обработка -> TX устройства B)
- DspAudioSession (IQ -> DSP -> Audio)
- CaptureSession (IQ -> UDP/файл)
- AnalysisSession (IQ -> детекторы/спектр)
- SignalSynthesisSession (генератор -> TX)

### Этап 4. DX и стабильность
- Простые property-based настройки устройства (частота, sample rate, усиления)
- Преднастроенные профили режимов
- Интеграционные тесты на latency/backpressure
- Примеры для script-first использования

## Предлагаемый минимальный публичный API (черновик)
- HackRfRuntime.Initialize();
- var rx = Device.OpenRx(serial);
- var tx = Device.OpenTx(serial);
- using var relay = RelaySession.Start(rx, tx, options);
- relay.Configure(pipeline => pipeline.Filter(...).Decimate(...).DemodulateFm(...));

## KPI для контроля качества
- Отсутствие unmanaged callback с потерей делегата
- Нулевые необработанные исключения в callback-пути
- Контролируемый drop policy без стопа процесса
- Предсказуемая задержка в рамках заданного бюджета

## Ближайшие шаги
1. Выделить отдельный Session API и отделить его от Device
2. Внедрить bounded очередь/кольцевой буфер и политику backpressure
3. Добавить StreamStats + события диагностики
4. Написать интеграционный тест на сценарий RX->TX relay

## Как восстановить контекст на другой машине
1. Открыть эту ветку и файл docs/streaming-architecture-tracking.md
2. Сверить изменения в Device.cs и Device.TxRx.cs
3. Продолжать с раздела "Ближайшие шаги" по порядку
4. После каждого шага обновлять этот файл: что сделано, какие метрики и какие риски закрыты
