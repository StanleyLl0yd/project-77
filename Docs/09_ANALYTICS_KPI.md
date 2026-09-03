# Project 77 — Analytics & KPI

## 1. Principle

Ни один крупный системный спор после soft launch не должен решаться только ощущением команды, если его можно измерить.

## 2. Event taxonomy

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

### Prototype/vertical slice

- ≥80% понимают basic gesture без повторного текстового объяснения;
- большинство тестеров добровольно хотят пройти ещё 5 уровней;
- первые 10 уровней не имеют rage-quit spikes.

### Soft launch

- tutorial completion ≥80%;
- D1 target ≥35%;
- D7 target ≥12–15%;
- D30 target ≥5%;
- crash-free sessions ≥99.5%;
- payment success rate контролируется отдельно по store provider;
- ad exposure не коррелирует с заметным retention collapse.

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


## Product gate dashboard

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
