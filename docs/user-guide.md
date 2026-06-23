# Руководство пользователя
## Назначение
CryptoMarketAnalysis — система анализа криптовалютного рынка.
Система позволяет:
- загружать исторические данные по криптовалютам;
- хранить данные в PostgreSQL;
- выполнять аналитические расчеты;
- строить PDF-отчеты;
- выполнять массовую загрузку исторических данных (backfill).
---
# Предварительные требования
Для работы необходимо установить:
- .NET SDK 10
- PostgreSQL
- Git
  При использовании Docker:
- Docker
- Docker Compose
---
# Сборка проекта
Сборка решения:
```bash
dotnet build
```
Ожидаемый результат:
```
Build succeeded
```
---

## Запуск тестов

### Выполнение всех тестов:

```bash
dotnet test

Ожидаемый результат:
```
```
Test summary:
failed: 0
```
---

## Загрузка данных

### Обычная загрузка

Загружает данные за указанный период.

Пример:
```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  load \
  --symbols BTC \
  --symbols ETH \
  --from 2026-06-01 \
  --to 2026-06-10 \
  --source BINANCE
```
Пример результата:
```
Status: Success
Requested symbols: 2
Loaded points: 20
Skipped duplicates: 0
```
---

### Массовая загрузка данных (Backfill)

Используется для загрузки больших периодов.

Например, нескольких лет исторических данных.

Пример:
```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  load backfill \
  --symbols BTC \
  --symbols ETH \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --sources BINANCE \
  --batch-days 90
```
Пример результата:
```
Backfill started
Symbols: BTC, ETH
Sources: BINANCE
Period: 2022-01-01 — 2026-06-10
Batch days: 90
Status: Success
Loaded points: 1461
Skipped duplicates: 1783
Errors: 0
```
---

## Анализ изменения цены

Команда рассчитывает изменение цены за выбранный период.

Пример:
```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics price-change \
  --symbol BTC \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --source BINANCE
```
Пример результата:
```
Points count: 1622
Start price USD: 47,722.6500
End price USD: 61,510.9900
Absolute change USD: 13,788.3400
Percentage change: 28.8927%
```
---

## Анализ волатильности

Команда рассчитывает волатильность актива.

Пример:
```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics volatility \
  --symbol BTC \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --source BINANCE
```
Пример результата:
```
Points count: 1622
Returns count: 1621
Average return: 0.0518%
Volatility: 2.6900%
```
---

## Корреляционный анализ

Команда рассчитывает коэффициент корреляции Пирсона между двумя активами.

Пример:
```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics correlation \
  --base BTC \
  --quote ETH \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --source BINANCE
```
Пример результата:
```
Matched returns count: 1621
Pearson correlation: 0.839518
```
---

## Генерация PDF-отчета

Команда формирует PDF-отчет с аналитикой и графиками.

Пример:
```bash
mkdir -p reports
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  report pdf \
  --symbol BTC \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --source BINANCE \
  --correlation ETH \
  --output ./reports/btc-report.pdf
```
Пример результата:
```
PDF report created
File: ./reports/btc-report.pdf
Content-Type: application/pdf
Size: 125663 bytes
Points count: 1622
```
---

## Содержимое PDF-отчета

Отчет включает:

* метаданные;
* аналитическую сводку;
* график цены;
* график объема торгов;
* историческую таблицу;
* пояснения по расчетам.

Для больших периодов историческая таблица автоматически сокращается.

---

## Поддерживаемые активы

На текущем этапе поддерживаются:

* BTC
* ETH

---

## Поддерживаемые источники данных
```
Источник  | Требуется API Key
BINANCE	  | Нет
COINGECKO | Да
```
---

Рекомендуемый сценарий первого запуска

1. Выполнить сборку проекта.
```bash
dotnet build
```

2. Выполнить тесты.
```bash
dotnet test
```

3. Выполнить backfill.
```
load backfill
```
4. Проверить аналитику.
```
analytics price-change
analytics volatility
analytics correlation
```
5. Сформировать PDF-отчет.

report pdf

---

### Возможные проблемы

Загружено 0 точек

Причина:

* данные уже присутствуют в базе;
* все записи определены как дубликаты.

Проверить:

Skipped duplicates

---

Не создается PDF

Проверить:

* корректность пути сохранения;
* права на запись файла;
* наличие исторических данных.

---

Аналитика возвращает 0 точек

Проверить:

* загружены ли данные за выбранный период;
* корректно ли указан источник данных;
* корректно ли указан символ актива.

---

## Контакты проекта

Проект разработан в рамках практики Университета ИТМО.

Руководитель темы:

Повышев Владислав Вячеславович