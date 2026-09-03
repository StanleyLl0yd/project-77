# Project 77 — Economy Simulation Specification

## 1. Цель

Экономику нельзя балансировать «на глаз». До полноценного магазина должна существовать воспроизводимая модель, показывающая, как ресурсы накапливаются и тратятся на горизонтах 1/7/30/90/365 дней.

## 2. Player archetypes

Минимум моделировать:

1. Casual F2P — короткая сессия, мало рекламы.
2. Active F2P — регулярная игра, часть rewarded ads.
3. Ad-active F2P — использует большинство рациональных rewarded opportunities.
4. Starter payer — одна небольшая покупка.
5. Season Pass payer.
6. Explorer Club subscriber.
7. Highly engaged payer — несколько cosmetics/convenience purchases без whale-only balance assumptions.

## 3. Inputs

Для каждого ресурса:

- starting balance;
- sources/day and sources/session;
- sinks;
- caps;
- timer effects;
- ad effects;
- pass/subscription modifiers;
- progression unlocks;
- event variance.

## 4. Outputs

Модель должна показывать:

- balance over time;
- time-to-upgrade;
- days/sessions to major story gate;
- starvation periods;
- runaway inflation;
- unused resource accumulation;
- ad dependence;
- paid acceleration advantage;
- probability/extent of deadlock;
- expected premium currency earn/spend.

## 5. Design checks

### Healthy

- игрок понимает, почему ресурс нужен;
- существует meaningful choice между несколькими sinks;
- ожидание не является единственным способом продать skip;
- платёж ускоряет/украшает, но не превращает бесплатную историю в абсурдный grind;
- rewarded ad полезен, но не обязателен для нормального темпа.

### Warning

- баланс всё время растёт и ресурс теряет смысл;
- один ресурс всегда равен нулю;
- игрок вынужден смотреть рекламу, чтобы поддерживать baseline progress;
- ускоритель экономит настолько мало времени, что не имеет ценности;
- подписка делает F2P экономику ощущаемо «неполной»;
- цена растёт быстрее, чем perceived value.

## 6. Tuning method

1. определить target pacing;
2. построить baseline без IAP;
3. добавить rewarded ads;
4. добавить subscription/pass;
5. проверить extreme active user;
6. выполнить sensitivity analysis +/-10/20/50% по главным faucet/sink параметрам;
7. только затем переносить значения в Remote Config baseline.

## 7. No loot-box dependency

Paid randomized loot boxes не являются launch-механикой Project 77.

Если когда-либо появится покупка со случайным виртуальным результатом, решение требует отдельного design/compliance review. Google Play требует раскрывать odds непосредственно перед такой покупкой: https://support.google.com/googleplay/android-developer/answer/9858738

## 8. Subscription economics

Explorer Club моделируется как recurring value, а не как пачка валюты под видом подписки.

Google Play требует sustained/recurring value на протяжении подписки: https://support.google.com/googleplay/android-developer/answer/9900533

## 9. Source of model truth

Когда появится рабочая симуляция (script/spreadsheet), её параметры являются частью release configuration и должны иметь version/history. Документ описывает правила; конкретные числа появляются только после prototype/alpha measurements.
