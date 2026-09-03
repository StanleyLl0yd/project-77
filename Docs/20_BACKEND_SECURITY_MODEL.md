# Project 77 — Backend, Offline & Security Model

## 1. Principle

Project 77 не требует MMO backend, но всё, что имеет денежную или соревновательную ценность, нельзя доверять только клиенту.

## 2. Authority matrix

### Client-authoritative / offline-tolerant

- визуальные настройки;
- локальная анимация/состояние UI;
- прохождение обычного puzzle до момента выдачи ценной награды;
- неценные tutorial markers;
- кэш контента.

### Server-authoritative или server-validated

- premium currency;
- paid entitlements;
- subscription state;
- high-value/limited rewards;
- event milestones;
- leaderboard/voting results;
- server-controlled timers;
- offer eligibility;
- inbox grants;
- anti-abuse counters.

## 3. Offline policy

Игрок может offline:

- проходить доступный core gameplay;
- просматривать/редактировать часть острова;
- выполнять безопасные локальные действия;
- копить queued non-sensitive progress.

Offline нельзя считать источником истины для:

- покупок;
- premium grants;
- global/community events;
- limited offers;
- friend/social operations;
- competitive results.

При возвращении online queued actions проходят validation/reconciliation.

## 4. Time model

Для долгосрочных таймеров и событий source of truth — server time.

Не доверять системным часам устройства для:

- build/expedition completion с экономической ценностью;
- daily reset;
- season boundaries;
- offer expiry;
- streak reward, если его можно абьюзить.

Clock tampering должен быть detectable, но реакция не должна наказывать случайную смену timezone/ошибку устройства как мошенничество.

## 5. Purchase security

- validate receipts/tokens через соответствующий provider/backend flow;
- transaction ID -> idempotency key;
- grant only once;
- store state and entitlement state разделены;
- refund/revocation может отозвать entitlement по правилам продукта;
- все ошибки имеют recoverable state, а не `purchase lost`.

## 6. Threat model — minimum

Учитывать:

- edited local save;
- rooted/emulated device;
- replayed purchase callback;
- fake client time;
- modified APK;
- duplicated network request;
- forged event completion;
- vote/like automation;
- resource inflation via offline replay;
- accidental admin misconfiguration.

Цель anti-cheat: защищать экономику и честность, не превращая casual game в DRM-проект.

## 7. API principles

- authenticated requests where identity matters;
- request IDs/idempotency for mutations;
- rate limiting;
- schema/version compatibility;
- structured errors;
- observability/correlation IDs;
- server validation for impossible values;
- no secrets in client binary.

## 8. Admin tooling security

LiveOps/admin console опаснее обычного клиента.

Требования:

- role-based access;
- MFA where provider supports;
- separate production/staging environments;
- audit log for grants/config/events;
- high-impact actions with confirmation/review;
- least privilege;
- emergency revoke access.

## 9. Privacy-by-design

Собирать только данные, которые нужны продукту, безопасности, аналитике или compliance. Diagnostic identifiers должны быть стабильными настолько, чтобы помогать support, но не становиться оправданием для лишнего трекинга.
