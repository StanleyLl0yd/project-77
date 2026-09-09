# Project 77 — Project Documentation

Версия документации: **0.6 Gate P1 Execution Baseline**.

Статус: **pre-production завершён; активная фаза — Prototype 0.1**. Implementation baseline и operational guardrails утверждены.

Project 77 — мобильная free-to-play adventure/puzzle-игра с долгой метапрогрессией. Игрок начинает с восстановления заброшенного острова, постепенно раскрывает тайну подземного исследовательского комплекса, находит древний корабль, выходит в космос, исследует другие планеты и в перспективе открывает сеть миров.

Ключевой принцип проекта:

> Каждый раз, когда игрок думает, что понял масштаб игры, мы показываем, что мир гораздо больше.

## Что уже зафиксировано

- Внутреннее codename: **Project 77**; публичное коммерческое название ещё не cleared.
- Ключевой спутник/маскот: маленький робот **77**.
- Начало игры намеренно выглядит как камерное приключение про заброшенный остров.
- Космический масштаб не раскрывается в первых рекламных материалах и описании игры.
- Вся основная история должна быть доступна бесплатно.
- Монетизация строится вокруг косметики, Season Pass, подписки, удобства, ускорений и добровольной рекламы, а не вокруг обязательной оплаты сюжета.
- Остров остаётся домом игрока навсегда, даже после открытия космоса.
- Основной игровой цикл должен быть простым, управляться одним пальцем и занимать примерно 30–90 секунд.
- Конкретная core-puzzle механика пока считается проверяемой гипотезой и должна пройти прототипирование.
- Production stack: **Unity 6.3 LTS + C# + URP**.
- Первая целевая платформа: **Android**, архитектура сразу cross-platform.
- Первый milestone: **Project 77 Prototype 0.1** — P0 сравнивает три greybox-варианта (minimum 10 validated levels each), затем P1 интегрирует победителя в 10–20-level путь → ресурсы → восстановление генератора → area unlock → обнаружение 77.
- Красивый production-art, магазин и большой backend не делаются до доказательства core/meta loop.
- Целевая product-аудитория: подростки и взрослые (13+ direction), не child-directed.
- Launch social: visits/likes/predefined reactions; без free-form chat/UGC.
- Saves, Remote Config, server authority, incident recovery и performance budgets проектируются до soft launch.
- Pre-production считается завершённым; дальнейшие неизвестные закрываются прототипами и данными, а не дополнительным speculative design.
- Активный implementation contract: `28_PROTOTYPE_01_SPEC.md`.
- Standardized P0/P1 playtests: `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.
- Prototype telemetry schema: `30_PROTOTYPE_ANALYTICS_CONTRACT.md`.
- Minimal Unity engineering rules/backlog: `31_ENGINEERING_CONVENTIONS.md` + `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`.

## Структура пакета

1. `00_PRODUCT_VISION.md` — продуктовая идея, аудитория, позиционирование и принципы.
2. `01_GAME_DESIGN_DOCUMENT.md` — основной GDD.
3. `02_CORE_GAMEPLAY.md` — core loop, puzzle-кандидаты, прогрессия сложности.
4. `03_NARRATIVE_BIBLE.md` — сюжет, мир, персонажи, тайны и правила раскрытия лора.
5. `04_WORLD_CONTENT_SYSTEM.md` — остров, базы, планеты, коллекции и контентная модель.
6. `05_ECONOMY_MONETIZATION.md` — экономика, валюты, IAP, подписка, Season Pass, реклама.
7. `06_LIVEOPS_SOCIAL.md` — события, сезоны, социальные функции и community-механики.
8. `07_ROADMAP.md` — этапы от прототипа до live product.
9. `08_TECH_ARCHITECTURE.md` — предлагаемый технический контур.
10. `09_ANALYTICS_KPI.md` — события аналитики, метрики и критерии принятия решений.
11. `10_UX_ONBOARDING.md` — первые 60 секунд, 10 минут, день, неделя и месяц.
12. `11_BACKLOG_IDEAS.md` — банк идей, не входящих в обязательный scope.
13. `12_RISKS_GUARDRAILS.md` — риски, анти-паттерны и продуктовые ограничения.
14. `13_DECISION_LOG.md` — что зафиксировано, что открыто, что запрещено менять без отдельного решения.
15. `14_RELEASE_AND_LIVE_CHECKLIST.md` — чек-лист подготовки релиза и работы после релиза.
16. `15_GO_TO_MARKET.md` — позиционирование, acquisition, store creatives и органический рост.
17. `16_PRODUCTION_TEAM.md` — роли, production pipeline и Definition of Done.
18. `17_ART_AUDIO_DIRECTION.md` — визуальный язык, 77, планеты и звук.
19. `PROJECT_CONTEXT_FOR_AI.md` — компактный контекст для ChatGPT/Codex/нового чата.
20. `18_PRODUCT_GATES.md` — CONTINUE/ITERATE/PIVOT/STOP gates и стартовые KPI.
21. `19_ACCOUNT_SAVE_SPEC.md` — guest/account/cloud save, migrations, conflicts, recovery.
22. `20_BACKEND_SECURITY_MODEL.md` — authority, offline, time, anti-abuse, purchase security.
23. `21_REMOTE_CONFIG_SPEC.md` — remotely controlled parameters, environments, rollback, kill switches.
24. `22_ECONOMY_SIMULATION.md` — экономика 1/7/30/90/365 дней и player archetypes.
25. `23_CONTENT_PRODUCTION_MODEL.md` — content velocity, capacity, season production.
26. `24_COMPLIANCE_PRIVACY.md` — audience, privacy, UGC, billing, Android/store baseline.
27. `25_QA_DEVICE_PERFORMANCE.md` — device tiers, performance/stability budgets, test matrix.
28. `26_INCIDENT_ROLLBACK_PLAN.md` — incident severity, containment, rollback/recovery playbooks.
29. `27_IP_BRAND_NAMING.md` — codename status, commercial naming/IP clearance and asset provenance.
30. `28_PROTOTYPE_01_SPEC.md` — активный контракт Prototype 0.1: scope, DoD, out-of-scope и gate.
31. `29_PROTOTYPE_PLAYTEST_PROTOCOL.md` — operational rules, voluntary-continuation definition and batch-report template.
32. `30_PROTOTYPE_ANALYTICS_CONTRACT.md` — canonical P0/P1 event names, properties, types and schema version.
33. `31_ENGINEERING_CONVENTIONS.md` — minimal Unity/C# structure, deterministic-domain/config/test rules.
34. `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md` — ordered implementation tasks and Definition of Done.
35. `33_P0_GATE_DECISION.md` — basis/limitations owner-authorized Gate P0 CONTINUE.
36. `34_P1_EXTERNAL_PLAYTEST_PLAN.md` — preregistered Gate P1 hypothesis, environment and decision rule.
37. `CHANGELOG.md` — история версий проектной документации.

## Текущая рабочая фаза

**Prototype 0.1.** На этом этапе проект больше не расширяет дизайн «на бумаге» без необходимости. Основная задача — проверить core + meta loop на внешних playtests.

Последовательность ближайшей работы:

`bootstrap -> A/B/C (10 validated levels each) -> P0 playtest -> select winner -> 10–20 integrated levels -> reward -> repair generator -> unlock area -> discover 77 -> P1 playtest -> gate decision`.

Gate P0 has CONTINUE for implementation purposes; Energy Routing is the selected P1 core. The current active work is the frozen external P1 batch and Gate P1 decision.

До успешного gate production-art, большой backend, store, Season Pass, subscription, планеты и массовое производство контента не являются допустимым приоритетом.

## Как пользоваться документацией

Для coding/AI work сначала применяется root `../AGENTS.md`. `13_DECISION_LOG.md` остаётся источником истины по product/high-level decision status; `28_PROTOTYPE_01_SPEC.md` — active implementation contract. `11_BACKLOG_IDEAS.md` — только неутверждённые идеи и **не committed scope**. Новая идея становится реализацией только через hypothesis/test/decision flow.

GDD не должен превращаться в архив всех идей. В нём хранится только текущая версия игры.

## Продуктовая гипотеза

Project 77 должен сочетать низкий порог входа casual/hybrid-casual игры с эмоциональной метапрогрессией, тайной, коллекционированием и LiveOps. Цель — широкая бесплатная аудитория и несколько независимых способов монетизации небольшими платежами.

Актуальный рыночный контекст 2026 поддерживает эту модель: hybrid-casual продолжает расти, а mature mobile market всё сильнее зависит от retention, LiveOps и гибридной монетизации IAP + ads. Источники и конкретные выводы зафиксированы в `00_PRODUCT_VISION.md` и `05_ECONOMY_MONETIZATION.md`.
