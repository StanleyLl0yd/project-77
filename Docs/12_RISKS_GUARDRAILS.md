# Project 77 — Risks & Guardrails

## 1. Biggest product risk: beautiful meta, boring core

Сюжет и остров могут выглядеть отлично, но если puzzle не выдерживает сотни повторений, retention рухнет.

Mitigation:

- greybox prototypes first;
- тест «сыграл бы ещё без награды?»;
- не производить сотни уровней до выбора core.

## 2. Scope explosion

Остров + персонажи + пазлы + космос + LiveOps легко превращаются в проект уровня большой студии.

Guardrail:

- launch story ограничена островом/underground reveal;
- планеты не обязаны быть в первом публичном билде;
- asynchronous social first;
- no combat system unless proven necessary.

## 3. Reveal spoiled by marketing

Если маркетинг сразу продаёт планеты, теряется главный «масштабный» surprise.

Mitigation:

- spoiler tiers;
- разные creative packs для acquisition после зрелости продукта;
- первые материалы Tier 0–1.

## 4. Monetization damages trust

Риск особенно высок, если игрок эмоционально привязался к island story.

Guardrails:

- no story paywall;
- rewarded-first ads;
- no purchase popups during reveals;
- transparent prices;
- no paid lootbox launch dependency.

## 5. Content treadmill

Если каждое обновление требует новую огромную планету, экономика производства не сойдётся.

Mitigation:

- seasons use existing worlds;
- modular planet package;
- collections/decorations reuse base systems;
- procedural assistance only with human validation.

## 6. Too many currencies/systems

Casual player может уйти из-за UI overload.

Guardrail:

- progressive disclosure;
- максимум 2–3 видимых базовых ресурса early game;
- удалять ресурс, если его функция дублируется.

## 7. Island loses relevance after space

Если новые планеты лучше во всём, дом станет старым меню.

Mitigation:

- все крупные победы возвращаются трофеями;
- island hub содержит музей, characters, social showcase;
- periodic island expansions;
- seasonal decorations applied at home.

## 8. 77 becomes annoying tutorial mascot

Mitigation:

- редко повторяет очевидное;
- tutorial chatter уменьшается;
- meaningful character arc;
- optional dialogue log;
- humour дозирован.

## 9. LiveOps fatigue/FOMO

Mitigation:

- catch-up;
- не более нескольких одновременных событий;
- core rewards доступны без ежедневного марафона;
- old cosmetic content может возвращаться контролируемо.

## 10. Technical save loss

Критический reputational risk.

Mitigation:

- cloud backup;
- schema migration tests;
- entitlement recovery;
- support tooling;
- idempotent purchase processing.

## 11. Fake complexity

Не добавлять RPG stats, gear score, десятки upgrade trees только потому, что они монетизируются в других играх.

Project 77 должен оставаться понятным casual adventure.

## 12. Ethical red lines

- no gambling positioning;
- no dark pattern pricing;
- no fake close buttons;
- no forced social spam;
- no monetized griefing;
- no loss of purchased cosmetics because of season expiration;
- no critical progress dependent on watching ads.

## Prototype-first production guardrail

**Риск:** команда начинает с красивого острова, персонажей, магазина, backend и большого количества контента, не доказав, что 30–90-секундный core loop вообще приятен.

**Правило:** до прохождения Core Prototype gate запрещено масштабировать art/content production и monetization infrastructure. Placeholder-графика — ожидаемое состояние Prototype 0.1.

**Критерий:** если puzzle не вызывает желания сыграть ещё один уровень без красивого оформления, графика не считается решением проблемы.

## 13. Operating blind spots

Запрещено откладывать «на потом» следующие системные вопросы:

- save migration/recovery;
- purchase idempotency;
- server-time timers;
- Remote Config rollback/killswitches;
- target audience/privacy/UGC implications;
- reference device/performance budgets;
- content throughput and QA capacity;
- incident playbooks;
- commercial-title/IP clearance.

## 14. False certainty guardrail

Не фиксировать как canon/production commitment числа и ответы, которые можно узнать только из telemetry. Для таких вопросов фиксируются measurement method, initial target и decision rule.

## 15. Prototype architecture astronautics

**Risk:** P0/P1 turns into a production-platform exercise (DI framework, event bus, backend, provider abstractions, generic content platform) before the game loop is proven.

**Guardrail:** follow `31_ENGINEERING_CONVENTIONS.md`; create only deterministic puzzle/domain code, testable config/validation, minimal presentation, minimal telemetry and the small meta state required by `28_PROTOTYPE_01_SPEC.md`. Future architecture described elsewhere is a constraint, not current implementation scope.

## 16. Biased playtest evidence

**Risk:** moderator hints or inconsistent telemetry make weak mechanics look successful.

**Guardrail:** P0/P1 external tests follow `29_PROTOTYPE_PLAYTEST_PROTOCOL.md` and `30_PROTOTYPE_ANALYTICS_CONTRACT.md`. Prompted continuation is not counted as voluntary continuation.
