# Project 77 — QA, Device Matrix & Performance Budgets

## 1. Principle

«Работает хорошо» не является критерием. До vertical slice задаются измеряемые budgets и reference devices.

## 2. Android device tiers

Поддерживать три референсных класса:

### Low tier

Устройство близко к минимально поддерживаемому рынку/API с ограниченной RAM/GPU.

Target:

- gameplay >= 30 FPS;
- без критического memory pressure;
- playable quality preset;
- no feature correctness difference.

### Mid tier

Основной performance reference.

Target:

- 60 FPS в core gameplay;
- cold start ориентир **< 5 s**;
- обычные scene transitions ориентир **< 2 s**;
- стабильные frame times.

### High tier

Используется для проверки enhanced visuals/refresh rate, но game design не должен требовать high-end device.

Конкретные модели устройств фиксируются перед Vertical Slice на основании фактической целевой географии и device analytics.

## 3. Stability budgets

Initial targets:

- crash-free users/sessions > **99.5%**;
- ANR-free > **99.5%**;
- zero known save-corruption blocker;
- zero known purchase-loss blocker.

После накопления telemetry цели ужесточаются.

## 4. Download/storage

Soft target: initial/base user download желательно держать **< 150 MB**, если это не вредит качеству/надёжности. Остальной контент — delivery/bundles по мере необходимости.

Размер отслеживается в CI/release report, а не вспоминается перед store upload.

## 5. Memory

Для каждой tier/platform version фиксировать peak/steady memory budget после profiling Vertical Slice. Любой новый biome/season проверяется на regression.

Особенно тестировать:

- repeated scene transitions;
- long sessions;
- background/foreground;
- asset bundle unload;
- low-memory callbacks;
- large island accounts.

## 6. 16 KB page size

Android build pipeline обязан проверять совместимость всех native/JNI/NDK dependencies и конечного AAB/APK с 16 KB memory page sizes на 64-bit devices.

Reference: https://developer.android.com/guide/practices/page-sizes

## 7. Network matrix

QA cases:

- clean online;
- slow/high latency;
- packet loss;
- offline launch;
- online -> offline during action;
- offline -> online reconciliation;
- Wi-Fi -> mobile data;
- server 4xx/5xx/timeouts;
- duplicate/retried request.

## 8. Lifecycle matrix

- force close;
- OS kill/background reclaim;
- screen lock;
- app pause/resume;
- phone call/system interruption where applicable;
- update over existing install;
- reinstall;
- device migration.

## 9. Battery/thermal

Проверять длинную 30–60 minute session на mid/low tier. 60 FPS не является оправданием чрезмерного нагрева; 30 FPS/battery saver должен быть доступен при необходимости.

## 10. Regression automation

Автоматизировать в первую очередь:

- save migrations;
- economy formulas;
- purchase idempotency logic;
- Remote Config parsing/defaults;
- core puzzle deterministic rules;
- content schema validation;
- build smoke test.
