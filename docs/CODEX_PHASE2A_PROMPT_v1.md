# Codex Phase 2A Prompt — Gameplay Runtime Architecture Refactor

Base branch: `prototype-foundation`
Expected base HEAD when this prompt was written: `f544c3db941f35f111804ed0e857c99ec6319b99`

## Mission

Refactor the Prototype 0.4 gameplay runtime so it performs the same gameplay/data behavior with clearer ownership and fewer responsibilities inside `SandPlanetPrototype04Controller`.

This is **Phase 2A only**. It is an internal architecture refactor, not a content migration and not a rules redesign.

Read first, in this order:

1. `AGENTS.md`
2. `docs/REFACTOR_HANDOFF_v1.md`
3. `docs/REFACTOR_AUDIT_REPORT_v1.md`
4. `docs/REFACTOR_PHASE1_RESULT.md`
5. this file

## Branch and safety

- Fetch the latest remote state first.
- Create/use branch `codex/refactor-phase2a` from `origin/prototype-foundation`.
- Do not work from an older Phase 1 base.
- Do not merge into `prototype-foundation`.
- Do not run `Tools > SandPlanet > Generate Prototype 0.4`.
- Do not modify scenes, prefabs, workbook, generated CSV content, narrative content, schedules, balance, or ending rules.
- Do not start Phase 2B.

## Non-negotiable behavior preservation

All current Prototype 0.4 runtime semantics must remain identical unless explicitly listed as a structural-only change below.

Preserve exactly:

- Day/hour/Will/XP/level behavior
- affinity and State mutation behavior
- interaction availability, open/close day, time-slot, repeat rules, conditions
- NPC schedule resolution and priority
- Interaction Flow staging and commit-at-flow-end semantics
- Event Flow behavior, queueing, silent-event auto resolution, forced choices
- pending effect order and result logging
- Quest status/step behavior
- current `QuestAction` behavior
- current `ProgressEventID/OnProgressEvent` behavior
- trigger timing: GAME_START, DAY_START, DAY_END, INTERACTION, STATE_CHANGE
- current State-trigger re-entry guard semantics
- current ESC/back behavior exposed through the Phase 1 presentation facade
- all Phase 1 public/presentation behavior and golden UI

Important: `QuestSteps.csv` currently uses `ProgressEventID/OnProgressEvent` extensively in Main and Character Quest progression. **Do not remove, reinterpret, migrate, or canonicalize it in Phase 2A.** Data migration to QuestAction-only is deferred to Phase 2B.

## Architecture goal

Reduce `SandPlanetPrototype04Controller` from a monolithic gameplay runtime into a small Unity coordinator/adapter over explicit gameplay services.

Use names close to the following unless the existing code suggests a clearly better split:

### `Prototype04GameState`
Own mutable runtime state only:

- day/hour/Will/maxWill
- Personal/Social/Technical level + XP
- States
- Affinity
- Quest status + current Quest step
- occurred Events
- interaction/trigger repeat tracking

It should not render UI and should not know Unity UI components.

### `Prototype04ConditionEvaluator`
Own condition comparison/evaluation:

- STATE
- DAY
- TIME
- AFFINITY
- QUEST_STATUS
- QUEST_STEP
- EVENT_OCCURRED
- INTERACTION_DONE
- STAT_LEVEL
- AND/OR pair logic

No duplicated condition evaluator may remain active elsewhere.

### `Prototype04QuestService`
Own Quest runtime mutations and queries:

- ActivateQuest
- CompleteQuest
- FailQuest
- SetQuestStep
- current status/step queries
- **preserve current event-driven `ProgressEventID/OnProgressEvent` progression exactly**
- explicit QuestAction application exactly as before

Do not change authored quest semantics.

### `Prototype04ScheduleService`
Own character-location resolution from NPC schedule rules and conditions.

### `Prototype04InteractionService`
Own interaction discovery/availability:

- target filtering
- QuestRole OFFER/PROGRESS/NONE gating
- open/close day
- time-slot availability
- repeat rules
- authored conditions

Do not change sorting or player-visible results.

### `Prototype04EventService`
Own event/trigger runtime:

- trigger evaluation
- triggersUsed / triggerLastDay behavior
- FireEvent
- event queue
- silent event auto-resolution decision
- STATE_CHANGE trigger invocation semantics

It may coordinate with QuestService and FlowRuntime, but mutation ownership must remain explicit.

### `Prototype04FlowRuntime`
Own active Interaction/Event flow state:

- active interaction / interaction flow / event flow / node
- `flowCommitted`
- pending staged effects
- node checks
- soft stat → extra Will rule
- Execute/Stage/Finalize
- Cancel
- committed ESC linear remainder behavior
- forced event choice behavior

There must be exactly one gameplay path that applies staged effects at flow completion.

### `SandPlanetPrototype04Controller`
After migration it should primarily:

- hold serialized scene references / CSV assets
- load `SandPlanetContent04`
- construct/wire gameplay services
- coordinate Unity lifecycle (`Awake`, `Start`, End Day, hub root visibility)
- expose the stable API/events required by `Prototype04RuntimeFacade`
- own the log only if keeping log ownership here is simpler and preserves behavior

Do not move presentation ownership back into the controller.

## Phase 1 compatibility requirement

The Phase 1 presentation architecture is already approved and merged.

Keep these working without presentation rewrites:

- `Prototype04CompositionRoot`
- `Prototype04RuntimeFacade`
- `Prototype04NavigationController`
- `Prototype04LocationPresenter`
- `Prototype04NarrativePresenter`
- `Prototype04HudPresenter`
- `SandPlanetPrototype04W1QuestTracker`
- `SandPlanetPrototype04PeoplePanel`
- `SandPlanetPrototype04OpeningSetup`

Prefer keeping the existing facade API stable. If a facade method must change internally, preserve its externally observable behavior and avoid unnecessary presenter churn.

## Rules against new spaghetti

Do not solve extraction by adding patch/override layers.

Forbidden:

- private Reflection
- UI text parsing for gameplay identity/state
- multiple services mutating the same authoritative gameplay collection without a clear owner
- duplicated condition/repeat/Quest mutation implementations
- execution-order races
- `Polish`, `Finalizer`, `Override`, `Guard` style compatibility patches as the primary architecture

Adapters are allowed only where they have a clearly documented migration boundary.

## Scope explicitly deferred to Phase 2B or later

Do **not** do these now:

- QuestAction-only authoring migration
- clearing `ProgressEventID/OnProgressEvent` from workbook/CSV
- State-trigger chaining redesign
- PlayerDescription workbook/schema migration beyond what is already present
- ending evaluator design
- balance changes
- new content
- moving location target coordinates into authoring data
- player-build portrait asset delivery redesign

## Validation

Before finishing:

1. Run searches confirming no duplicate active implementations remain for:
   - condition evaluation
   - Quest mutations
   - repeat-rule evaluation
   - flow pending-effect application
2. `git diff --check`
3. Unity 6000.4.1f1 batch compile if available
4. Do not claim Play Mode success unless actually tested interactively.

## Required result document

Create `docs/REFACTOR_PHASE2A_RESULT.md` containing:

- base commit used
- changed files
- new gameplay ownership map
- what remains in `SandPlanetPrototype04Controller`
- exact compatibility guarantees preserved
- any deliberate compatibility adapters still present
- remaining gameplay debt for Phase 2B
- static/compile validation results
- a focused Unity regression checklist

The Unity regression checklist must include at minimum:

- starting stat allocation
- Day 1 startup triggers
- location/interaction availability
- one normal Interaction with staged cost/result
- one Quest offer/accept path
- one Quest progression path using `ProgressEventID/OnProgressEvent`
- one explicit QuestAction path
- State change trigger
- daily/unlimited/once repeat behavior
- NPC schedule location change
- silent Event
- narrative Event with forced choice
- End Day / Will recovery / Day-start triggers
- HUD/Quest/People/Location/Narrative still updating through Phase 1 presentation

Stop after Phase 2A. Do not begin data migration or Phase 2B.