# QR Code Manager (QRTwin)

Кросс-платформенное приложение для сканирования и генерации QR-кодов на **.NET 10** и **.NET MAUI**.

## Возможности

- Сканирование QR-кодов через камеру (ZXing.Net.Maui)
- Генерация QR-кодов из текста или URL
- Поделиться и сохранить сгенерированный QR-код
- Локальная история (SQLite)
- Тёмная тема, адаптивный макет
- SVG-иконки через SkiaSharp + Svg.Skia

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 17.14+ с workload **.NET Multi-platform App UI development**
- Для Android: Android SDK (API 21+)
- Для Windows: Windows 10/11 SDK

## Запуск

### Windows

```bash
cd "C:\SHILY PROJECTS\QRTwin"
dotnet build src/QRTwin.Maui/QRTwin.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet run --project src/QRTwin.Maui/QRTwin.Maui.csproj -f net10.0-windows10.0.19041.0
```

Или откройте `QRTwin.slnx` в Visual Studio / Rider и запустите проект с профилем **Windows Machine**.

### Android

Подключите устройство или эмулятор, затем:

```bash
dotnet build src/QRTwin.Maui/QRTwin.Maui.csproj -f net10.0-android
dotnet build -t:Run -f net10.0-android src/QRTwin.Maui/QRTwin.Maui.csproj
```

При первом запуске на Android приложение запросит разрешение на использование камеры.

## Структура проекта

```
src/QRTwin.Maui/
├── Models/           # Модели данных
├── ViewModels/       # MVVM (CommunityToolkit.Mvvm)
├── Views/            # XAML-представления
├── Services/         # Бизнес-логика, SQLite, QR
├── Controls/         # SvgIconView и др.
├── Converters/       # XAML-конвертеры
├── Behaviors/        # Поведения UI
├── Helpers/          # Вспомогательные классы
└── Resources/        # Стили, SVG-иконки, шрифты
```

## Используемые пакеты

| Пакет | Назначение |
|-------|-----------|
| CommunityToolkit.Mvvm | MVVM |
| CommunityToolkit.Maui | FileSaver, UI toolkit |
| ZXing.Net.Maui.Controls | Сканирование и генерация QR |
| SkiaSharp.Extended.UI.Maui | SkiaSharp в MAUI |
| Svg.Skia | Отрисовка SVG-иконок |
| sqlite-net-pcl | Локальная история |

## Лицензия

См. [LICENSE](LICENSE).
