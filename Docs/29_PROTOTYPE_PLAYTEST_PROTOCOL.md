# Project 77 — Prototype Playtest Protocol

Status: **ACTIVE operational protocol for Gate P0/P1**.

Purpose: make Prototype 0.1 playtests reproducible enough that P0/P1 decisions are based on observable behavior rather than moderator interpretation.

Authoritative gate thresholds live in `18_PRODUCT_GATES.md`. This document defines **how** sessions are run and reported.

## 1. Tester eligibility

### Fresh tester

Preferred for comprehension and first-use measurements:

- external to implementation of the mechanic;
- did not design the tested variant;
- does not know the rules in advance;
- has not read detailed GDD/core/prototype specs;
- has not previously played that variant/revision.

### Returning tester

May be used for regression/iteration tests, but must be marked as returning. Do not mix returning testers into fresh-comprehension rates without a separate cohort label.

### Comparative variant sessions

If one person tests multiple variants, record the order. Randomize/counterbalance order where practical. Only the first exposure to a mechanic is a fresh-comprehension exposure.

For the initial P0 comparison, aim for at least **10 fresh exposures per variant** across one or more batches before CONTINUE, unless a variant is stopped early because a severe, repeated usability failure makes further exposure wasteful. Small samples are directional evidence, not statistical proof.

## 2. Required test-environment record

Every session/batch records:

- `build_version` / commit SHA if available;
- device model or emulator profile;
- OS/API version;
- screen orientation;
- prototype variant;
- level sequence and level revisions;
- telemetry/event-schema version;
- tester cohort: fresh or returning;
- test date/time;
- moderator identifier/initials if a moderator is present.

Do not record unnecessary personal data.

## 3. Moderator rules

The moderator observes first and teaches only according to the build's intended tutorial.

### Allowed before play

A neutral introduction such as:

> "Please play this prototype as you normally would. I am testing the game, not you. Say anything you naturally notice; I may stay quiet while you play."

The moderator may explain device/test logistics that are unrelated to the game rule.

### Not allowed before the intended help point

Do not say or imply:

- what the puzzle rule is;
- which object to touch;
- the correct first gesture;
- the next move;
- why a reward matters;
- where to spend a resource;
- "try the next level" / "press continue" / equivalent prompting.

Do not react in a way that confirms or rejects a move before the game itself does.

### When help is allowed

Help may be given only when:

- the test plan explicitly defines a help threshold; or
- the tester is blocked long enough that continuing without help no longer provides useful evidence.

Default blocked threshold: **30 seconds with no meaningful progress after the tester has clearly attempted interaction**. A batch plan may pre-register a different threshold for a specific experiment.

Any help must be recorded with:

- timestamp/step;
- what was said/done;
- reason;
- whether the affected metric is excluded from unaided-comprehension calculations.

## 4. Core observations

Record at minimum:

- time from first interactive frame to first gesture;
- first gesture type/target;
- wrong or invalid gestures;
- time to demonstrate understanding of the rule;
- tutorial/help exposure;
- help required yes/no;
- level starts/completions/fails;
- retries;
- quits;
- median/representative level time;
- visible hesitation;
- frustration signals;
- voluntary next-puzzle action;
- voluntary quit;
- spontaneous tester comments verbatim or near-verbatim where useful.

Do not convert every facial expression into a conclusion. Separate observation from interpretation.

## 5. Meta observations

After the first island reward/action, record:

- did the tester understand what resource was received;
- did they understand `puzzle -> reward`;
- did they understand what the repair action would do;
- did they notice the visible world change;
- did they understand `reward -> repair -> world change`;
- did discovery feel like a reward rather than tutorial overhead;
- did they notice/remember robot 77;
- did the island increase, decrease, or not change desire to continue;
- did puzzle and island feel like one loop or two unrelated games.

## 6. Formal definition: voluntary continuation

A `voluntary continue` is counted when all are true:

1. the build has clearly offered the next intended puzzle/action;
2. the tester receives no direct or indirect moderator prompt to continue;
3. the tester initiates the next puzzle/action on their own;
4. the action occurs within the pre-registered observation window.

Default observation window: **15 seconds after the continuation affordance becomes available**. A different window is valid only if recorded before the batch because the UX requires it.

Do **not** count continuation if the moderator says or implies:

- "go to the next one";
- "press here";
- "let's see what happens next";
- "continue";
- equivalent wording/gestures.

If the UI auto-starts the next puzzle, this metric is invalid for that build; use a build with a genuine player choice for P0/P1 voluntary-continuation measurement.

## 7. Session questionnaire

Ask only after the observation phase unless the experiment explicitly needs an earlier question.

Standard questions:

1. How would you explain the game rule to another person?
2. What was the most pleasant/interesting part?
3. What was unclear?
4. What was annoying or frustrating?
5. What were the resources for?
6. What changed on the island because of what you did?
7. What did you expect to happen next?
8. Did you want to keep playing? Why or why not?
9. Would you choose to open a game like this again tomorrow? Why?

Avoid leading questions such as "Did you like the robot?" or "Was the puzzle easy to understand?" Ask open questions first.

## 8. Batch report template

Every playtest batch ends with a report using this structure:

```text
Batch ID:
Build / commit:
Event schema version:
Hypothesis:
Prototype variant(s):
Sample:
Fresh / returning split:
Device/API/orientation coverage:
Level set + revisions:
Exclusions and why:

Primary metrics:
- unaided comprehension:
- voluntary continuation:
- completion/fail/retry/quit:
- median level duration:
- help-required rate:

Meta metrics (P1 if applicable):
- resource comprehension:
- puzzle->reward comprehension:
- repair/world-change comprehension:
- post-island voluntary continuation:

Qualitative findings:
Major confusion points:
Repeated spontaneous comments:
Observed frustration/hesitation patterns:

Decision: CONTINUE / ITERATE / PIVOT / STOP
Reason against pre-registered gate:
Next experiment:
Owner:
```

## 9. Decision hygiene

- Do not change a gate threshold after seeing the result merely to obtain CONTINUE.
- Separate instrumentation failure from product failure.
- Record excluded sessions and reasons.
- A strong anecdote does not override repeated behavioral evidence.
- A small sample can justify ITERATE/PIVOT, but not a claim of market-wide retention.
- Keep raw session notes/reports anonymous and minimal.
