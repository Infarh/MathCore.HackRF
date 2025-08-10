# MathCore.HackRF

[![.NET](https://img.shields.io/badge/.NET-9%20|%208-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/MathCore.HackRF.svg)](https://www.nuget.org/packages/MathCore.HackRF/)

C# библиотека для управления SDR-устройством HackRF One. Предоставляет высокоуровневую обёртку над нативной библиотекой hackrf.dll с безопасным управлением ресурсами.

## 🚀 Возможности

- Полный контроль над HackRF One через простой C# API
- Поддержка приёма и передачи данных в реальном времени
- Автоматическое управление ресурсами с SafeHandle
- Развёртка частот (линейная и псевдослучайная)
- Настройка всех радиопараметров (частота, усиление, фильтры)
- Поддержка множественных устройств

## 📋 Системные требования

- .NET 8.0 или .NET 9.0
- Windows x64
- HackRF One или совместимое устройство
- Установленные драйверы HackRF

## 🛠️ Структура проекта

```
MathCore.HackRF/
├── MathCore.HackRF/           # Основная библиотека
│   ├── HackRFLib.cs          # Главный класс с P/Invoke методами
│   ├── TransferInfo.cs       # Структура передачи данных
│   ├── DeviceListSafeHandle.cs # Безопасная работа с устройствами
│   └── *.cs                  # Перечисления и вспомогательные классы
├── Tests/
│   ├── MathCore.HackRFTests/ # Unit тесты
│   └── MathCore.HackRF.ConsoleTests/ # Консольные тесты
└── README.md                 # Документация репозитория
```

## 🔧 Сборка проекта

### Требования для разработки

- Visual Studio 2022 или JetBrains Rider
- .NET SDK 9.0
- Git

### Команды сборки

```bash
# Клонирование репозитория
git clone https://github.com/infarh/mathcore.hackrf.git
cd mathcore.hackrf

# Восстановление пакетов
dotnet restore

# Сборка решения
dotnet build

# Запуск тестов
dotnet test

# Создание NuGet пакета
dotnet pack --configuration Release
```

## 📦 NuGet пакет

Пакет доступен в NuGet Gallery:

```bash
dotnet add package MathCore.HackRF
```

## 🧪 Тестирование

Проект содержит два типа тестов:

- **Unit тесты** (`MathCore.HackRFTests`) - тестирование логики без реального оборудования
- **Консольные тесты** (`MathCore.HackRF.ConsoleTests`) - интеграционные тесты с реальным HackRF

Для запуска консольных тестов необходимо подключённое HackRF устройство.

## 📖 Документация

Подробная документация по использованию библиотеки находится в [README пакета](MathCore.HackRF/README.md).

## 🤝 Участие в разработке

1. Форкните репозиторий
2. Создайте ветку для новой функции (`git checkout -b feature/amazing-feature`)
3. Сделайте коммит изменений (`git commit -m 'Add amazing feature'`)
4. Отправьте изменения в ветку (`git push origin feature/amazing-feature`)
5. Создайте Pull Request

### Стандарты кодирования

- Используйте русские комментарии (см. [.github/copilot-instructions.md](.github/copilot-instructions.md))
- Следуйте соглашениям именования .NET
- Покрывайте новый код тестами
- Обновляйте XML документацию для публичных методов

## 📝 Лицензия

Проект распространяется под лицензией [MIT](https://opensource.org/licenses/MIT).

## 👨‍💻 Автор

**Шмачилин Павел**
- Email: shmachilin@yandex.ru
- GitHub: [@infarh](https://github.com/infarh)

## 🔗 Полезные ссылки

- [Официальная документация HackRF](https://hackrf.readthedocs.io/)
- [Драйверы HackRF](https://github.com/greatscottgadgets/hackrf)
- [NuGet пакет](https://www.nuget.org/packages/MathCore.HackRF/)
- [Issues и предложения](https://github.com/infarh/mathcore.hackrf/issues)