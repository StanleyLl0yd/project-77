# Project 77 — Prototype 0.1 Specification

Статус: **ACTIVE IMPLEMENTATION CONTRACT**.

Этот документ определяет ближайший обязательный scope Project 77 после завершения pre-production. Его задача — не доказать красоту, масштаб вселенной или монетизацию, а проверить, работает ли фундаментальный игровой опыт.

## 1. Phase status

**Pre-production завершён. Project 77 находится в Prototype Phase.**

До прохождения Prototype Gate проект не расширяется в production scope.

## 2. Главная гипотеза

Игроку должен быть приятен и понятен цикл:

```text
Launch
↓
Very short intro
↓
Puzzle
↓
Reward
↓
Island
↓
Repair / Visible Change
↓
Discovery
↓
Next Puzzle
```

Ключевой вопрос Prototype 0.1:

> Хочет ли игрок сам нажать «продолжить» после того, как puzzle превратился в заметный прогресс на острове?

## 3. Цель Prototype 0.1

Проверить две связанные вещи:

1. Core puzzle приятно играть даже на placeholder-графике.
2. Meta loop острова усиливает желание пройти следующий puzzle.

Прототип не обязан выглядеть как финальная игра. Он обязан быть достаточно целостным, чтобы внешний тестировщик понял правила без разработчика рядом.

## 4. Technical baseline

- Unity 6.3 LTS.
- C#.
- URP.
- Android build target.
- Android-first, platform-neutral core code.
- Placeholder art/UI допускается и предпочтителен.
- Базовая telemetry обязательна до внешних playtests.
- GitHub repository `project-77`.
- Документация хранится в `/Docs`.

## 5. Core mechanic test

До окончательной фиксации core реализуются и сравниваются минимум три дешёвых варианта:

- **A — Energy Routing**: текущий фаворит.
- **B — Path / Expedition Routing**.
- **C — Flow / Network Restoration**.

Для каждого варианта достаточно примерно 10–20 технических уровней, если меньшего количества недостаточно для уверенного вывода.

Выбор делается по измерениям и playtests, а не по вкусу автора/разработчика.

## 6. Prototype 0.1 minimum content

После выбора core-механики сборка должна содержать:

- очень короткое вступление;
- 10–20 коротких puzzle levels;
- простой reward screen;
- два базовых ресурса как минимум в тестовом виде: Scrap и Energy или их эквиваленты;
- минимальный остров/карта из placeholders;
- повреждённый генератор;
- один закрытый участок острова;
- восстановление генератора за заработанные ресурсы;
- видимое изменение мира после ремонта;
- открытие нового участка;
- обнаружение повреждённого робота **77**;
- переход к следующему puzzle;
- базовые analytics events для FTUE, level start/finish/fail/retry, reward, repair, discovery и next-level intent/action.

## 7. Definition of Done

Prototype 0.1 считается функционально законченным, если новый игрок без устных подсказок разработчика способен:

1. запустить игру;
2. понять цель первого puzzle;
3. пройти первый уровень;
4. понять, что получил награду;
5. понять, зачем нужен хотя бы один из ресурсов;
6. потратить ресурс на восстановление генератора;
7. увидеть причинно-следственную связь «puzzle → ресурс → изменение острова»;
8. открыть новый участок;
9. обнаружить 77;
10. получить понятный следующий игровой шаг.

Но продуктовый Definition of Done требует ещё одного пункта:

> Игрок **сам хочет продолжить**, а не продолжает потому, что тестировщик/разработчик попросил.

## 8. Prototype Gate metrics

Процесс и стартовые ориентиры описаны в `18_PRODUCT_GATES.md`.

Для core отдельно проверяем:

- tutorial/core comprehension без устного объяснения;
- `next puzzle` voluntary rate;
- среднее/медианное время уровня;
- retry/frustration;
- частые ошибки input/goal comprehension;
- субъективное «ещё один уровень»;
- удобство на разных размерах Android-экранов.

Для meta проверяем:

- понимает ли игрок ценность ресурсов;
- является ли ремонт заметным reward;
- усиливает ли island action желание пройти новый puzzle;
- воспринимается ли открытие 77 как discovery/reward, а не tutorial-popup;
- нет ли ощущения двух несвязанных игр: puzzle отдельно, остров отдельно.

## 9. Explicitly OUT of scope

До успешного Prototype Gate **не реализовывать как production systems**:

- IAP/store;
- реальные цены;
- rewarded/interstitial ads;
- Season Pass;
- Explorer Club subscription;
- production backend/microservices;
- полноценные cloud/social systems;
- realtime multiplayer;
- большой island map;
- десятки персонажей;
- сотни props/decorations;
- полноценные cinematics;
- другие планеты;
- корабль как игровой слой;
- production-quality VFX/audio/content;
- большой narrative content pack;
- expensive marketing asset production.

Допустимы только минимальные mocks/stubs, если они нужны для архитектурной проверки и не расширяют scope.

## 10. No-premature-production guardrail

До решения **CONTINUE** по Gate P0 и P1 нельзя использовать аргументы:

- «потом графика вытянет»;
- «когда добавим сюжет, станет интереснее»;
- «мы уже много сделали, надо продолжать»;
- «нужно ещё 100 уровней, чтобы понять»;
- «монетизация компенсирует слабое удержание».

Если core/meta loop не работает на дешёвой версии, сначала меняется loop.

## 11. Deliberately unresolved after pre-production

Следующие вопросы **не являются пробелом документации**. Они намеренно оставлены для экспериментов и данных:

- победившая core mechanic;
- точная difficulty curve;
- portrait vs landscape;
- точный темп mystery reveal;
- количество уровней между story beats;
- реальные IAP price points;
- реальные D1/D7/D30;
- production content velocity;
- оптимальный season cadence;
- финальное коммерческое название;
- долгосрочный content plan на годы.

## 12. Data-over-speculation rule

После старта Prototype Phase новая крупная design-идея не должна автоматически становиться committed scope.

Порядок:

`Idea -> Backlog -> Hypothesis -> Prototype/Test -> Decision -> Documentation update`.

Если вопрос можно дешёво проверить поведением игроков, тест имеет приоритет над дальнейшим теоретическим проектированием.

## 13. What happens after a successful Prototype Gate

Только после подтверждения P0/P1 начинается Vertical Slice:

- один production-quality island sector;
- финализируемый UI language;
- silhouette/animation 77;
- 15–25 polished levels;
- один законченный narrative beat;
- один mystery clue/anomaly;
- save/load prototype;
- reference-device profiling.

Следующий пакет документации после реальных тестов должен обновляться **по данным**, а не ради увеличения объёма GDD.
