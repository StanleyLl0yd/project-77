# Project 77 — Economy & Monetization

## 1. Monetization thesis

Project 77 — free-to-play, но не «бесплатная демо-версия».

Бесплатный игрок должен иметь возможность:

- пройти основную историю;
- открыть все основные миры;
- получить базовый набор персонажей;
- развивать остров;
- участвовать в сезонах и событиях;
- получать premium currency ограниченными бесплатными способами.

Платящий игрок получает больше:

- персонализации;
- темпа;
- удобства;
- дополнительных наград;
- коллекционных целей.

## 2. Revenue pillars

### 2.1 Season Pass — основной IAP pillar

Рабочая длительность: 35–42 дней.

Две дорожки:

- Free;
- Premium.

Premium даёт:

- тематические cosmetics;
- premium currency;
- дополнительные boosters;
- уникальный island trophy/skin;
- cosmetic для 77/корабля.

Не включать обязательную основную сюжетную главу только в Premium.

### 2.2 Explorer Club — subscription

Гипотеза цены определяется рынком и тестами; для RU можно начинать с относительно низкого психологического порога, а не копировать западный $9.99.

Возможные преимущества:

- no forced interstitials;
- daily premium stipend;
- +1 expedition slot;
- небольшая дополнительная daily reward;
- subscriber cosmetic each month;
- badge/profile frame;
- quality-of-life convenience.

Не давать безусловный множитель силы, который делает non-subscriber вторым сортом.

### 2.3 Cosmetics

- 77 skins;
- island themes;
- building variants;
- ship skins;
- character outfits;
- weather/lighting packs;
- pets/ambient creatures.

Это самый безопасный долгосрочный каталог.

### 2.4 Convenience IAP

- booster bundles;
- extra expedition ticket;
- timer skips;
- retry pack;
- resource bundle.

Нельзя проектировать искусственный дискомфорт только для продажи решения.

### 2.5 Starter Pack

Одноразовый недорогой пакет после того, как игрок уже понял игру:

- premium currency;
- 1 exclusive cosmetic;
- 2–3 boosters;
- small resource pack.

### 2.6 Rewarded ads

Предпочтительный формат рекламы.

Примеры opt-in:

- x2 часть награды после уровня;
- дополнительная expedition reward;
- бесплатный booster;
- ускорение небольшого таймера;
- extra daily chest.

## 3. Forced ads policy

Если forced interstitials вообще используются, то:

- не в первые onboarding sessions;
- не после каждого уровня;
- не перед/после эмоциональной сюжетной сцены;
- frequency cap;
- Explorer Club/Ad-free purchase их отключает.

Главная гипотеза: rewarded-first, forced-light.

## 4. No loot-box dependency

Не делать random loot boxes основой revenue.

Если существуют mystery rewards, игрок должен иметь достаточный бесплатный доступ, а покупка не должна строиться на непрозрачных шансах. Для launch лучше вообще обойтись без paid randomized boxes.

## 5. Currency design

### Soft

Supplies / Parts / Research Data.

Правило: каждый ресурс должен отвечать на понятный вопрос «зачем он существует?». Если два ресурса выполняют одно и то же — объединить.

### Premium: Crystals

Источники:

- IAP;
- achievements;
- Season Pass free track;
- events;
- rare discoveries;
- limited daily/weekly sources.

Sinks:

- cosmetics;
- convenience;
- select bundles;
- optional acceleration.

### Echo

Сюжетная ценность. Не продавать напрямую как обычную пачку.

## 6. Economy faucets/sinks

Для каждой валюты вести таблицу:

- source;
- average per day/week;
- sink;
- expected spend;
- target balance;
- early/mid/late game.

Экономика не должна балансироваться «на глаз» после запуска. Все выдачи и цены — remote-configurable.

## 7. Store architecture

Каталог:

1. Featured.
2. Season.
3. Cosmetics.
4. Convenience.
5. Currency.
6. Subscription.

Не создавать 20 flashing offers одновременно.

## 8. Offer logic

Разрешены:

- first purchase;
- milestone offer;
- comeback offer;
- seasonal bundle;
- collection completion helper.

Запрещены deceptive offers и fake scarcity.

## 9. Regional payment architecture

Платёжный слой должен быть provider-based.

Пример интерфейса:

`StoreProvider -> queryProducts / purchase / restore / subscriptionStatus / acknowledge / refund-state`

Реализации:

- Google Play Billing;
- RuStore Pay SDK;
- iOS StoreKit при iOS-релизе.

Не смешивать бизнес-логику inventory с SDK конкретного магазина.

На 02.09.2026:

- Google Play требует собственную billing system для цифровых IAP/подписок в приложениях из Google Play, если не применимо исключение политики.
- RuStore старый BillingClient уже deprecated; обработка через него прекращена с 01.08.2026, актуальная интеграция — Pay SDK.

Sources:
- https://support.google.com/googleplay/android-developer/answer/10281818
- https://www.rustore.ru/help/sdk/payments
- https://www.rustore.ru/developer/en/blog/pay-sdk-for-developers-everything-about-rustore-s-new-payment-solution

## 10. Monetization sequence

Не показывать магазин в первые секунды.

Пример:

- session 1: никаких hard sales;
- после первой персонализации: показать, что cosmetics существуют;
- после формирования ценности: optional starter offer;
- Season Pass показывать после открытия событий/сезонной шкалы;
- subscription — после нескольких возвращений, когда игрок понимает daily loop.

## 11. Revenue experiments

Тестировать:

- placement, а не только цену;
- состав starter pack;
- rewarded ad multiplier;
- subscription benefits;
- season length;
- free/premium reward spacing;
- cosmetic bundles.

Не проводить одновременно несколько экспериментов, которые ломают интерпретацию LTV/retention.

## 12. Success condition

Монетизация считается здоровой, если рост ARPDAU/LTV не сопровождается заметным падением D1/D7, session count или review sentiment.


## 13. Production-readiness monetization rules

- Explorer Club должен давать recurring value весь subscription period; одноразовая currency/booster пачка не является достаточной подписочной ценностью.
- Paid randomized loot boxes не входят в launch scope.
- Economy проходит симуляцию для нескольких archetypes на горизонтах 1/7/30/90/365 дней до масштабирования IAP.
- Premium grants и entitlements server-validated; purchase processing idempotent.
- Store price отображается из billing provider; Remote Config не должен подменять фактическую store price.
