# Project 77 — Release & Live Checklist

## Product

- [ ] Core mechanic validated externally.
- [ ] FTUE understandable without long text.
- [ ] First-session mystery hook works.
- [ ] Island visibly changes early.
- [ ] 77 has final visual identity.
- [ ] No major system introduced without purpose.

## Content

- [ ] Launch level pack validated by telemetry/bot solver/manual QA.
- [ ] No impossible levels.
- [ ] Narrative continuity checked.
- [ ] Localization placeholders eliminated.
- [ ] Spoiler tiers respected in store assets.

## Economy

- [ ] Faucets/sinks documented.
- [ ] Remote-configurable prices/rewards where appropriate.
- [ ] Free player can progress main story.
- [ ] Starter offer not shown too early.
- [ ] Purchase restoration tested.

## Ads

- [ ] Reward granted idempotently.
- [ ] No ad available fallback.
- [ ] Frequency cap.
- [ ] Child/privacy/consent requirements reviewed for target markets.
- [ ] No ad interrupting reveal/cutscene.

## Stores

- [ ] Product IDs mapped per provider.
- [ ] Test purchases completed.
- [ ] Subscription lifecycle tested.
- [ ] Refund/revocation handled.
- [ ] Restore purchases works.
- [ ] RuStore build uses current Pay SDK, not deprecated BillingClient.
- [ ] Google Play build follows current Play Billing policy.

## Save

- [ ] Fresh install.
- [ ] Reinstall restore.
- [ ] Device change.
- [ ] Offline progress.
- [ ] Conflict resolution.
- [ ] Save schema migration.
- [ ] Purchased entitlement recovery.

## Stability

- [ ] Crash-free target monitored.
- [ ] ANR monitoring.
- [ ] Low-memory tests.
- [ ] Background/foreground tests.
- [ ] Network transition Wi-Fi/mobile/offline.
- [ ] Thermal/battery observation.

## Analytics

- [ ] Event schema versioned.
- [ ] FTUE funnel dashboard.
- [ ] Level health dashboard.
- [ ] Economy dashboard.
- [ ] Purchase funnel.
- [ ] Ad funnel.
- [ ] Retention cohorts.

## LiveOps

- [ ] Remote event kill switch.
- [ ] Inbox emergency gift.
- [ ] Config rollback.
- [ ] Season start/end tested.
- [ ] Catch-up logic tested.
- [ ] Support runbook.

## Community

- [ ] Spoiler policy.
- [ ] Support channel.
- [ ] Known issues page/process.
- [ ] Community event moderation plan.

## Post-release weekly review

- retention;
- crash/ANR;
- level outliers;
- economy inflation/starvation;
- ad opt-in and complaints;
- payment failure rate;
- reviews/support themes;
- content production throughput;
- LiveOps participation;
- next experiment decision.


## Production readiness

- [ ] Product gate for current stage has an explicit decision.
- [ ] Project 77 is still treated as codename unless commercial naming clearance completed.
- [ ] Target audience/store declarations match actual design; not accidentally child-directed.
- [ ] No free-form UGC/chat ships without dedicated moderation/compliance milestone.
- [ ] targetSdk meets current store requirement (baseline on 02.09.2026: Google Play API 36+).
- [ ] 16 KB page-size compatibility validated for all 64-bit/native dependencies.
- [ ] Performance budgets measured on low/mid/high reference devices.
- [ ] Remote Config known-good defaults, version and rollback tested.
- [ ] Critical feature/event/purchase kill switches tested.
- [ ] Incident playbooks and emergency grant/recovery access verified.
- [ ] Content velocity supports announced LiveOps cadence.
- [ ] User-facing strings are localization-key based.
- [ ] Accessibility baseline reviewed.
