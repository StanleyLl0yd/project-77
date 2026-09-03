# Project 77 — Product Vision

## 1. Elevator pitch

**Project 77** — мобильное приключение, которое начинается как уютная игра о восстановлении заброшенного острова, а затем постепенно раскрывается в научно-фантастическую историю о древнем корабле, других планетах и сети миров.

Игрок проходит короткие головоломки, получает ресурсы, восстанавливает и украшает свой дом, исследует новые зоны, находит артефакты и раскрывает тайну исчезнувшей экспедиции. Масштаб истории расширяется поэтапно, но остров остаётся постоянной персональной базой и визуальной летописью всего прогресса.

## 2. Product fantasy

Игрок должен чувствовать:

1. **Это мой мир.** Я сам восстановил остров и сделал его уникальным.
2. **Здесь есть тайна.** Что-то не сходится, и я хочу узнать, что произошло.
3. **Мне всегда есть что открыть.** Новая зона, персонаж, коллекция, секрет, планета.
4. **Масштаб постоянно растёт.** Остров — лишь начало.
5. **Я не обязан платить, чтобы увидеть историю.** Деньги дают красоту, удобство, темп и дополнительные цели, но не покупают финал.
6. **Мой прогресс виден другим.** Остров — витрина достижений и истории игрока.

## 3. Целевая аудитория

### Primary

Product positioning remains teens/adults (13+ direction) per Decision Log; **16–55+ below is the primary marketing/design focus, not a conflicting store-age declaration**.

Массовые мобильные игроки 16–55+, которым нравятся:

- короткие игровые сессии;
- головоломки с одним простым правилом;
- восстановление/декорирование;
- коллекционирование;
- ощущение постоянного прогресса;
- лёгкая научная фантастика без сложного лора на старте.

### Secondary

- поклонники mystery/adventure;
- игроки, любящие LiveOps и сезонные коллекции;
- completionist-аудитория;
- игроки, которые редко платят много, но готовы иногда покупать недорогие визуальные предметы или пропуск.

### Не целевая аудитория на старте

- hardcore PvP;
- игроки, ожидающие сложную RPG-боёвку;
- MMO-аудитория;
- игроки, которым нужен competitive esports loop.

## 4. Позиционирование

Не рекламировать Project 77 как «космическую игру» на старте.

Первое обещание:

**Восстанови заброшенный остров. Найди следы исчезнувшей экспедиции. Узнай, что здесь произошло.**

Визуальный маркетинг первого периода:

- остров;
- маяк;
- старый научный лагерь;
- пещера;
- робот 77;
- странные сигналы;
- необычные металлические фрагменты.

Не показывать в первых креативах:

- полноценный космический корабль;
- карту галактики;
- чужие планеты;
- сеть врат.

Это не технический секрет, который невозможно раскрыть в интернете. Это управляемый narrative surprise: игрок должен иметь шанс испытать момент открытия самостоятельно.

## 5. Pillars

### P1. One-finger clarity

Core gameplay объясняется одним предложением и управляется одним пальцем.

### P2. Mystery as retention

Игрок возвращается не только ради награды, но и ради вопроса: «что дальше?»

### P3. Home forever

Остров никогда не становится устаревшей стартовой зоной. Он остаётся персональной базой, выставкой достижений и социальным профилем.

### P4. Endless expandable content

Новые планеты, биомы, коллекции и сезоны добавляются без необходимости каждый раз менять основной gameplay.

### P5. Fair F2P

Основная история не продаётся. Нет обязательной гачи, платного PvP-преимущества и искусственно созданных paywall-пиков.

### P6. Live world

События, странные находки, глобальные загадки и сезонные изменения создают ощущение, что мир существует и без игрока.

## 6. High-level genre

**Hybrid-casual puzzle adventure + collection/base restoration + mystery/live service.**

Это не классический city builder и не полноценная survival-игра. Строительство — эмоциональная метаигра и визуальный прогресс, а не сложная экономическая симуляция.

## 7. Platform strategy

Production baseline: Unity 6.3 LTS + C# + URP, Android-first. Архитектура должна допускать Android/iOS без изменения core design. Для Android платежи абстрагируются от конкретного магазина: Google Play Billing для Google Play и RuStore Pay provider для RuStore-сборки. Store, ads, analytics, cloud save, authentication и notifications не должны проникать в core gameplay.

На 2 сентября 2026 года RuStore прекратил обработку покупок через старый BillingClient SDK с 1 августа 2026 года и рекомендует Pay SDK. Google Play требует Play Billing для цифровых товаров и подписок в приложениях, распространяемых через Google Play.

## 8. Market rationale — 2026

Не считать эти данные гарантией успеха; они лишь поддерживают направление.

- Sensor Tower: hybrid-casual IAP revenue в H1 2026 вырос примерно на 23% год к году до ~$2.4B.
- Sensor Tower: мобильный рынок стал более monetization-first; при дорогом user acquisition важнее retention, LiveOps и эффективность LTV.
- Sensor Tower: ad-supported games дали около 83% загрузок в исследованных рынках в 2025; гибридная модель IAP + rewarded ads остаётся рациональной.
- LiveOps в 2026 рассматривается как ключевой инструмент retention и monetization, особенно в puzzle/casual.

Источники:
- https://sensortower.com/blog/h1-2026-digital-gaming-market-index
- https://sensortower.com/report/state-of-mobile-2026
- https://sensortower.com/report/gaming-deep-dive-ad-monetization
- https://sensortower.com/report/2026-live-ops-competitive-intelligence-playbook
- https://support.google.com/googleplay/android-developer/answer/10281818
- https://www.rustore.ru/developer/en/blog/transition-from-billingclient-sdk-to-pay-sdk-a-complete-guide-for-developers

## 9. North Star

Не «скачивания» и не «количество уровней».

North Star проекта:

**доля игроков, которые продолжают развивать свой остров и открывать новый контент спустя недели после установки.**

Практический proxy: Weekly Engaged Explorers — пользователи, которые за неделю сделали минимум одно действие в каждой из трёх категорий:

1. сыграли core-пазл;
2. продвинули/украсили базу или коллекцию;
3. открыли сюжет/экспедицию/событие.

## 10. Product promise

Project 77 должен быть игрой, которую легко начать за минуту, но невозможно полностью «увидеть» за один вечер.

## Production-readiness clarifications (v0.3)

- **Project 77 is an internal codename**, not a cleared commercial title.
- Intended product positioning: teens/adults (13+ direction), family-accessible but not child-directed.
- Launch social deliberately excludes free-form chat/UGC.
- Product progression is governed by measurable gates; content spend does not continue on sunk-cost logic.
- Public brand name, final store age declaration and exact LiveOps cadence are deferred until evidence/clearance exists.
