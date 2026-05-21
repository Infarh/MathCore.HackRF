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
- Добавлен прикладной режим `--mode fmscan` для поиска FM станций по уровню спектральной мощности
- Реализована FFT обработка IQ блоков и детекция пиков в диапазоне 70-110 МГц
- Добавлено подавление DC/LO артефакта (`--f-dc-reject-khz`)
- Оформлен пример использования: `docs/examples/fm-band-scan.md`
- Добавлен режим `--mode switch` (быстрое чередование RX -> TX на одном устройстве)
- В сценарии `switch`: захват IQ, расчёт средней мощности, передача обратно захваченного сигнала
- Исправлены TX-метрики: `underrun` не считается при успешном producer, producer-блоки учитываются в `dequeued`
- Консольный тест расширен режимами `--mode rx|tx`
- Добавлен TX стресс-сценарий на одном устройстве (очередь + feeder + метрики underrun)
- Добавлена overflow-политика `DropOldest` для RX очереди
- Консольный тест расширен параметрами нагрузки: `--seconds`, `--processing-delay-ms`, `--queue`, `--drop-oldest`
- Исправлена критичная семантика bounded-очереди: `Channel` переведён с `DropWrite` на `Wait` для явного контроля переполнения через `TryWrite`
- Добавлены компоненты передачи: `DeviceTxSession`, `TxSessionOptions`, `TxSessionStatistics`, `TxBlockProducer`
- Добавлена `DeviceRelaySession` для сценария RX устройства A -> TX устройства B
- Внедрён новый high-level API `Device.StartRxSession(...)` для безопасного потокового приёма
- Расширен API `Device`: `StartTxSession(...)` и `StartRelaySession(...)`
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

## Аппаратная проверка (Smoke)
- Устройство: HackRF One, serial `000000000000000015a863dc24604b87`
- Сценарий: RX через `DeviceRxSession` в течение ~5 сек
- Параметры: 433 МГц, sample rate 10 МГц, BW 10 МГц, LNA 32 дБ, VGA 40 дБ
- Результат: `recv=386`, `proc=386`, `drop=0`, средняя скорость ~9.99 МГц
- Вывод: базовый session-слой подтверждён на реальном устройстве, признаков overrun в тестовом окне не выявлено

## Аппаратная проверка (Stress)
- Сценарий: RX через `DeviceRxSession`, искусственная задержка обработки `20ms`, очередь `16`, политика `DropOldest`, длительность ~5 сек
- Результат: `recv=385`, `proc=238`, `drop=131`, `q≈15..16`
- Вывод: переполнение очереди детектируется корректно, статистика дропов соответствует нагрузке

## Аппаратная проверка (TX)
- Сценарий: `--mode tx --seconds 5 --queue 16 --tx-feed-delay-ms 20 --tx-vga 0`
- Результат: `dequeued=161`, `underrun=223`, `enqueued=161`, `dropped=0`
- Вывод: underrun в TX корректно наблюдается и измеряется при медленной подаче блоков в очередь

## Быстрые команды запуска
- RX базовый: `dotnet run --project .\\Tests\\MathCore.HackRF.ConsoleTests\\MathCore.HackRF.ConsoleTests.csproj -c Debug -- --mode rx --seconds 5`
- RX стресс: `dotnet run --project .\\Tests\\MathCore.HackRF.ConsoleTests\\MathCore.HackRF.ConsoleTests.csproj -c Debug -- --mode rx --seconds 5 --processing-delay-ms 20 --queue 16 --drop-oldest`
- TX стресс: `dotnet run --project .\\Tests\\MathCore.HackRF.ConsoleTests\\MathCore.HackRF.ConsoleTests.csproj -c Debug -- --mode tx --seconds 5 --queue 16 --tx-feed-delay-ms 20 --tx-vga 0`
- RX/TX switch: `dotnet run --project .\\Tests\\MathCore.HackRF.ConsoleTests\\MathCore.HackRF.ConsoleTests.csproj -c Debug -- --mode switch --cycles 5 --rx-ms 300 --tx-ms 300 --queue 64 --max-capture-blocks 128 --tx-vga 0`

## Аппаратная проверка (RX/TX switch)
- Сценарий: `--mode switch --cycles 3 --rx-ms 300 --tx-ms 300 --queue 64 --max-capture-blocks 128 --tx-vga 0`
- Результат: по циклам стабильно `RX recv=23/proc=23/drop=0`, `TX dequeued=23/underrun=0`
- Вывод: быстрые переключения RX->TX отрабатывают штатно, capture-and-replay работает, базовые проблемы переключения в этом профиле не воспроизводятся

## Аппаратная проверка (FM scan)
- Сценарий: `--mode fmscan --f-start-mhz 70 --f-stop-mhz 110 --f-step-mhz 1 --f-bin-khz 25 --f-threshold-db 7 --f-blocks 12 --f-lna 32 --f-vga 16 --f-dc-reject-khz 250`
- Результаты близки к наблюдениям в SDR#: `97.2`, `98.825`, `100.475`, `101.8`, `102.5`, `102.975`, `104.2` МГц
- Обнаружен шумовой участок около `99.65-100.05` МГц, что соответствует наблюдаемой помехе в диапазоне `99.6-100.1` МГц

## Как восстановить контекст на другой машине
1. Открыть эту ветку и файл docs/streaming-architecture-tracking.md
2. Сверить изменения в Device.cs и Device.TxRx.cs
3. Продолжать с раздела "Ближайшие шаги" по порядку
4. После каждого шага обновлять этот файл: что сделано, какие метрики и какие риски закрыты
