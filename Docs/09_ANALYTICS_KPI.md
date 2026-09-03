# Project 77 — Analytics & KPI

## 1. Principle

Ни один крупный системный спор не должен решаться только ощущением команды, если его можно измерить. Для Prototype 0.1 canonical event schema находится в `30_PROTOTYPE_ANALYTICS_CONTRACT.md`; этот документ описывает более широкий KPI/analytics контур продукта.

## 2. Product event taxonomy

Ниже — long-term taxonomy direction. Она **не означает**, что все эти события/SDK должны быть реализованы в Prototype 0.1. Для P0/P1 реализуется только минимальный contract из `30_PROTOTYPE_ANALYTICS_CONTRACT.md`.

### Lifecycle

- app_install
- first_open
- session_start
- session_end
- app_update

### Onboarding

- tutorial_step_start
- tutorial_step_complete
- tutorial_skip
- tutorial_fail

### Puzzle

- level_start
- level_complete
- level_fail
- level_quit
- booster_use
- hint_use
- retry

Properties:

- level_id;
- attempt;
- duration;
- moves/path actions;
- modifier set;
- booster source;
- story context.

### Meta

- zone_unlock
- building_restore
- building_upgrade
- decoration_place
- collectible_get
- collection_complete
- expedition_start
- expedition_complete

### Narrative

- chapter_start
- story_beat
- mystery_clue_found
- reveal_reached

### Economy

- currency_earn
- currency_spend
- item_earn
- item_spend

Обязательно source/sink.

### Monetization

- store_open
- offer_view
- purchase_start
- purchase_success
- purchase_fail
- subscription_start/cancel/renew where available
- rewarded_ad_offer
- rewarded_ad_start
- rewarded_ad_complete

### LiveOps

- event_join
- event_progress
- milestone_claim
- season_level
- season_pass_purchase

## 3. Funnel dashboards

### FTUE

Install → tutorial start → first puzzle → first restoration → 77 acquired → second session.

### Story

Island start → lighthouse → cave → underground → vessel reveal → launch.

### Monetization

Store view → product view → purchase start → success.

### Ads

Offer shown → accepted → completed → reward claimed.

## 4. Retention

Track:

- D1;
- D3;
- D7;
- D14;
- D30;
- rolling retention;
- return frequency.

Segment by:

- acquisition source;
- country/store;
- device tier;
- puzzle difficulty cohort;
- payer/non-payer;
- ad exposure cohort.

## 5. Internal greenlight targets

Это рабочие цели, а не обещания и не «средние по рынку».

### Prototype / Gate P0 + P1

Authoritative initial targets are defined in `18_PRODUCT_GATES.md` and measured using `29_PROTOTYPE_PLAYTEST_PROTOCOL.md` + `30_PROTOTYPE_ANALYTICS_CONTRACT.md`. Current P0/P1 anchors:

- P0 tutorial/core comprehension target: **>= 85%**;
- P0 voluntary next-puzzle target: **>= 70%**;
- P1 voluntary continuation after first meaningful island reward: **>= 50%**;
- instrumentation/help/exclusions must be recorded so an assisted action is not counted as unaided comprehension or voluntary continuation.

These are prototype decision aids, not market-retention claims.

### Soft launch / Gate P4

Current initial decision targets from `13_DECISION_LOG.md` / `18_PRODUCT_GATES.md`:

- D1 **>= 30%**;
- D7 **8–12%+**;
- D30 **4–7%+**;
- crash-free sessions/users **> 99.5%**;
- ANR-free **> 99.5%**;
- payment success rate контролируется отдельно по store provider;
- ad exposure не коррелирует с заметным retention collapse.

The older 35% / 12–15% / 5% retention set is retired as an authoritative target as of documentation v0.5; do not use it as a second gate.

## 6. Puzzle health

Для каждого level:

- first-attempt win rate;
- total win rate;
- median attempts;
- median duration;
- quit rate;
- booster usage;
- post-level session termination.

Auto-flag level, если резко отличается от target band.

## 7. Economy health

Track:

- currency balance distribution;
- daily faucets/sinks;
- hoarding;
- resource shortage points;
- premium currency earned vs bought;
- time-to-goal.

## 8. Monetization health

- payer conversion;
- ARPDAU;
- ARPPU;
- LTV by cohort;
- first purchase time;
- subscription retention;
- Season Pass attach rate;
- rewarded ad opt-in;
- revenue mix IAP/ads.

Не оптимизировать одну метрику в изоляции.

## 9. Mystery effectiveness

Уникальная метрика Project 77:

- % игроков, вернувшихся в течение 24h после major clue;
- progression speed between clues;
- drop-off before reveal;
- optional clue engagement.

Сюжет должен быть измерим как retention layer.

## 10. North Star proxy

Weekly Engaged Explorers:

уникальные игроки за 7 дней, выполнившие минимум:

- 1 puzzle activity;
- 1 island/collection progression action;
- 1 story/expedition/event action.

Это лучше отражает здоровье всей игры, чем DAU сам по себе.

## 11. Product gate dashboard

Отдельный dashboard должен напрямую поддерживать `18_PRODUCT_GATES.md`:

- tutorial completion;
- next-puzzle voluntary continuation;
- first island reward -> next action;
- FTUE exits by step;
- D1/D7/D30 cohorts;
- crash/ANR;
- level attempts/fail/win/quit;
- economy balance distribution;
- ad opt-in;
- purchase funnel;
- config version and experiment cohort.

Metrics без decision owner/action не считаются достаточной аналитикой.
