# CryptoMarketAnalysis CLI commands

CLI — консольный интерфейс для запуска сценариев проекта без REST API.

CLI использует Application use cases и не обращается напрямую к БД, репозиториям или внешним provider-ам.

---

## load

### Назначение

Загрузка исторических рыночных данных по одному или нескольким активам.

### Пример

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  load --symbols BTC --symbols ETH --from 2026-06-01 --to 2026-06-10 --source BINANCE
```

### Параметры

| Параметр | Обязательный | Описание |
|---|---:|---|
| `--symbols` | Да | Символ актива. Для нескольких активов параметр повторяется. |
| `--from` | Да | Начало периода в UTC. |
| `--to` | Да | Конец периода в UTC. |
| `--source` | Нет | Источник данных: `BINANCE` или `COINGECKO`. Если не указан, используются все provider-ы. |

### Пример успешного вывода

```text
Status: Success
Requested symbols: 2
Loaded points: 0
Skipped duplicates: 20

BTC BINANCE Loaded=0 Skipped=10 Error=OK
ETH BINANCE Loaded=0 Skipped=10 Error=OK
```

### Типичные ошибки

| Ошибка | Причина |
|---|---|
| `At least one symbol must be provided` | Не указан `--symbols`. |
| `--from must be earlier than --to` | Начало периода больше или равно концу периода. |
| Provider/source not found | Указан неизвестный источник данных. |

---

---

## load backfill

### Назначение

Массовая загрузка исторических рыночных данных за большой период с разбиением на батчи.

Команда предназначена для backfill-сценариев, когда нужно загрузить данные за месяцы или годы.

В отличие от обычной команды `load`, backfill:

- разбивает период на несколько батчей;
- показывает progress;
- агрегирует результат по каждому активу и источнику;
- безопасно обрабатывает повторную загрузку через механизм пропуска дублей.

### Пример

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  load backfill \
  --symbols BTC \
  --symbols ETH \
  --from 2026-06-01 \
  --to 2026-06-10 \
  --sources BINANCE \
  --batch-days 5
```

### Пример для большого периода

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

### Параметры

| Параметр | Обязательный | Описание |
|---|---:|---|
| `--symbols` | Да | Символ актива. Для нескольких активов параметр повторяется. |
| `--from` | Да | Начало периода в UTC. |
| `--to` | Да | Конец периода в UTC. |
| `--sources` | Да | Источник данных. Для нескольких источников параметр повторяется. |
| `--batch-days` | Нет | Размер одного батча в днях. По умолчанию используется `30`. |

### Пример успешного вывода

```text
Backfill started
Symbols: BTC, ETH
Sources: BINANCE
Period: 2026-06-01 — 2026-06-10
Batch days: 5
Total batches per source: 2

Backfill summary
Status: Success
Loaded points: 0
Skipped duplicates: 20
Errors: 0

Source   Symbol   Batches   Loaded   Skipped   Errors
BINANCE  BTC      2         0        10        0
BINANCE  ETH      2         0        10        0
```

### Типичные ошибки

| Ошибка | Причина |
|---|---|
| `At least one --symbols value is required.` | Не указан ни один символ актива. |
| `At least one --sources value is required.` | Не указан ни один источник данных. |
| `--from must be less than or equal to --to.` | Дата начала больше даты окончания. |
| `--batch-days must be greater than zero.` | Размер батча меньше или равен нулю. |
| Ошибка внешнего API | Внешний источник временно недоступен или сработал rate limit. |

### Особенности

Период разбивается на включительные батчи.

Пример:

```text
from = 2026-06-01
to = 2026-06-10
batch-days = 5
```

Будут сформированы батчи:

```text
2026-06-01 — 2026-06-05
2026-06-06 — 2026-06-10
```

Повторный запуск той же команды не должен создавать дубли в БД. Уже существующие точки учитываются как `Skipped duplicates`.

## analytics price-change

### Назначение

Расчет изменения цены актива за период.

### Пример

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics price-change --symbol BTC --from 2026-06-01 --to 2026-06-10 --source BINANCE
```

### Параметры

| Параметр | Обязательный | Описание |
|---|---:|---|
| `--symbol` | Да | Символ актива. |
| `--from` | Да | Начало периода в UTC. |
| `--to` | Да | Конец периода в UTC. |
| `--source` | Нет | Источник данных. |

### Пример успешного вывода

```text
Symbol: BTC
Source: BINANCE
Points count: 10
Start price USD: 71,408.9000
End price USD: 61,510.9900
Absolute change USD: -9,897.9100
Percentage change: -13.8609%
```

### Типичные ошибки

| Ошибка | Причина |
|---|---|
| `--symbol is required` | Не указан символ актива. |
| `--from is required` | Не указан период начала. |
| `--to is required` | Не указан период окончания. |

---

## analytics volatility

### Назначение

Расчет волатильности актива по доходностям между соседними точками.

Волатильность в текущей версии не annualized.

### Пример

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics volatility --symbol BTC --from 2026-06-01 --to 2026-06-10 --source BINANCE
```

### Параметры

| Параметр | Обязательный | Описание |
|---|---:|---|
| `--symbol` | Да | Символ актива. |
| `--from` | Да | Начало периода в UTC. |
| `--to` | Да | Конец периода в UTC. |
| `--source` | Нет | Источник данных. |

### Пример успешного вывода

```text
Symbol: BTC
Source: BINANCE
Points count: 10
Returns count: 9
Average return: -1.6016%
Volatility: 3.0735%
```

---

## analytics correlation

### Назначение

Расчет корреляции Пирсона между доходностями двух активов.

Корреляция считается по доходностям, а не по абсолютным ценам.

### Пример

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  analytics correlation --base BTC --quote ETH --from 2026-06-01 --to 2026-06-10 --source BINANCE
```

### Параметры

| Параметр | Обязательный | Описание |
|---|---:|---|
| `--base` | Да | Первый актив. |
| `--quote` | Да | Второй актив. |
| `--from` | Да | Начало периода в UTC. |
| `--to` | Да | Конец периода в UTC. |
| `--source` | Нет | Источник данных. |

### Пример успешного вывода

```text
Base symbol: BTC
Quote symbol: ETH
Source: BINANCE
Base points count: 10
Quote points count: 10
Matched returns count: 9
Pearson correlation: 0.898438
```

---

## report pdf

### Назначение

Генерация PDF-отчета по историческим данным и аналитике.

### Пример

```bash
dotnet run --project src/CryptoMarketAnalysis/Cli/CryptoMarketAnalysis.Cli -- \
  report pdf --symbol BTC --from 2026-06-01 --to 2026-06-10 --source BINANCE --correlation ETH --output ./reports/btc-report.pdf
```

### Параметры

| Параметр | Обязательный | Описание |
|---|---:|---|
| `--symbol` | Да | Основной актив отчета. |
| `--from` | Да | Начало периода в UTC. |
| `--to` | Да | Конец периода в UTC. |
| `--source` | Нет | Источник данных. |
| `--correlation` | Нет | Второй актив для расчета корреляции. |
| `--output` | Нет | Путь сохранения PDF. Если не указан, используется имя файла из use case. |

### Пример успешного вывода

```text
PDF report created
File: ./reports/btc-report.pdf
Content-Type: application/pdf
Size: 35626 bytes
Points count: 10
```

### Типичные ошибки

| Ошибка | Причина |
|---|---|
| Не удалось создать файл | Некорректный путь или нет прав на запись. |
| PDF пустой | Ошибка генерации отчета или отсутствует content в response. |
| Points count = 0 | Нет исторических данных за выбранный период. |

---

## Архитектурные ограничения CLI

CLI:

- вызывает Application use cases;
- не обращается напрямую к `DbContext`;
- не обращается напрямую к repositories;
- не обращается напрямую к CoinGecko/Binance provider-ам;
- не содержит бизнес-логику аналитики;
- не генерирует PDF самостоятельно;
- не является Worker.