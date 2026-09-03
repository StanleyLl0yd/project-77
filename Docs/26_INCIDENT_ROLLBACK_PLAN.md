# Project 77 — Incident, Rollback & Recovery Plan

## 1. Goal

Авария не должна впервые обсуждаться в момент аварии. Для каждого критического класса заранее существует owner, containment action, rollback/recovery path и player communication rule.

## 2. Severity

### SEV-0

- массовая потеря/порча save;
- массово потерянные покупки/entitlements;
- security/privacy breach;
- economy exploit, угрожающий целостности live economy;
- приложение не запускается у значимой доли игроков.

### SEV-1

- store/purchase flow массово сломан без потери уже выданного;
- LiveOps event выдаёт неверные rewards;
- major progression blocker;
- backend outage значимой длительности.

### SEV-2

- отдельная feature degradation;
- cosmetic/content issue;
- non-critical analytics/notification failure.

## 3. Mandatory recovery capabilities

До soft launch:

- Remote Config rollback;
- per-feature kill switches;
- event disable;
- store/offer disable;
- inbox compensation grant;
- cloud read-only/recovery mode where feasible;
- previous known-good client build retained;
- previous known-good content bundle retained;
- audit log of config/admin changes.

## 4. Scenario playbooks

### Backend unavailable

- core offline gameplay remains usable where safe;
- queue safe actions;
- no fake confirmation of server-authoritative rewards;
- clear retry/status behavior;
- reconcile after recovery.

### Bad Remote Config

- stop rollout/rollback config;
- client fallback to known-good defaults where config invalid;
- log config version;
- disable affected feature.

### Wrong massive reward grant

- freeze source of further grants;
- quantify affected accounts;
- prefer fair correction/compensation over destructive blanket rollback;
- never silently remove legitimately purchased currency/items.

### Purchase charged, reward not visible

- transaction remains recoverable;
- retry validation/grant idempotently;
- support can lookup transaction by Player ID/provider transaction identifier;
- do not require player to purchase again.

### Duplicate reward

- idempotency prevents ordinary duplicate callbacks;
- if systemic exploit occurred, remediation follows documented economy incident policy.

### Save migration bug

- disable/limit writes if necessary;
- preserve old snapshot;
- rollback client/content if compatible;
- ship migration fix;
- never overwrite all good backups with bad migrated state.

### Event starts/ends incorrectly

- disable event;
- fix config;
- preserve earned valid progress where possible;
- compensation if player opportunity was lost.

## 5. Release rollback limitation

Store binaries нельзя считать мгновенно откатываемыми. Поэтому remote kill switches, backward-compatible backend и config rollback обязательны: они дают containment до нового store review/update.

## 6. Communication

Для SEV-0/1:

- known issue message/status channel;
- concise factual description;
- workaround only if safe;
- compensation only after scope understood;
- post-incident note internally.

## 7. Postmortem

После SEV-0/1 документировать:

- timeline;
- root cause;
- impact;
- detection gap;
- containment;
- recovery;
- why safeguards failed;
- concrete preventive actions with owners.

Blameless не означает безответственность: цель — изменить систему, а не найти удобного виноватого.
