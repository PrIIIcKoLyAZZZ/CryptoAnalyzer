# CryptoMarketAnalysis

Система анализа криптовалютного рынка на основе открытых данных криптовалютных бирж.

Проект разработан в рамках практики Университета ИТМО.

---

# О проекте

CryptoMarketAnalysis получает исторические данные из открытых источников, сохраняет их в PostgreSQL, выполняет аналитическую обработку и формирует отчеты.

Основные возможности:

- загрузка исторических рыночных данных;
- хранение данных в PostgreSQL;
- анализ изменения цены;
- расчет волатильности;
- корреляционный анализ;
- генерация PDF-отчетов;
- массовая загрузка данных (backfill);
- REST API;
- CLI-интерфейс.

---

# Тема практики

**Разработка системы анализа криптовалютного рынка на основе открытых данных криптовалютных бирж**

Руководитель:

**Повышев Владислав Вячеславович**

---

# Технологический стек

- C# (.NET 10)
- PostgreSQL
- Entity Framework Core
- REST API
- Swagger / OpenAPI
- Spectre.Console
- QuestPDF
- CoinGecko API
- Binance API

---

# Архитектура

Проект построен по принципам Clean Architecture.

```text
src/
├── Api
├── Application
│   ├── Contracts
│   └── Abstractions
├── Infrastructure
├── Cli
├── Worker
└── Domain
```

Подробнее:

```text
docs/architecture/architecture.md
```

---

# Поддерживаемые источники данных

| Источник | API Key |
|-----------|-----------|
| BINANCE | Не требуется |
| COINGECKO | Требуется Demo API Key |

---

# Поддерживаемые активы

На текущем этапе:

- BTC
- ETH

---

# Сборка

```bash
dotnet build
```

---

# Тестирование

```bash
dotnet test
```

На текущем этапе:

```text
93 теста
0 ошибок
```

---

# Быстрый старт

## 1. Загрузить данные

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

---

## 2. Выполнить анализ изменения цены

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics price-change \
  --symbol BTC \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --source BINANCE
```

---

## 3. Выполнить анализ волатильности

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics volatility \
  --symbol BTC \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --source BINANCE
```

---

## 4. Выполнить корреляционный анализ

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics correlation \
  --base BTC \
  --quote ETH \
  --from 2022-01-01 \
  --to 2026-06-10 \
  --source BINANCE
```

---

## 5. Сформировать PDF-отчет

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

---

# Возможности CLI

Поддерживаются команды:

```text
load
load backfill
analytics price-change
analytics volatility
analytics correlation
report pdf
```

Подробная документация:

```text
docs/cli/commands.md
```

---

# Документация

Архитектура:

```text
docs/architecture/architecture.md
```

Руководство пользователя:

```text
docs/user-guide.md
```

CLI:

```text
docs/cli/commands.md
```

Лицензии:

```text
docs/licenses/nuget-licenses.md
```

---

# Реализовано

- PostgreSQL persistence
- CoinGecko provider
- Binance provider
- REST API
- Swagger
- Price Change Analysis
- Volatility Analysis
- Correlation Analysis
- PDF Report Generation
- SVG Charts
- CLI
- Backfill Loading
- Batch Processing
- Progress Reporting
- Unit Tests

---

# Планируемые улучшения

- автоматическая загрузка через Worker;
- планировщик обновления данных;
- дополнительные аналитические показатели;
- обнаружение аномалий;
- прогнозирование временных рядов;
- визуализация через Web UI.

---

# Лицензии

Проект использует сторонние библиотеки с открытыми лицензиями.

Подробности:

```text
docs/licenses/nuget-licenses.md
```

Особое внимание следует обратить на:

- QuestPDF Community License;
- FluentAssertions Community License.