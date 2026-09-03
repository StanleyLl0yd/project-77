# Project 77 — Remote Config Specification

## 1. Principle

Числовые параметры экономики, difficulty, ads и LiveOps не должны требовать нового APK/AAB для обычной балансировки.

Remote Config — first-class capability с default values внутри build и безопасным fallback.

## 2. Config domains

### Gameplay

```text
level.moves.*
level.timer.*
level.energyCost.*
booster.power.*
booster.freeCount.*
retry.cost.*
```

### Economy

```text
reward.scrap.*
reward.energy.*
reward.research.*
build.cost.*
build.duration.*
expedition.cost.*
expedition.duration.*
dailyReward.*
```

### Monetization

```text
store.offer.enabled.*
store.offer.priceTier.*
store.offer.contents.*
season.passXp.*
subscription.bonus.*
ad.rewardMultiplier.*
ad.frequencyCap.*
```

Цены, которые фактически определяет store SKU, не подменяются клиентским config; config управляет eligibility/presentation/content, а provider остаётся источником фактической цены.

### LiveOps

```text
event.enabled.*
event.start.*
event.end.*
event.rewardTable.*
event.levelPack.*
community.goal.*
```

### UX / rollout

```text
feature.enabled.*
feature.rolloutPercent.*
ftue.variant.*
push.enabled.*
```

## 3. Kill switches

Обязательные emergency flags:

- purchases enabled;
- rewarded ads enabled;
- specific offer disabled;
- specific event disabled;
- social visits disabled;
- voting disabled;
- cloud write disabled / read-only recovery mode;
- new content bundle disabled;
- risky feature disabled.

Kill switch должен работать без новой клиентской сборки, если клиент уже содержит поддержку флага.

## 4. Environments

Минимум:

- Development;
- Staging;
- Production.

Config не копируется вручную между средами без review/versioning.

## 5. Versioning

Каждая published config set имеет:

- config version;
- author;
- timestamp;
- change note;
- rollback target;
- optional experiment ID.

Клиент отправляет `configVersion` в analytics/error reports.

## 6. Safe defaults

Если Remote Config недоступен:

- игра стартует на встроенном known-good baseline;
- магазин не показывает неизвестные offers;
- expired/unknown events не стартуют самопроизвольно;
- critical kill-switch-sensitive feature выбирает безопасное состояние.

## 7. Segmentation & experiments

Сегменты допустимы по продуктовым признакам, если это соответствует privacy/compliance:

- install cohort;
- progression;
- payer/non-payer;
- returning player;
- platform/store;
- locale/region;
- experiment cohort.

Не создавать десятки непрозрачных overlapping overrides без ownership.

## 8. Governance

Изменение config — production change.

Для high-impact settings нужны:

- documented owner;
- expected effect;
- guardrail metric;
- rollback plan;
- expiry/review date for temporary overrides.

Reference: Unity Remote Config supports Game Overrides/settings and recommends planning settings early: https://docs.unity.com/en-us/remote-config/game-overrides-and-settings
