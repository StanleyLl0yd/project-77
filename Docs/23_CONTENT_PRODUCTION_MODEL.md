# Project 77 — Content Production Model

## 1. Principle

LiveOps cadence определяется реальной производительностью команды, а не маркетинговым желанием «новый сезон каждый месяц».

## 2. Content units

Для Project 77 считать отдельно:

- puzzle level;
- puzzle mechanic/modifier;
- island prop/decor;
- building variant;
- 77 cosmetic;
- character outfit;
- creature/pet;
- narrative scene/dialogue beat;
- environment vignette/anomaly;
- event configuration;
- seasonal reward track;
- localization string batch;
- QA verification unit.

## 3. Velocity measurement

Для каждой content unit измерять:

- concept time;
- implementation time;
- review/rework;
- QA;
- localization;
- integration;
- defect rate;
- lead time end-to-end.

Не использовать первый успешный asset как норму. Минимум несколько representative samples.

## 4. Capacity formula

Практическая season capacity:

`team throughput - maintenance - bugfix - platform/compliance work - contingency`

Нельзя планировать 100% времени команды на новый контент.

Начальный planning reserve: оставить существенный буфер до измерения реального throughput; конкретный процент фиксируется после первых production sprints.

## 5. Season template gate

Season Pass/LiveOps cadence LOCKED только после того, как команда дважды воспроизвела production template без heroics/crunch.

До этого 35–42 дней остаётся продуктовой гипотезой, а не обязательством.

## 6. Modular reuse

Чтобы не попасть в content treadmill:

- новые сезоны должны переиспользовать core systems;
- планета — пакет биома/контента, а не новая игра;
- puzzle modifiers комбинируются;
- события data-driven;
- environment props строятся из модульных наборов;
- narrative delivery использует повторяемые presentation formats.

Reuse не должен быть визуально ленивым: ценность достигается новой комбинацией, темой и контекстом.

## 7. Content backlog states

Каждый объект проходит:

`IDEA -> APPROVED -> SPEC -> IN PRODUCTION -> REVIEW -> QA -> READY -> LIVE -> RETIRED/RETURNABLE`

READY означает: asset, data, localization, analytics hooks и rollback behavior готовы.

## 8. QA bottleneck

Нельзя считать level/season готовым, если его физически не успевает проверить QA. QA throughput входит в production capacity так же, как художники и level designers.

## 9. Localization multiplier

Каждый новый язык увеличивает стоимость не только перевода, но и:

- UI fitting;
- screenshots/store text;
- narrative QA;
- push/inbox text;
- legal/commerce copy;
- support.

Поэтому язык добавляется как продуктовый рынок, а не как «ещё один CSV».

## 10. Content debt

Ежемесячно учитывать:

- obsolete content;
- assets needing optimization;
- stale events;
- old localization;
- broken references;
- analytics gaps;
- return/re-run compatibility.

Live game не должен становиться кладбищем старых сезонов.
