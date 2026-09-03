# Project 77 — Technical Architecture

## 0. Current-phase boundary

This document describes the production-capable direction, **not the implementation scope of Prototype 0.1**. Until Gate P0 + P1 return CONTINUE, follow `28_PROTOTYPE_01_SPEC.md` and `31_ENGINEERING_CONVENTIONS.md`: implement deterministic puzzle/domain code, placeholder presentation, minimal local prototype telemetry, and only the smallest meta state required for generator repair/77 discovery.

Do **not** implement the production backend, store, ads, cloud/social, LiveOps platform, or provider adapters merely because they are described below. Architecture requirements for future phases are constraints to preserve, not a mandate to build them now.

## 1. Goals

Архитектура должна поддерживать:

- Android/iOS;
- несколько app stores;
- remote-configured economy/events;
- data-driven content;
- cloud save;
- LiveOps без выпуска новой версии для каждой таблицы наград;
- offline-tolerant core gameplay;
- масштабирование без MMO backend.

## 2. Locked implementation baseline

Основной production-стек зафиксирован:

- **Engine:** Unity 6.3 LTS.
- **Language:** C#.
- **Rendering:** Universal Render Pipeline (URP).
- **Primary target:** Android-first.
- **Future targets:** iOS, затем при наличии продуктового смысла PC/Web/другие платформы.
- **Repository:** GitHub.
- **CI/CD baseline:** GitHub Actions + Unity-compatible build automation.
- **Android release format:** AAB как основной store artifact; APK допустим для внутреннего тестирования/direct install.
- **Minimum Android:** API 26 (Android 8.0+) как продуктовый baseline, пока технические или рыночные причины не потребуют изменения.
- **Google Play target baseline (02.09.2026):** targetSdk API 36+; compileSdk не ниже target и latest stable, поддерживаемый выбранным Unity/Android toolchain.
- **64-bit page-size compatibility:** все native/JNI/NDK зависимости и итоговые builds обязаны поддерживать 16 KB memory pages.
- **CPU ABI:** arm64-v8a обязательно; дополнительные ABI добавляются только при доказанной необходимости.
- **Performance target:** 60 FPS как основной режим; 30 FPS fallback для слабых устройств/энергосбережения.

Почему Unity: Project 77 требует не только игрового клиента, но и зрелого мобильного коммерческого контура — IAP, rewarded ads/mediation, analytics, remote config, cloud save, attribution, LiveOps, Android/iOS и интеграции нескольких магазинов. Unity выбран как наиболее практичный компромисс между скоростью производства, зрелостью mobile ecosystem и кроссплатформенностью.

Godot и Unreal не являются текущими production-кандидатами. Возврат к выбору движка возможен только отдельным архитектурным решением с доказанной причиной.

## 3. Client architecture

Production direction for layers/modules (create only when active scope needs them):

- Core Game / puzzle simulation;
- Meta Game / island;
- Narrative;
- Economy;
- Inventory;
- LiveOps;
- Store abstraction;
- Ads abstraction;
- Analytics abstraction;
- Save/Sync;
- Content/Config;
- UI;
- Platform services.

SDK конкретного магазина не должен проникать в economy logic.

### Platform service boundaries

Core-код с первого дня не должен быть Android-specific. During Prototype 0.1, do not create empty interfaces/providers for out-of-scope services. When a service enters scope, keep it behind a narrow adapter. Production direction:

```text
Project 77 Core
├── Gameplay
├── World
├── Narrative
├── Economy
├── Progression
├── LiveOps
└── Save System

Platform Services
├── IStore
│   ├── GooglePlayStoreProvider
│   ├── RuStorePayProvider
│   └── AppleStoreProvider
├── IAds
├── IAnalytics
├── ICloudSave
├── IAuthentication
└── INotifications
```

Переход на iOS не должен требовать переписывания gameplay/economy/narrative слоёв. Платформенные SDK подключаются только через адаптеры.

## 4. Data-driven definitions

Config entities:

- levels;
- rewards;
- items;
- buildings;
- quests;
- dialogues;
- events;
- seasons;
- offers;
- prices display metadata;
- expeditions;
- unlock conditions.

Валидация выполняется до публикации конфигов.

## 5. Backend scope

На launch не нужен realtime MMO backend.

Минимум:

- account identity;
- cloud save;
- remote config;
- event schedule;
- inbox/gifts;
- purchase verification where required/recommended;
- entitlement service;
- social profile snapshots;
- analytics ingestion through provider;
- admin tools.

Можно использовать managed services на ранней стадии, сохраняя слой абстракции для критичных данных.

## 6. Save model

Критично.

Нужно:

- local save;
- server/cloud authoritative merge rules для важных entitlements;
- versioned schema;
- migration tests;
- backup snapshots;
- conflict resolution;
- anti-duplication для IAP.

Потеря острова = один из худших возможных инцидентов.

## 7. Offline model

Можно локально:

- играть часть puzzle levels;
- менять часть декора;
- читать уже полученный narrative.

Нужна сеть:

- IAP;
- ads;
- LiveOps claims;
- social;
- account sync;
- authoritative timed rewards.

## 8. Store abstraction

Interface concept:

- initialize();
- loadCatalog();
- purchase(productId);
- restorePurchases();
- getEntitlements();
- getSubscriptionState();
- acknowledge/consume as provider requires.

Providers:

- GooglePlayStoreProvider;
- RuStorePayProvider;
- AppleStoreProvider.

## 9. Security

- никогда не доверять client-only premium entitlement, если его можно проверить;
- server-side validation для high-value purchases/entitlements где возможно;
- signed config/content manifests;
- rate limits;
- no secrets in client;
- secure account linking;
- audit trail admin actions.

## 10. Analytics event contract

События не должны формироваться как произвольные строки из десятков мест. Использовать централизованный typed schema/versioning. Prototype 0.1 uses the exact minimal schema in `30_PROTOTYPE_ANALYTICS_CONTRACT.md`; long-term production analytics may expand later without changing puzzle-domain rules.

## 11. Content delivery

Большие планеты и seasonal assets желательно доставлять как remote asset bundles/content packs, чтобы base app не рос бесконечно.

Нужны:

- version manifest;
- resumable download;
- rollback;
- cache cleanup;
- minimum compatible client version.

## 12. QA matrix

Минимум:

- low/mid/high Android devices;
- разные aspect ratios;
- 60/90/120 Hz;
- network loss/reconnect;
- background/foreground during purchase;
- interrupted asset download;
- clock manipulation;
- save conflict;
- store unavailable;
- ad unavailable;
- locale change.

## 13. Performance targets

- stable frame pacing;
- low thermal load in puzzle mode;
- fast resume;
- memory budget фиксируется после engine spike;
- island asset count ограничивается LOD/culling/batching strategy.

Не гнаться за тяжёлой графикой, которая исключает mid-range массовую аудиторию.

## Production-readiness additions (v0.3)

### 16 KB memory pages

Все native/JNI/NDK зависимости и конечные Android builds должны тестироваться на 16 KB memory page size compatibility для 64-bit devices. Это release gate, а не post-launch optimization.

### Offline / authority

Core gameplay допускает offline-tolerant работу. Premium currency, purchases, subscriptions, valuable event grants, social/global state и meaningful timers валидируются сервером. Device clock не считается source of truth для экономики.

### Saves

Save schema versioned; migrations tested. Cloud provider находится за interface. Entitlements и premium economy отделены от обычного local save.

### Remote Config

Economy/difficulty/events/offers/ads/features имеют встроенные defaults + remote overrides. Обязательны environments, config version, rollback и emergency kill switches.

### Operations

Backend/API проектируются с idempotent mutations, structured errors, request correlation, rate limiting, admin audit log и backward compatibility на период staged rollout.
