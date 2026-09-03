# Project 77 — Production Model & Team

## 1. Lean production principle

Project 77 должен быть спроектирован так, чтобы первая коммерчески проверяемая версия была достижима маленькой командой. Масштаб вселенной — контентная перспектива, а не стартовый scope.

## 2. Minimum serious team shape

Роли могут совмещаться.

### Product / Game Design

- product owner/game director;
- game/level designer;
- economy/liveops responsibility.

### Engineering

- client/gameplay engineer;
- backend/integration engineer — может быть part-time/managed-services-heavy на раннем этапе.

### Art

- 2D/3D generalist/environment artist;
- UI/UX artist/designer;
- animation/VFX — part-time или outsource на ранней стадии.

### Narrative/Content

- narrative design/writing — совмещаемая роль до масштабирования.

### QA/Analytics

- QA сначала distributed + external testing;
- analytics ownership должен существовать с vertical slice, даже без отдельного analyst headcount.

## 3. First hires if project shows traction

Порядок определяется bottleneck:

1. level/content designer;
2. liveops/economy specialist;
3. additional artist;
4. QA owner;
5. backend/live services engineer;
6. community/support;
7. UA/marketing specialist.

## 4. Production pipeline

Идея проходит состояния:

`Backlog -> Design -> Prototype -> Review -> Production -> QA -> Config/Staging -> Release -> Telemetry Review -> Iterate/Retire`

Нельзя отправлять feature прямо из идеи в production.

## 5. Level pipeline

1. mechanic/rule goal;
2. level draft;
3. solver validation;
4. designer playtest;
5. difficulty tag;
6. QA;
7. production telemetry;
8. rebalance/retire.

## 6. Narrative pipeline

1. story beat objective;
2. dependency/unlock;
3. environment clue;
4. dialogue draft;
5. spoiler tier;
6. localization-safe review;
7. implementation;
8. pacing telemetry.

## 7. Art pipeline

1. brief;
2. silhouette/readability;
3. concept;
4. in-game scale test;
5. final asset;
6. optimization;
7. prefab/data binding;
8. QA on target devices.

## 8. Definition of Done — feature

Feature не Done, пока:

- работает;
- имеет analytics;
- локализуема;
- не ломает save;
- имеет error/fallback behavior;
- протестирована на target devices;
- имеет remote kill switch, если относится к LiveOps/monetization;
- документация обновлена.

## 9. Weekly product review

Обсуждаем:

- что реально улучшило player experience;
- текущий главный риск;
- level/content throughput;
- retention funnel;
- top support/review complaints;
- next experiment;
- scope items, которые можно удалить.

## 10. Monthly roadmap review

Вопросы:

- подтверждена ли текущая гипотеза;
- не производим ли мы content для системы, которую игроки не ценят;
- где bottleneck — art, levels, backend, economy, QA;
- что можно упростить;
- не стал ли monetization pressure выше, чем value delivery.

## 11. Repository/docs convention

Рекомендуемая структура репозитория:

```text
/Assets        # Unity game project assets/code
/Docs          # Project documentation; this package lives here
/Tools         # Content/build/validation tooling
/Tests         # automated and playtest support
/Build         # build scripts/configuration; artifacts themselves are not committed
/Backend       # added only when backend scope becomes necessary
/Config        # data-driven content/economy/liveops schemas and safe defaults
```

Production baseline: Unity 6.3 LTS / C# / URP / Android-first. Core game modules must remain platform-neutral; store, ads, analytics, cloud save, auth and notifications sit behind interfaces/adapters.

`docs/13_DECISION_LOG.md` или его эквивалент должен оставаться source of truth для утверждённых high-level решений.

## 12. AI-assisted development

ChatGPT/Codex можно использовать для:

- boilerplate;
- tests;
- data validators;
- level tooling;
- config schemas;
- documentation updates;
- analytics event review;
- code audit/refactoring.

Но AI не должен автоматически менять economy, purchase handling, save migration или live config без review/tests.
