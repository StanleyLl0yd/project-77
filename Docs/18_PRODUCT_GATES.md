# Project 77 — Product Gates

Статус: **ACTIVE — Prototype Phase gate framework**.

## 1. Зачем нужны gates

Project 77 не должен развиваться по принципу «вроде нравится — делаем дальше». Каждый крупный этап имеет заранее определённый вопрос, набор измерений и одно из решений: **CONTINUE / ITERATE / PIVOT / STOP**.

Сами gates — LOCKED как процесс. Численные пороги являются **initial targets** и могут быть пересмотрены только после документированного теста и записи причины в Decision Log. P0/P1 sessions следуют `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`; prototype telemetry следует `30_PROTOTYPE_ANALYTICS_CONTRACT.md`.

## 2. Gate P0 — Core Prototype

Вопрос: хочется ли играть в 30–90-секундную механику без production-art и сюжета?

Проверяем на fresh external testers по playtest protocol. Начальный ориентир сравнения — минимум 10 fresh exposures на вариант, если variant не остановлен досрочно из-за повторяемого severe usability failure. Assisted sessions не засчитываются как unaided comprehension/voluntary continuation без соответствующей маркировки.

Initial targets:

- tutorial completion: **>= 85%**;
- игрок понимает правило без устного объяснения разработчика;
- **>= 70%** добровольно запускают следующий puzzle по формальному определению `voluntary continue` из playtest protocol;
- медианное время уровня попадает в целевой диапазон 30–90 секунд;
- нет систематической путаницы с input/goal/fail state;
- qualitative: большинство может своими словами объяснить, почему им было приятно/интересно продолжить.

Решения:

- CONTINUE: одна механика явно сильнее альтернатив и проходит qualitative + quantitative checks;
- ITERATE: понятная проблема UX/difficulty, не требующая смены core;
- PIVOT: все варианты понятны, но не вызывают желания «ещё один уровень»;
- STOP: core не показывает потенциал после ограниченного числа итераций.

## 3. Gate P1 — Core + Meta Loop

Вопрос: усиливает ли остров желание продолжать играть?

Loop:

`Puzzle -> Reward -> Island Action -> Visible Change/Discovery -> Next Puzzle`

Initial targets:

- **>= 50%** тестировщиков добровольно продолжают после первого meaningful island reward по формальному `voluntary continue` definition;
- игрок понимает, зачем нужны полученные ресурсы;
- первое заметное изменение острова происходит в первой сессии;
- открытие 77 воспринимается как reward/reveal, а не как очередной tutorial popup;
- нет ощущения, что puzzle и остров — две несвязанные игры.

## 4. Gate P2 — Vertical Slice

Вопрос: работает ли Project 77 как целостный продукт, а не как набор прототипов?

Должны быть представлены:

- финальный или близкий к финальному visual language одного сектора;
- 77;
- один законченный narrative beat;
- 20–30 минут связного опыта;
- базовый save/load;
- аналитика FTUE и level events;
- performance на reference Android devices.

Go condition: игрок понимает loop, запоминает 77/тайну и хочет узнать, что дальше.

## 5. Gate P3 — Closed Alpha

Вопрос: удерживает ли игра несколько дней и не ломается ли экономика/прогресс?

Минимальные проверки:

- onboarding funnel;
- D1 directional retention;
- early economy balance;
- save stability;
- crash/ANR;
- level difficulty distribution;
- support/bug themes.

На этом этапе retention ещё не трактуется как market truth из-за небольшого sample size.

## 6. Gate P4 — Soft Launch

Initial product targets, не гарантии рынка:

- D1: **желательно >= 30%**;
- D7: **желательно 8–12%+**;
- D30: **желательно 4–7%+**;
- crash-free sessions/users: **> 99.5%**;
- ANR-free: **> 99.5%**;
- FTUE completion: сохраняет приемлемый уровень после масштабирования аудитории;
- monetization не ухудшает retention/review sentiment заметно относительно control.

Диагностика:

- слабый D1 -> core/FTUE/value proposition;
- нормальный D1, слабый D7 -> meta/progression/content cadence;
- хороший retention, слабая монетизация -> economy/offers/value;
- хороший продукт, плохой CPI/CTR -> positioning/creative/store page;
- высокий revenue + падающий retention/sentiment -> токсичная монетизация, а не успех.

## 7. Gate P5 — Scale UA

Платный acquisition масштабируется только если одновременно:

- продукт стабилен;
- retention не деградирует при расширении аудитории;
- LTV/CAC показывает реалистичный путь к окупаемости;
- content factory выдерживает будущий LiveOps cadence;
- support/operations способны обслуживать рост.

## 8. Experiment rules

- один эксперимент должен отвечать на один основной вопрос;
- не менять одновременно несколько параметров, влияющих на одну метрику, если это ломает интерпретацию;
- заранее записывать hypothesis, primary metric, guardrail metrics, cohort, duration/volume и decision rule;
- negative result считается полезным результатом и фиксируется;
- после решения эксперимент либо становится baseline, либо полностью удаляется.

## 9. Stop-loss rule

Нельзя продолжать производство контента только потому, что уже потрачено много времени. Sunk cost не является аргументом для CONTINUE.

## 10. Prototype Phase completion rule

`28_PROTOTYPE_01_SPEC.md` является активным implementation contract.

Pre-production считается завершённым. Prototype Phase не считается завершённым только потому, что сборка технически работает. Для перехода в Vertical Slice должны быть приняты решения по обоим воротам:

- P0 — core mechanic;
- P1 — core + meta loop.

Технический DoD без желания игрока продолжить = **не CONTINUE**.

## 11. Data-over-speculation gate

После начала Prototype Phase вопросы, которые можно проверить playtest/telemetry, не должны закрываться дополнительным speculative design вместо теста.

Ориентация, difficulty curve, pacing mystery, levels-per-story-beat, фактические retention results/validated thresholds, prices и content velocity могут оставаться provisional/open до появления данных. Initial Gate P4 targets уже определены в этом документе и Decision Log и не должны дублироваться другим набором чисел.
