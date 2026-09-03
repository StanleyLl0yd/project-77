# Project 77 — Roadmap

## Roadmap philosophy

Не строить сразу «игру на пять лет». Каждый этап должен доказать следующую гипотезу до расширения scope.

Roadmap разбит по продуктовым воротам, а не только по календарю.

---

# Phase 0 — Product Lock — COMPLETE

## Цель

Зафиксировать продукт, не тратя производство на спорные детали.

**Статус на v0.5: завершено. Проект находится в Prototype Phase.**

## Deliverables

- Product Vision.
- GDD v0.1.
- Narrative Bible v0.1.
- Monetization principles.
- Core mechanic shortlist.
- Art direction references.
- Locked implementation baseline: Unity 6.3 LTS / C# / URP / Android-first.
- Repository/bootstrap plan.
- Analytics taxonomy draft.

## Exit gate

Команда одинаково отвечает на вопросы:

- что игрок делает каждую минуту;
- почему возвращается завтра;
- почему играет месяц;
- за что платит;
- что делает Project 77 отличимым.

---

# Phase 1 — Core Prototype — ACTIVE

Активная спецификация: `28_PROTOTYPE_01_SPEC.md`. Исполняемый порядок задач: `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`. Playtests и telemetry: `29_PROTOTYPE_PLAYTEST_PROTOCOL.md` + `30_PROTOTYPE_ANALYTICS_CONTRACT.md`.

Ориентир: 4–6 недель для маленькой команды, но gate важнее срока.

## Stage 1A — Repository / Bootstrap

Repository `project-77` создан. После Unity bootstrap version-controlled структура включает `Assets/`, `Packages/`, `ProjectSettings/`, `Docs/` и project-owned `Assets/Project77/...`. Build outputs остаются ignored и не требуют tracked `/Build` directory.

На старте:

- Unity 6.3 LTS project;
- C#;
- URP;
- Android build target;
- Git LFS **не включать автоматически**; добавлять только для конкретных крупных binary asset types после появления реальной необходимости;
- базовый CI build;
- документация Project 77 хранится в `/Docs`;
- никакой store/backend зависимости в core gameplay.

## Stage 1B — Core mechanic prototypes

Build:

- Prototype A: Energy Routing.
- Prototype B: Path / Expedition Routing.
- Prototype C: Flow / Network Restoration.
- минимум **10 validated levels на вариант** для P0; расширять только если этого требует конкретная гипотеза.
- telemetry по `30_PROTOTYPE_ANALYTICS_CONTRACT.md`.
- placeholder UI/art only.
- no store, no subscription, no production backend.

Tests:

- понимание без длинного текста;
- one-more-level desire;
- retry frustration;
- управление на разных размерах экранов;
- возможность контентного расширения;
- связь puzzle с фантазией ремонта/исследования мира;
- external sessions по `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.

## Stage 1C — Meta prototype / Project 77 Prototype 0.1

После выбора лучшего puzzle делается дешёвая мета-обвязка:

`puzzle -> victory -> resources -> visible island action -> unlock/discovery -> next puzzle`.

Минимальная карта острова может быть условной и состоять из простых tiles/placeholders. Проверяем не графику, а желание продолжить прогресс.

### Prototype 0.1 milestone

Игрок должен иметь возможность:

1. Запустить игру.
2. Пройти 10–20 коротких головоломок.
3. Получить ресурсы за победы.
4. Потратить их на восстановление генератора.
5. Открыть новый участок острова.
6. Обнаружить повреждённого робота 77.

## Exit gate

- выбран один core mechanic;
- micro loop приятен сам по себе даже на placeholder-графике;
- meta loop `Puzzle -> reward -> discovery -> puzzle` создаёт желание продолжить;
- подтверждена техническая сборка Android;
- если core/meta loop не работает, **не переходить к красивой vertical slice**;
- никакой production-scope expansion до решения CONTINUE по P0/P1;
- следующий пакет design-документации обновляется прежде всего по данным playtests.

---

# Phase 2 — Vertical Slice

Ориентир: 8–12 недель.

## Scope

Игровой отрезок 20–40 минут, показывающий финальное качество:

- прибытие на остров;
- 77;
- 15–25 polished puzzle levels;
- восстановление причала/лагеря/маяка;
- 1 collection;
- 1 anomaly;
- первая mystery clue;
- platform-services sandbox (store/rewarded ads) только после подтверждённого core loop;
- cloud save prototype;
- analytics.

## Art

Утвердить:

- camera/orientation;
- character scale;
- 77 final silhouette;
- island rendering;
- UI system;
- VFX language ancient tech.

## Exit gate

Новые игроки понимают loop и хотят узнать, что за тайна на острове.

---

# Phase 3 — MVP / Closed Alpha

Ориентир: 10–14 недель после vertical slice.

## Content target

- 100–150 puzzle levels;
- 3–4 island zones;
- 3–5 named characters;
- полноценный 77 progression lite;
- 3–5 collections;
- first underground reveal teaser;
- daily/weekly tasks;
- basic expeditions;
- store;
- starter pack;
- rewarded ads;
- remote config;
- crash/error telemetry;
- account/cloud save.

## Monetization

Реальные sandbox/store test products, но без агрессивной настройки.

## Exit gate

- стабильность;
- tutorial completion;
- retention signal;
- уровень production throughput понятен;
- economy не разваливается за первые дни.

---

# Phase 4 — Soft Launch

Ориентир: 8–12 недель итераций, возможно несколькими волнами.

## Goal

Проверить не «нравится ли игра», а unit economics и retention foundation.

## Add

- 200–300 levels;
- 5–6 island zones;
- first Season Pass;
- Explorer Club experiment;
- 2–3 event types;
- social visits lite;
- first major mystery arc;
- improved economy;
- A/B framework.

## Metrics gates

Внутренние ориентиры, не универсальные нормы:

- tutorial completion ≥ 80%;
- D1: initial decision target **>= 30%**;
- D7: initial decision target **8–12%+**;
- D30: initial decision target **4–7%+**;
- crash-free sessions/users **> 99.5%**;
- ANR-free **> 99.5%**;
- meaningful rewarded-ad opt-in without retention damage;
- first purchase conversion показывает жизнеспособность IAP;
- organic reviews не сигнализируют «слишком много рекламы/paywall».

Эти значения синхронизированы с `13_DECISION_LOG.md` / Gate P4 в `18_PRODUCT_GATES.md`. Более старый набор 35% / 12–15% / 5% retired в v0.5 и не является отдельным gate.

Если D1/D7 слабы, нельзя лечить проблему новыми IAP.

---

# Phase 5 — Launch Candidate

## Content

- цельная Island Act 1–3;
- сильный reveal подземного комплекса;
- 300+ validated levels или достаточный live pipeline;
- Season 1;
- social showcase;
- robust save/recovery;
- store integrations by distribution channel;
- localization pipeline;
- customer support/admin tools;
- privacy/consent/compliance.

## Marketing promise

Продаём тайну острова, а не галактику.

## Exit gate

Retention, stability и content production позволяют увеличивать acquisition без сжигания бюджета.

---

# Phase 6 — The Ship Update

Первое большое post-launch расширение или поздняя launch arc — зависит от темпа игроков.

## Features

- полное раскрытие vessel chamber;
- ship restoration meta;
- ship cosmetic slots;
- новая expedition layer;
- narrative reveal о 77;
- launch sequence.

Корабль должен ощущаться как огромный payoff, а не новая кнопка в меню.

---

# Phase 7 — First Planet

## Goal

Доказать, что космос расширяет игру, а не заменяет остров.

## Package

- один полноценный alien biome;
- local mystery;
- unique collection;
- planet trophy returning to island;
- 20–40 levels/variants;
- repeatable expeditions.

---

# Phase 8 — Live Universe

После доказанного retention/LTV:

- 2–4 planet archetypes;
- network map;
- global mysteries;
- richer social systems;
- community seasons;
- long-term character arcs;
- creator/community showcase tooling.

---

# Production tracks

Параллельные workstreams:

1. Core/Level Design.
2. Meta/Island.
3. Narrative.
4. Art/Animation/VFX.
5. Economy/Monetization.
6. Backend/LiveOps.
7. Analytics/Experimentation.
8. QA/Device Compatibility.
9. Community/Marketing.

# Scope cuts order

Если сроки/ресурсы давят, резать в таком порядке:

1. realtime social;
2. сложные leaderboards;
3. количество cosmetics на старт;
4. количество secondary characters;
5. количество event types;
6. extra planet teasers.

Не резать:

- качество core puzzle;
- 77;
- island emotional progression;
- first mystery hook;
- analytics;
- save integrity;
- performance.

## Production-readiness workstream (v0.3)

Этот workstream идёт параллельно разработке, а не после неё.

### Before external Prototype tests

- analytics event baseline;
- Prototype gate criteria;
- reproducible builds;
- localization-key architecture.

### Before Vertical Slice exit

- reference device matrix;
- performance budgets;
- save schema/version/migration baseline;
- Remote Config defaults/environment model;
- audience/social boundaries confirmed.

### Before Closed Alpha

- guest Player ID + cloud recovery path;
- economy simulation v1;
- server-time policy;
- purchase/entitlement architecture skeleton;
- content velocity measurement starts.

### Before Soft Launch

- server validation for valuable economy/purchases;
- incident/rollback runbook;
- kill switches;
- account deletion/privacy/support workflows;
- 16 KB compatibility validation;
- API/store compliance review;
- measured LiveOps capacity;
- commercial naming clearance before public brand investment.

Every phase exit references `18_PRODUCT_GATES.md`.
