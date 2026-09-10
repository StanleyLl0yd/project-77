# Project 77 — Build 11 Device Finding

Status: **P1 ITERATION INPUT — BUILD 11 NOT APPROVED FOR FRESH-TESTER BATCH**.

Physical-device review of Unity Build #11 (`357356a9fdf34052cc666bf6ac16abec9e419848`) on the preregistered Joy 4-class portrait device found a first-use comprehension blocker before P77-071.

Observed problems:

- the normal launch setup exposed legacy P0 A/B/C debug choices, allowing a tester to leave the intended selected-meta P1 path;
- Energy Routing endpoints relied too heavily on subtle color/background tint and one-letter markers;
- blocked cells were represented primarily by darker tint and were easy to miss on the real dark display;
- the required touch-and-drag gesture was not taught explicitly enough for a fresh player;
- accidentally entering Path / Expedition exposed implementation-oriented `RS/RG/BS/BG` markers that are not intended as P1 player-facing onboarding.

Decision:

- keep Build #11 and its artifact/freeze as historical technical evidence;
- do **not** use Build #11 for the protocol-grade P1 fresh-tester batch;
- iterate the P1 first-use presentation before starting P77-071;
- require a new Android build and a new artifact-bound freeze after the fix.

The deterministic Energy Routing rule implementation is not rejected by this finding. The problem is presentation/onboarding and external-build entry-point hygiene.
