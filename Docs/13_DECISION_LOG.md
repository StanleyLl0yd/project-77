# Project 77 — Decision Log

Этот документ — источник истины по ключевым решениям.

## LOCKED — утверждено

### D-001 — Project codename

Внутреннее рабочее название: **Project 77**. Это codename, а не подтверждённое коммерческое название; public title требует отдельного IP/trademark/store/domain clearance.

### D-002 — Companion

Ключевой маскот/спутник игрока — маленький робот **77**.

### D-003 — Macro progression

Заброшенный остров → тайна под островом → подземный исследовательский комплекс → древний корабль → космос → другие планеты → сеть миров.

### D-004 — Scale reveal

Космический масштаб намеренно не раскрывается игроку сразу.

### D-005 — Island permanence

Остров остаётся постоянным домом игрока после открытия планет.

### D-006 — Free main story

Основная сюжетная линия доступна бесплатно.

### D-007 — Monetization direction

Основные деньги: Season Pass, subscription, cosmetics, convenience IAP, rewarded ads.

### D-008 — No pay-to-win core

Не строить проект вокруг pay-to-win PvP.

### D-009 — Short core sessions

Core gameplay: one-finger, примерно 30–90 секунд.

### D-010 — Asynchronous social first

Realtime multiplayer не является launch requirement.

### D-011 — LiveOps is core product capability

События, сезоны и remote config проектируются архитектурно заранее.

### D-012 — Production engine and language

Production baseline: **Unity 6.3 LTS + C#**.

### D-013 — Rendering pipeline

Основной rendering pipeline: **URP**.

### D-014 — Platform strategy

Разработка ведётся **Android-first**, но core architecture остаётся cross-platform. Следующая приоритетная платформа после подтверждения продукта — iOS.

### D-015 — Android baseline

На старте разработки: min Android API 26, arm64-v8a обязательно, AAB — основной release artifact, APK — тестовый/direct-install artifact. На 02.09.2026 Google Play baseline: **targetSdk API 36+**; compileSdk — не ниже target и актуальный стабильный, поддерживаемый выбранным Unity/Android toolchain. Все native/JNI/NDK зависимости должны проходить 16 KB page-size validation на 64-bit устройствах. Требования магазинов перепроверяются перед каждым release.

### D-016 — Prototype-before-production rule

Нельзя начинать production красивого острова, большого количества контента, магазина или backend-инфраструктуры до доказательства core loop на дешёвом техническом прототипе.

### D-017 — Prototype 0.1 milestone

Первый реальный milestone: игрок запускает игру, проходит 10–20 коротких puzzle levels, получает ресурсы, восстанавливает генератор и обнаруживает робота 77.

### D-018 — Audience direction

Project 77 проектируется прежде всего для подростков и взрослых (13+ product positioning), а не как приложение специально для детей. Финальная store age/target declaration определяется по фактическому контенту и policy requirements.

### D-019 — Launch social boundaries

Launch social = asynchronous visits, likes, showcase, community goals и predefined reactions/messages. **Свободного чата, DM, комментариев, пользовательского текста/картинок и другого free-form UGC на launch нет.**

### D-020 — Product-gate process

Каждый крупный production stage проходит заранее описанный gate с решениями CONTINUE / ITERATE / PIVOT / STOP. Продолжение только из-за sunk cost запрещено.

### D-021 — Save/account baseline

Guest-first onboarding + stable Player ID + optional account binding + versioned local/cloud save + tested migrations. Purchased entitlements и premium economy не доверяются только локальному save.

### D-022 — Server authority

Premium currency, purchases, subscriptions, high-value event rewards, global/social results и long-lived economic timers должны быть server-authoritative или server-validated.

### D-023 — Offline baseline

Core gameplay и безопасная часть island interaction доступны offline; покупки, premium grants, global events, social и other server-authoritative actions требуют online validation/reconciliation.

### D-024 — Server time

Для сезонов, offers, long-lived build/expedition timers и других экономически значимых временных ограничений source of truth — server time; device clock не является доверенным источником.

### D-025 — Remote Config first-class

Difficulty, economy, rewards, event timing, offer eligibility, ad parameters и feature flags должны быть максимально data-driven/remote-configurable с known-good defaults, environments, versioning, rollback и kill switches.

### D-026 — No paid loot-box launch

Paid randomized loot boxes/gacha не входят в launch-модель. Любое будущее добавление требует отдельного design/compliance decision.

### D-027 — Subscription recurring value

Explorer Club обязан давать устойчивую повторяющуюся ценность весь период подписки; одноразовые rewards могут быть только дополнением.

### D-028 — Performance budgets

Проект имеет измеряемые budgets для FPS, startup, transitions, crash/ANR, memory, download size, battery/thermal и network behavior. «Работает нормально» без измерений не является Definition of Done.

### D-029 — Android 2026 compliance baseline

На 02.09.2026 Android/Google Play baseline: targetSdk **API 36+** для новых приложений/обновлений; 64-bit build и native dependencies должны поддерживать **16 KB memory page sizes**. Эти требования перепроверяются перед каждым release.

### D-030 — Incident readiness

До soft launch обязательны Remote Config rollback, feature/event kill switches, purchase recovery, save recovery path, emergency inbox grant и documented incident playbooks.

### D-031 — Content velocity rule

LiveOps/season cadence не фиксируется как production commitment, пока команда не измерит и не воспроизведёт реальную content throughput. План строится от capacity, включая QA/localization/maintenance.

### D-032 — Localization architecture

User-facing text не hardcode-ится. Localization keys/formatting/text expansion support закладываются с первого дня даже при одном launch-языке.

### D-033 — Accessibility baseline

Accessibility (readability, color independence, touch targets, text/subtitles, reduced motion where practical) учитывается в UI system до production масштабирования.

### D-034 — Pre-production completion

Начиная с документации v0.4, **pre-production считается завершённым**. Текущая активная стадия разработки — Project 77 Prototype 0.1 / Core Prototype.

### D-035 — Prototype 0.1 implementation contract

`28_PROTOTYPE_01_SPEC.md` является активным контрактом ближайшей разработки. Его minimum scope и Definition of Done имеют приоритет над расширением feature backlog до прохождения Prototype Gate.

### D-036 — Prototype scope freeze

До CONTINUE по Gate P0 + P1 не начинать production масштабирование острова, store/IAP/ads/subscription, Season Pass, production backend, другие планеты, ship layer, массовый narrative/art/content production. Разрешены только дешёвые mocks/stubs, необходимые для проверки гипотезы.

### D-037 — Data over speculation

После перехода в Prototype Phase вопросы, доступные дешёвому playtest/telemetry test, решаются данными. Дополнительная документация не считается заменой прототипа. Новая идея проходит путь `Backlog -> Hypothesis -> Test -> Decision -> Documentation`.

### D-038 — Prototype product DoD

Технически работающая сборка сама по себе не завершает Prototype 0.1. Ключевой продуктовый критерий: после понятного puzzle → reward → island change/discovery игрок **добровольно хочет продолжить**. Без этого результат = ITERATE/PIVOT, а не автоматический CONTINUE.

### D-039 — Gate P0 CONTINUE and selected core

08.09.2026 владелец проекта принял решение **CONTINUE** к P1 после успешного physical-device smoke и нескольких дополнительных внешних прогонов без выявленных блокирующих проблем.

Выбранный P1 core: **Energy Routing**.

Полный protocol-grade количественный P0 dataset не был сохранён, поэтому решение не трактуется как доказательство прохождения каждого initial numeric threshold или статистического превосходства A над B/C. Ограничение и basis решения зафиксированы в `33_P0_GATE_DECISION.md`. P1 обязан собирать полноценную telemetry/moderation evidence для следующего gate.

## PROVISIONAL — рабочая гипотеза

### P-002 — Art direction

Stylized 2.5D/isometric island + readable alien biomes.

### P-003 — Season length

35–42 дней.

### P-004 — Lore resource

Редкая сюжетная энергия называется Echo / Эхо.

### P-005 — Premium currency

Рабочее имя Crystals.

### P-006 — 77 origin

77 связан с древней сетью, а его номер имеет большее значение, чем простой serial ID.

### P-007 — Initial soft-launch gates

Рабочие ориентиры: D1 >= 30%, D7 8–12%+, D30 4–7%+, crash/ANR-free >99.5%. Это initial decision targets, а не вечные market truths; пересмотр требует данных и записи причины. Более старый набор D1 35% / D7 12–15% / D30 5% retired в документации v0.5 и не является вторым gate/stretch-набором.

### P-008 — Performance numeric targets

Initial targets: mid-tier 60 FPS, low-tier >=30 FPS, cold start <5 s, ordinary scene transition <2 s, base download желательно <150 MB. Финальные memory/device budgets уточняются на Vertical Slice profiling.

### P-009 — P0 core comparison set

Для Gate P0 сравниваются три PROVISIONAL hypotheses:

- A — **Energy Routing** (current favorite);
- B — **Path / Expedition Routing**;
- C — **Flow / Network Restoration**.

`Signal Sequence` больше не является текущим Prototype C. Ни один из A/B/C не LOCKED до решения P0. Для первичного сравнения implementation contract требует minimum 10 validated levels per variant, а playtest process определяется `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.

## OPEN — требуется решение

### O-002 — Screen orientation

Portrait vs landscape/isometric hybrid. Решить на vertical slice UX test.

### O-003 — Exact avatar model

Безымянный игрок / lightly defined protagonist / visible customizable avatar.

### O-004 — Exact time to ship reveal

Определить по telemetry progression.

### O-005 — First alien planet

Borealis vs Viridia vs другой мир.

### O-006 — Forced interstitials

Нужны ли вообще; по умолчанию rewarded-first.

### O-007 — Launch distribution sequence

RuStore / Google Play / iOS sequencing определяется отдельно.

### O-008 — Exact early difficulty curve

Определить по Prototype playtests/telemetry.

### O-009 — Levels per story beat

Не фиксировать до pacing tests.

### O-010 — Real content velocity / LiveOps cadence

Определить только после нескольких воспроизводимых production cycles.

### O-011 — Exact IAP price points

Не фиксировать до economy model + store/market experiments.

### O-012 — Exact production timing of 77 reveal

77 должен стать ранним компаньоном, но точная production FTUE секунда/порядок относительно первого power/repair beat не LOCKED. Active Prototype 0.1 тестирует `generator repair -> visible change/area unlock -> discover 77`; финальный pacing решить после P1/FTUE tests.

## REJECTED / NOT FOR MVP

- MMORPG backend.
- realtime PvP.
- complex combat RPG.
- paid story ending.
- launch dependence on loot boxes/gacha.
- dozen currencies visible from day 1.
