# .NET Agentic Acceptance

Методология ускорения инженерных практик через AI-агентов. Audit, review и guardrails, которые раньше требовали дорогой экспертизы, теперь масштабируются.

[🇬🇧 English version](README.md)

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![License MIT](https://img.shields.io/badge/License-MIT-green.svg)
![CI](https://github.com/svetkis/agentic-acceptance-dotnet/workflows/Examples%20CI/badge.svg)

> Принципы (Decision Guards, Engineering Assurance Levels, гигиена промптов) не привязаны к стеку, но все готовые артефакты в этом репозитории — только для .NET. Для других стеков извлекайте принципы; рабочих артефактов для них здесь нет.

Репозиторий содержит готовые артефакты для .NET-проектов: правила, **скиллы** (инструкция роли агента + чек-лист, напр. security-audit), тестовые паттерны и CI-воркфлоу.

> **Впервые видишь методологию?** Канонический путь: [GLOSSARY.md](GLOSSARY.md) (термины) →
> [Как это работает](#как-это-работает) (модель уровней) → [docs/README.md](docs/README.md)
> (карта знаний) → [docs/ONBOARDING.md](docs/ONBOARDING.md) (применить к своему проекту).

## Проблема

AI-агенты (Cursor, Claude, Copilot) ускоряют написание кода, но генерируют скрытый техдолг, нарушают архитектурные границы и ломают безопасность. Ручное ревью такого кода становится бутылочным горлышком.

**Agentic Acceptance** — методология проверки сгенерированного кода по принципу «доверяй, но проверяй» (аналогия с Zero Trust: никакой артефакт агента не считается корректным без детерминированной проверки). Контроль переносится из вероятностных промптов в детерминированные пайплайны.

## Как это работает

Модель контроля — **Engineering Assurance Levels**. Артефакт классифицируется по
области проверки, а не по месту запуска: unit-тест не становится System Check
только потому, что запущен в CI.

| Уровень | Когда срабатывает | Что входит | Главный вопрос |
|---------|-------------------|------------|----------------|
| **Control Foundation** | До изменения кода | `AGENTS.md`, architecture boundaries, Decision Guards, policies | Какие ограничения и решения уже приняты? |
| **1. Change Checks** | IDE, build, pre-commit | Компилятор, nullable, анализаторы, formatting, banned APIs | Может ли изменение технически существовать? |
| **2. Behavior Checks** | Локальный или CI test run | Unit, regression, contract, архитектурные тесты, ratchets; уровень замыкает **ревью агента** (гейт перед PR) | Сохранились ли ожидаемые свойства и поведение? |
| **3. System Checks** | PR, CI, release pipeline | Integration, характеризующие, E2E, smoke, Testcontainers, нагрузочные (NBomber), deployment verification | Работает ли система целиком? |
| **4. Reality Checks** | По расписанию или risk-trigger | LLM-аудиты (security, database, performance, UX, API, i18n, tech-debt), дрейф сложности (когнитивная/цикломатическая через baseline + ratchet), устаревшие и уязвимые зависимости | Какие свойства кодовой базы дрейфуют со временем и не видны на уровне отдельного изменения? |

Отдельные процессы, не являющиеся уровнями:

- **Engineering Governance** — принятие остаточного риска, release decision, бизнес- и продуктовые решения.
- **Control Maintenance** — актуализация инструкций, agent memory, backlog, baselines, suppressions и самих guardrails (скиллы `memory-hygiene`, `doc-hygiene`, `backlog-hygiene`).

> **Доказательная база:** метрики эффективности и ROI уровней —
> [`docs/EVIDENCE.md`](docs/EVIDENCE.md).

### Карта артефактов по уровням

| Уровень / процесс | Артефакты репозитория |
|-------------------|-----------------------|
| Control Foundation | `rules/AGENTS_TEMPLATE.md` (+ efcore/dapper add-ons), `rules/CONVENTIONS.md`, Decision Guards (`PERF-###`/`DB-###`) |
| 1. Change Checks | Banned APIs, Roslyn-анализаторы (`examples/DemoProject/src/DemoProject.Analyzers/`), `ci/github-actions/safe-ci.yml` |
| 2. Behavior Checks | `tests/patterns/` (Ratchet, NetArchTest, Snapshot, Analyzer tests), `tests/conventions/`, `templates/skills/code-review/`, `templates/skills/task-compliance/` |
| 3. System Checks | E2E/smoke паттерны, NBomber (`tests/patterns/LoadTest.cs`) |
| 4. Reality Checks | `templates/skills/*-audit/` (security, dba, performance, api-design, bot, i18n, tech-debt, simplicity, complexity, version, test, mutation, spellcheck, business-risk) |
| Control Maintenance | `templates/skills/memory-hygiene/`, `doc-hygiene/`, `backlog-hygiene/` |
| Engineering Governance | `docs/solutions/human-audit-bridge.md`, release decision |

`templates/skills/` — готовые инструкции для аудитов. Запускаются по расписанию или когда меняется код в зоне ответственности.

## Быстрый старт

```bash
# 1. Клонируй
git clone https://github.com/svetkis/agentic-acceptance-dotnet.git

# 2. Запусти DemoProject
cd examples/DemoProject
dotnet build
dotnet run --project tests/DemoProject.Tests

# 3. Оцени свой проект
# Открой .agents/skills/acceptance-bootstrap/SKILL.md — прогони чеклист,
# разберись что уже есть и что внедрить в первую очередь.

# 4. Адаптируй скиллы под свой стек
# См. templates/skills/ADAPTATION.md — вычеркни неприменимые проверки.

# 5. Скопируй ТОЛЬКО выбранные артефакты (не всё подряд)
# Путь: inventory → risk profile → selected controls → validation.
# Конституция (Control Foundation):
cp rules/AGENTS_TEMPLATE.md /your/project/AGENTS.md   # затем отредактируй под стек
# По одному контролю на спринт, например pre-commit review:
cp -r templates/skills/code-review /your/project/.kimi/skills/
# Тестовые паттерны — бери по одному, когда он покрывает реальный риск
# (tests/patterns/*.cs — шаблоны для чтения, а не пакет для массового копирования):
# cp tests/patterns/ArchitectureRules.cs /your/project/tests/
```

## Структура

```
.
├── AGENTS.md                     # Инструкции для AI-агентов
├── rules/
│   ├── AGENTS_TEMPLATE.md        # Базовая конституция для агентов (универсальная)
│   ├── AGENTS_TEMPLATE.efcore.md # Add-on: EF Core-специфичные правила
│   ├── AGENTS_TEMPLATE.dapper.md # Add-on: Dapper / Raw SQL-специфичные правила
│   └── CONVENTIONS.md            # Коммиты, воркфлоу, тесты
├── templates/skills/                        # 28 скиллов-ролей (полный каталог: docs/README.md)
├── docs/
│   ├── traps/                     # Ловушки агента
│   └── solutions/
│       ├── architecture-tests.md  # Гайд по arch-тестам
│       └── ai-patterns.md         # 10 паттернов AI-driven разработки
├── tests/
│   ├── patterns/                  # Шаблоны тестов (Ratchet, NetArchTest, NBomber)
│   └── conventions/               # Именование, TUnit гайд
├── ci/                            # CI/CD guardrails
└── examples/
    ├── DemoProject/               # Рабочий пример на .NET 10 (Clean Architecture + Traps)
    └── DemoProject.MinimalApi/    # Single-project MVP (Minimal API, no layers)
```

## DemoProject

`examples/DemoProject/` — рабочий пример на .NET 10 со всеми паттернами:

- Clean Architecture (Domain → Application → Infrastructure)
- NetArchTest: проверка зависимостей между слоями
- Ratchet-тесты: контроль публичных типов и количества тестов
- Snapshot-тесты: контракты JSON-сериализации
- NBomber: нагрузочные тесты (read + write mix)
- TUnit: запуск через `dotnet run --project`

```bash
cd examples/DemoProject
dotnet build
dotnet run --project tests/DemoProject.Tests
```

## DemoProject.Traps

`examples/DemoProject/traps-src/DemoProject.Traps/` (см. [TRAPS.md](examples/DemoProject/TRAPS.md)) — специально сломанный код для демонстрации guardrails в действии. Каждый тест здесь падает, показывая, что ловит архитектурный тест, если агент нарушает правила.

```bash
cd examples/DemoProject
dotnet run --project tests/DemoProject.Traps.Tests
```

**Что ломается:**
- `MutableState` — мутабельное состояние в Domain
- `DomainLeakingToInfra` — Domain зависит от `System.Net.Http`
- `PaymentService` — прямая зависимость между Features (Orders → Payments)
- `Modules/` — циклические зависимости между модулями (ArchUnitNET)
- `RawGuidEntity` — голый `Guid` вместо strongly typed ID

См. также [`examples/DemoProject/TRAPS.md`](examples/DemoProject/TRAPS.md).

## DemoProject.MinimalApi

`examples/DemoProject.MinimalApi/` — вариант для **Minimal API без Clean Architecture**. Показывает, как адаптировать guardrails, когда нет слоёв Domain / Application / Infrastructure.

```bash
cd examples/DemoProject.MinimalApi
dotnet build
dotnet run --project tests/DemoProject.MinimalApi.Tests
```

**Что внутри:**
- Naming conventions, banned APIs (`DateTime.Now`)
- `CancellationToken` guard
- Ratchet-тесты на публичные типы
- Duplication guard для бизнес-логики

См. также [`examples/DemoProject.MinimalApi/README.md`](examples/DemoProject.MinimalApi/README.md).

## Навигация

Потерялись? Полная карта знаний — все артефакты по ролям — живёт в
[docs/README.md](docs/README.md). Самые частые запросы:

| Что нужно | Куда идти |
|-----------|-----------|
| Незнакомый термин | [GLOSSARY.md](GLOSSARY.md) |
| Правила для агента (базовые) | `rules/AGENTS_TEMPLATE.md` (+ аддоны [EF Core](rules/AGENTS_TEMPLATE.efcore.md) / [Dapper](rules/AGENTS_TEMPLATE.dapper.md)) |
| Паттерны тестов | `tests/patterns/` |
| Ловушки агента | `docs/traps/` |
| Онбординг проекта | [docs/ONBOARDING.md](docs/ONBOARDING.md) |
| Рабочий пример (Clean Architecture) | `examples/DemoProject/` |
| Рабочий пример (Single-project MVP) | `examples/DemoProject.MinimalApi/` |
| Failing demo (guardrails) | `examples/DemoProject/TRAPS.md` |
| Настройка AI-агентов (Kimi, Claude, Cursor, Codex) | `docs/agents/` |

## Автор

**Светлана Мелешкина** — автор методологии Agentic Acceptance, докладчик.

- 💬 Telegram-канал: [@kot_review](https://t.me/kot_review)
- ✉️ Telegram: [@svetkis](https://t.me/svetkis)

## Контрибуция

См. [CONTRIBUTING.md](CONTRIBUTING.md). Принимаем новые скиллы, паттерны тестов, ловушки и интеграции с агентами.

## Лицензия

[MIT](LICENSE)
