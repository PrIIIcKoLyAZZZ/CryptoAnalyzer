# NuGet licenses

Документ фиксирует ключевые NuGet-пакеты, используемые в проекте CryptoMarketAnalysis.

Проект является учебным проектом в рамках университетской практики.

---

## QuestPDF

Назначение:

PDF generation в Infrastructure-слое.

Использование в проекте:

- генерация PDF-отчетов;
- используется только в `CryptoMarketAnalysis.Infrastructure`.

Лицензия:

QuestPDF Community License.

Ограничения:

- библиотека имеет отдельные условия Community License;
- для коммерческого использования могут потребоваться дополнительные условия или коммерческая лицензия.

Допустимость:

Для учебного / academic проекта использование допустимо в рамках Community License.

---

## Spectre.Console

Назначение:

Красивый консольный вывод.

Использование в проекте:

- таблицы;
- панели;
- статус-сообщения CLI.

Лицензия:

MIT License.

Допустимость:

MIT License допускает использование в учебных проектах.

---

## Spectre.Console.Cli

Назначение:

CLI command framework.

Использование в проекте:

- команды `load`;
- команды `analytics`;
- команды `report`.

Лицензия:

MIT License.

Допустимость:

MIT License допускает использование в учебных проектах.

---

## FluentAssertions

Назначение:

Удобные assertions в unit-тестах.

Использование в проекте:

- Application tests;
- Infrastructure tests.

Лицензия:

Xceed Fluent Assertions Community License.

Ограничения:

- бесплатное использование разрешено для non-commercial use;
- для commercial use требуется активная подписка.

Допустимость:

Проект учебный и некоммерческий, поэтому использование допустимо в рамках Community License.

---

## Microsoft Entity Framework Core

Назначение:

ORM и доступ к PostgreSQL.

Использование в проекте:

- Infrastructure persistence;
- repositories;
- DbContext;
- entity configuration.

Лицензия:

MIT License.

Допустимость:

MIT License допускает использование в учебных проектах.

---

## Npgsql

Назначение:

PostgreSQL driver для .NET.

Использование в проекте:

- подключение к PostgreSQL;
- работа EF Core provider-а.

Лицензия:

PostgreSQL License.

Допустимость:

PostgreSQL License является permissive-лицензией и допускает использование в учебных проектах.

---

## Npgsql.EntityFrameworkCore.PostgreSQL

Назначение:

EF Core provider для PostgreSQL.

Использование в проекте:

- подключение EF Core к PostgreSQL;
- Infrastructure persistence.

Лицензия:

PostgreSQL License.

Допустимость:

PostgreSQL License допускает использование в учебных проектах.

---

## xUnit

Назначение:

Unit testing framework.

Использование в проекте:

- Application tests;
- Infrastructure tests.

Лицензия:

Apache License 2.0.

Допустимость:

Apache License 2.0 допускает использование в учебных проектах.

---

## Moq

Назначение:

Mocking framework для unit-тестов.

Использование в проекте:

- моки use case dependencies;
- моки repositories;
- моки providers.

Лицензия:

BSD-3-Clause License.

Допустимость:

BSD-3-Clause License допускает использование в учебных проектах.

---

## Swashbuckle.AspNetCore

Назначение:

Swagger/OpenAPI generation.

Использование в проекте:

- Swagger UI;
- OpenAPI specification для REST API.

Лицензия:

MIT License.

Допустимость:

MIT License допускает использование в учебных проектах.

---

## Microsoft.Extensions.*

Назначение:

DI, configuration, logging, hosting.

Использование в проекте:

- REST API composition root;
- CLI composition root;
- Infrastructure registration.

Лицензия:

MIT License.

Допустимость:

MIT License допускает использование в учебных проектах.

---

## Итог

Использование перечисленных пакетов допустимо для текущего учебного проекта.

Особо отмечены пакеты с заметными лицензионными условиями:

- QuestPDF;
- FluentAssertions.

Для коммерческого использования проекта потребуется отдельная повторная проверка лицензий.