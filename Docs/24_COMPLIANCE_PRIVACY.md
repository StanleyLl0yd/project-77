# Project 77 — Compliance, Privacy & Audience Baseline

Policy snapshot date: **2026-09-02**. Перед каждым релизом требования магазинов перепроверяются; этот файл не заменяет юридическую консультацию.

## 1. Intended audience — LOCKED product direction

Project 77 проектируется прежде всего для **подростков и взрослых (13+ product positioning)**, а не как приложение специально для детей.

Принципы:

- family-accessible tone допустим;
- не использовать toddler/kids-directed positioning;
- store target-audience declarations должны честно соответствовать фактическому продукту;
- финальный age/content rating определяется по реальному контенту и store questionnaire.

Если когда-либо дети включаются в declared target audience, включаются дополнительные Families requirements. Google Play прямо указывает, что при наличии детей среди целевых групп применяются Families Policy Requirements, включая ограничения на data practices и ads SDKs.

Reference: https://support.google.com/googleplay/android-developer/answer/9893335

## 2. Social / UGC — launch baseline

На launch:

- island visits;
- likes/reactions;
- predefined messages/stickers;
- showcase;
- community goals.

**Нет free-form chat, DMs, comments, user-uploaded images/text и другого свободного UGC.**

Причина — одновременно продуктовая и операционная: свободный UGC требует постоянной moderation infrastructure. Google Play UGC policy требует robust ongoing moderation, Terms of Use, reporting/blocking и действия по жалобам для соответствующих UGC-функций.

Reference: https://support.google.com/googleplay/android-developer/answer/9876937

## 3. Privacy-by-design

До интеграции SDK для каждого типа данных фиксировать:

- что собираем;
- зачем;
- retention period;
- processor/provider;
- region/storage assumptions;
- whether data is linked to identity;
- deletion path;
- Data Safety / privacy policy disclosure.

Не подключать SDK «на всякий случай».

## 4. Consent & ads

Реклама/аналитика/attribution проектируются с учётом региональных consent requirements и возрастной модели. Конкретная consent platform/provider выбирается позже, но SDK должны поддерживать отключение/ограничение сбора до consent там, где это требуется.

## 5. Google Play Android baseline as of 2026-09-02

Для обычных mobile apps с 31 августа 2026 новые приложения и обновления должны target Android 16 / **API 36+**.

Reference: https://developer.android.com/google/play/requirements/target-sdk

Для приложений targeting Android 15 / API 35+ на 64-bit devices действует требование поддержки **16 KB memory page sizes**; с 1 февраля 2027 несовместимые обновления нельзя выпускать через Google Play.

Reference: https://developer.android.com/guide/practices/page-sizes

## 6. Billing

Digital goods/subscriptions в Google Play используют Play billing, если не применяется разрешённая policy exception/program.

Reference: https://support.google.com/googleplay/android-developer/answer/10281818

Store-specific billing изолируется provider adapter-ами.

## 7. Subscription rule

Explorer Club должен давать sustained/recurring value весь период подписки. Не использовать подписку как одноразовую пачку currency/boosters.

Reference: https://support.google.com/googleplay/android-developer/answer/9900533

## 8. Randomized paid items

Paid loot-box launch dependency запрещена продуктовым решением. Если когда-либо появляется paid randomized virtual item, требуется отдельный compliance review; Google Play требует раскрывать odds до покупки.

Reference: https://support.google.com/googleplay/android-developer/answer/9858738

## 9. Account deletion & support

До public launch должны быть определены:

- account deletion workflow;
- deletion vs legally/financially required transaction records;
- privacy request handling;
- user-facing support channel;
- Player ID usable without exposing secrets.

## 10. Accessibility baseline

Закладывать сразу:

- UI scale/readability;
- не кодировать важное состояние только цветом;
- достаточный contrast;
- reduced motion where practical;
- haptic/audio не единственный канал critical feedback;
- one-handed/touch target ergonomics;
- subtitles/text for narrative audio.

## 11. Localization architecture

С первого дня:

- никакого user-facing hardcoded text;
- localization keys;
- plural/number/date formatting;
- variable-safe strings;
- text expansion allowance;
- separate store/localization content pipeline.
