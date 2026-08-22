# Prototype 0.4 Refactor Phase 2A Result

## Base and scope

- Base branch: `origin/prototype-foundation`
- Base commit: `6d03bb0` (`Add Codex Phase 2A gameplay runtime refactor prompt`)
- Phase: Gameplay Runtime Architecture Refactor — Phase 2A only
- `codex/refactor-phase2a` was created directly from the latest remote base.
- No merge or direct push to `prototype-foundation` was performed.
- `Tools > SandPlanet > Generate Prototype 0.4` was not run.
- No scene, prefab, workbook, generated CSV, content, schedule, balance, or ending rule was changed.

## Changed files

Added gameplay services:

- `Assets/SandPlanet/Runtime/Prototype/Prototype04GameState.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04ConditionEvaluator.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04QuestService.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04ScheduleService.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04InteractionService.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04EventService.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04FlowRuntime.cs`
- Matching Unity `.meta` files for the seven new scripts

Modified:

- `Assets/SandPlanet/Runtime/Prototype/SandPlanetPrototype04Controller.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04PresentationModels.cs`

Added documentation:

- `docs/REFACTOR_PHASE2A_RESULT.md`

## Gameplay ownership map

### `Prototype04GameState`

Sole owner of mutable runtime data:

- Day, hour, Will, max Will
- Personal/Social/Technical level and XP
- State values
- Affinity
- Quest status and current step storage
- occurred Events
- Interaction and trigger repeat tracking

It owns the single repeat-rule policy used by Interaction and Event services. It has no Unity UI dependency.

### `Prototype04ConditionEvaluator`

Sole condition implementation for:

- `STATE`
- `DAY`
- `TIME`
- `AFFINITY`
- `QUEST_STATUS`
- `QUEST_STEP`
- `EVENT_OCCURRED`
- `INTERACTION_DONE`
- `STAT_LEVEL`
- two-condition `AND` / `OR`
- numeric and string comparisons

Schedule, Interaction, Trigger, and Flow hard-condition checks all use this evaluator.

### `Prototype04QuestService`

Sole Quest mutation/query authority:

- activate, complete, fail, and set current step
- status/current-step queries
- explicit node `QuestAction` application
- existing `ProgressEventID/OnProgressEvent` progression

`FireEvent` still records the Event, runs `ProgressEventID/OnProgressEvent`, and then logs/routes the Event in the same order. No Quest authoring migration or canonicalization was performed.

### `Prototype04ScheduleService`

Owns character-location resolution from authored NPC schedules, including:

- open/close day
- Morning/Afternoon/Evening
- shared authored conditions
- existing `>=` priority behavior, so a later equal-priority matching row still wins as before

### `Prototype04InteractionService`

Owns Interaction discovery and availability:

- active target/type/ID filtering
- open/close day and time slot
- ONCE/DAILY/UNLIMITED repeat availability
- `QuestRole` OFFER/PROGRESS/NONE and legacy linked-Quest fallback
- current Quest step gate
- authored condition pair

It returns authored order; sorting remains at the same controller/presentation boundary as before.

### `Prototype04EventService`

Owns:

- trigger evaluation and priority order
- trigger repeat marking
- Event occurrence marking
- `FireEvent`
- Event queue
- silent linear Event decision
- State mutation and the existing STATE_CHANGE re-entry guard

The re-entry guard deliberately retains the current behavior: State changes produced while a STATE_CHANGE trigger pass is already running do not recursively start another pass.

### `Prototype04FlowRuntime`

Owns:

- active Interaction/Event flow, node, pending effects, and committed state
- node availability checks
- the existing TEMP soft-stat shortage → extra Will rule
- stage, execute, cancel, finalize, silent resolution, and committed ESC linear remainder
- forced Event choice protection
- the only pending-effect application implementation

Effect application order remains:

1. time and Will
2. Personal/Social/Technical XP
3. Affinity
4. State changes and STATE_CHANGE triggers
5. explicit QuestAction
6. emitted Events

At Interaction completion, repeat usage is marked and `INTERACTION` triggers run after effects, followed by result logging, as before.

## What remains in `SandPlanetPrototype04Controller`

The controller is now a Unity coordinator/adapter. It retains:

- serialized scene references and CSV assets
- `SandPlanetContent04` loading
- explicit service construction/wiring
- `Awake`, `Start`, End Day, hub root switching, and current Location/selection coordination
- the stable Phase 1 facade API and `PresentationChanged` event
- log storage/text output
- simple Day 21 message ownership
- inactive legacy presentation fallback builders used only when consolidated presentation is disabled

The controller no longer owns gameplay dictionaries, condition evaluation, repeat evaluation, Quest mutation, NPC schedule evaluation, Event queue/trigger implementation, active Flow state, or pending-effect application. Its source length changed from 1123 to 658 lines.

## Compatibility guarantees preserved

- Day/hour/Will, sleep recovery, XP rollover, level cap, and affinity clamp formulas are unchanged.
- Interaction day/time/repeat/QuestRole/QuestStep/condition gates are unchanged.
- NPC schedule filtering and priority selection are unchanged.
- Flow effects remain staged and are applied once only when the flow finishes.
- The existing pending-effect order and log timing are retained.
- `QuestAction` behavior is unchanged.
- `ProgressEventID/OnProgressEvent` remains active and is still invoked immediately from Event firing before Event presentation/resolve.
- GAME_START, DAY_START, DAY_END, INTERACTION, and STATE_CHANGE trigger timing is retained.
- State-trigger re-entry guard behavior is intentionally unchanged.
- Silent Event linear-chain detection still requires exactly one authored row per node and does not reinterpret Active flags.
- Narrative Event queueing and forced-choice ESC protection are retained.
- Uncommitted Interaction ESC cancellation and committed linear-remainder behavior are retained.
- Phase 1 facade method/property signatures used by Navigation, Location, Narrative, HUD, Quest, People, and Opening remain available.
- Phase 1 presentation files were not redesigned.

## Deliberate compatibility adapters

- `SandPlanetPrototype04Controller` remains the stable adapter consumed by `Prototype04RuntimeFacade`; presenters do not depend directly on gameplay services.
- `Prototype04RuntimeFacade.NormalizeQuestRole` keeps its existing API and delegates to the single gameplay normalization policy.
- Flow/Event services report lifecycle changes to the controller through explicit events so Unity presentation refresh and queued modal display remain coordinated.
- FlowRuntime receives a typed current-Location getter for trigger filtering; it does not inspect UI or parse display text.
- GameState exposes read-only collection interfaces to the existing facade while mutations remain routed through the owning services.
- Legacy controller button-building code remains gated behind the Phase 1 consolidated-presentation flag for compatibility; it is not a second active gameplay path.

## Remaining debt deferred to Phase 2B or later

- `ProgressEventID/OnProgressEvent` and explicit `QuestAction` intentionally remain parallel supported Quest progression paths until an approved data migration.
- State-trigger chaining/re-scan semantics remain unchanged.
- `PlayerDescription` remains the existing tracker compatibility read.
- Controller legacy presentation fallback code can be removed only after the approved Unity regression pass.
- Controller logging is still string-based rather than structured gameplay events.
- Ending evaluation, balance, stance/TEMP values, and Soft Requirement formula remain TBD.
- Location coordinates and player-build portrait delivery remain outside this phase.

## Static and compile validation

Completed:

- Confirmed branch base `origin/prototype-foundation` at `6d03bb0`.
- Confirmed only `Prototype04ConditionEvaluator` implements condition comparison/evaluation.
- Confirmed only `Prototype04GameState` implements repeat-rule evaluation.
- Confirmed all Quest mutations route through `Prototype04QuestService`; direct storage setters have no other caller.
- Confirmed only `Prototype04FlowRuntime.ApplyPendingEffects` applies staged effects.
- Confirmed active Flow/pending state exists only in `Prototype04FlowRuntime`.
- Confirmed STATE_CHANGE re-entry guard exists only in `Prototype04EventService`.
- Confirmed `ProgressEventID/OnProgressEvent` remains loaded and consumed.
- Confirmed no Reflection, UI-text identity parsing, execution-order patch, or canvas callback was added.
- `git diff --check` passed.
- Generated `Assembly-CSharp.csproj` compilation passed with zero warnings and zero errors.

Unity batch note:

- Unity `6000.4.1f1` is available, but a separate interactive Unity Editor currently has this exact worktree open, so a second batch instance correctly refused the project lock.
- The active Editor process was not terminated or manipulated.
- No interactive Play Mode success is claimed.

## Focused Unity regression checklist

Use Unity `6000.4.1f1`, open `Assets/Scenes/SandPlanet_Prototype_04.unity`, and do not regenerate the scene.

### Startup and core state

- [ ] Starting stat allocation defaults to 2/2/2, accepts any total of 6, resets all XP to 0, and delays controller Start/GAME_START until confirmation.
- [ ] Day 1 GAME_START triggers run once, followed by DAY_START triggers in the existing order.
- [ ] Initial Benjamin/Faye stance compatibility values remain STAY/UNDECIDED.
- [ ] HUD, Quest tracker, People panel, Location, and Narrative receive their first Phase 1 presentation refresh.

### Interaction and staged effects

- [ ] Enter a Location and compare the available targets/Interactions with the Phase 1 baseline.
- [ ] Verify open/close day, current time slot, QuestRole, current Quest step, and authored conditions gate the same rows.
- [ ] Run one normal Interaction with Time/Will/XP/Affinity/State results; confirm nothing applies before flow completion and every result applies once at completion.
- [ ] Cancel one uncommitted Interaction and confirm staged costs/results do not apply.
- [ ] Use ESC on a committed linear Interaction and confirm the existing remainder-stage/finish behavior.

### Quest compatibility

- [ ] Run one Quest OFFER path and confirm LOCKED → ACTIVE, initial step assignment, `? → !`, tracker, and log.
- [ ] Run one Quest progression path driven by `ProgressEventID/OnProgressEvent` and confirm its next step or completion occurs exactly once.
- [ ] Run one explicit `QuestAction` path, including SET_QUEST_STEP or COMPLETE_QUEST, and confirm identical status/step/log behavior.
- [ ] Confirm a linked PROGRESS Interaction disappears when its authored current-step gate no longer matches.

### Event, State, and repeat behavior

- [ ] Apply a State-changing result that invokes a STATE_CHANGE trigger and confirm the Event timing and result.
- [ ] Confirm nested State changes retain the existing re-entry-guard behavior; do not expect a new chain re-scan.
- [ ] Verify ONCE Interaction/Trigger cannot repeat.
- [ ] Verify DAILY becomes available on the next Day but not twice on the same Day.
- [ ] Verify UNLIMITED remains repeatable.
- [ ] Trigger one SILENT linear Event and confirm effects/log/Quest progression without Narrative UI.
- [ ] Trigger one narrative Event with multiple choices and confirm queueing and that ESC cannot bypass the forced choice.

### Schedule and day transition

- [ ] Advance to an authored NPC schedule boundary and confirm character Location changes with the same priority result.
- [ ] End Day and confirm DAY_END triggers run before increment, Day increments once, hour returns to 08:00, and Will recovers by +2 with max clamp.
- [ ] Confirm DAY_START triggers run after the new Day state and Day 15 hub switching remains correct.

### Phase 1 presentation regression

- [ ] Location browse 1.5x, target focus 2.0x, focus pan, clipping, and Interaction→Narrative map continuity remain unchanged.
- [ ] HUD values and progress bars update after normal, silent, Quest, State, and End Day changes.
- [ ] Quest tracker/hover/checklist and People panel reflect new runtime state without polling or Reflection regressions.
- [ ] Interaction/Narrative text, Quest metadata, QuestAction chips, portraits, and forced-choice UI remain unchanged.
- [ ] ESC still consumes one topmost Navigation layer per press.

Stop after this checklist. Do not begin Quest data migration or Phase 2B.
