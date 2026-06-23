# Entrypoints проекта

## Назначение

В проекте CryptoMarketAnalysis используется несколько entrypoint-проектов.

Каждый entrypoint отвечает за отдельный способ запуска системы:

* CryptoMarketAnalysis.Api — HTTP-интерфейс;
* CryptoMarketAnalysis.Cli — ручной запуск команд из терминала;
* CryptoMarketAnalysis.Worker — автоматические фоновые задачи по расписанию.

---

## CryptoMarketAnalysis.Api

### Роль

CryptoMarketAnalysis.Api предоставляет REST API для взаимодействия с системой через HTTP.

API используется для:

* получения справочников;
* получения исторических данных;
* запуска аналитических сценариев;
* генерации PDF-отчетов;
* демонстрации проекта через Swagger/OpenAPI.

### Особенности

API:

* использует Controller-based подход;
* подключает Swagger/OpenAPI;
* использует middleware для обработки ошибок;
* вызывает Application use cases;
* не содержит бизнес-логику;
* не обращается напрямую к внешним API;
* не обращается напрямую к низкоуровневой инфраструктуре вне зарегистрированных сервисов.

---

## CryptoMarketAnalysis.Cli

### Роль

CryptoMarketAnalysis.Cli предоставляет консольный интерфейс для ручного запуска сценариев.

### CLI используется для:

* загрузки данных;
* массовой backfill-загрузки;
* запуска аналитики;
* генерации PDF-отчетов;
* демонстрации проекта без HTTP API.

### Основные команды

* load
* load backfill
* analytics price-change
* analytics volatility
* analytics correlation
* report pdf

### Особенности

CLI:

* использует Spectre.Console.Cli;
* выводит результаты в виде таблиц и панелей;
* поддерживает progress reporting для backfill;
* вызывает Application use cases;
* не обращается напрямую к DbContext;
* не обращается напрямую к репозиториям;
* не обращается напрямую к CoinGecko/Binance provider-ам;
* не содержит бизнес-логику аналитики;
* не является Worker.

---

## CryptoMarketAnalysis.Worker

### Роль

CryptoMarketAnalysis.Worker предназначен для автоматического запуска фоновых задач.

На текущем этапе Worker реализует scheduled-загрузку рыночных данных.

### Worker используется для:

* автоматической ежедневной загрузки данных;
* обновления последних рыночных точек;
* повторного безопасного запуска без создания дублей;
* логирования результатов scheduled-загрузки.

### Основной сервис

DailyMarketDataLoadWorker

Сервис запускается как BackgroundService.

### Поведение

Worker:

1. Читает настройки из конфигурации.
2. Проверяет корректность настроек.
3. Определяет период загрузки.
4. Формирует LoadMarketDataRequest.
5. Вызывает ILoadMarketDataUseCase.
6. Логирует результат.
7. Ожидает следующий запуск.

### Конфигурация

Пример настройки:
```json
{
"Worker": {
"MarketDataLoad": {
"Enabled": true,
"IntervalMinutes": 1440,
"DaysBack": 1,
"Symbols": [
"BTC",
"ETH"
],
"MarketDataSourceCode": "BINANCE"
}
}
}
```

### Параметры

| Параметр | Описание |
|----------|----------|
| Enabled | Включает или отключает scheduled-загрузку. |
| IntervalMinutes | Интервал между запусками в минутах. |
| DaysBack | Сколько дней назад загружать относительно текущей даты UTC. |
| Symbols | Список активов для загрузки. |
| MarketDataSourceCode | Источник данных. Может быть BINANCE, COINGECKO или null для всех источников. |

### Production-настройка

Для обычного режима используется:
```json
IntervalMinutes = 1440
```
Это соответствует одному запуску в сутки.

### Development-настройка

Для ручной проверки можно временно использовать:
```json
IntervalMinutes = 1
```
Это позволяет увидеть несколько scheduled-запусков локально.

---

## Архитектурные ограничения Worker

Worker:

* не является CLI;
* не принимает пользовательские команды;
* не содержит бизнес-логику;
* не обращается напрямую к DbContext;
* не обращается напрямую к репозиториям;
* не обращается напрямую к CoinGecko/Binance provider-ам;
* вызывает только Application use cases.

Правильная схема:
```
Worker hosted service
↓
Application use case
↓
Application abstractions
↓
Infrastructure
↓
PostgreSQL / External APIs
```
---

## Проверка Worker

### Запуск:
```bash
dotnet run --project src/CryptoMarketAnalysis/CryptoMarketAnalysis.Worker
```
Ожидаемые логи:
```
Daily market data load worker started.
Daily market data load started.
Daily market data load completed.
Scheduled load result.
Next daily market data load will start in ...
```
При повторном запуске уже существующие точки не добавляются повторно, а учитываются как SkippedDuplicatesCount.

---

## Итог

В проекте реализованы три независимых entrypoint-а:

| Entrypoint| Назначение |
|-----------|------------|
| API | HTTP-доступ к функциям системы. |
| CLI | Ручной запуск команд и демонстрация без HTTP. |
| Worker | Автоматическая scheduled-загрузка данных. |

Все entrypoint-проекты используют Application layer и не дублируют бизнес-логику.