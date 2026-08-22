# Codex Post-Refactor Audit Prompt v1

Base branch: `prototype-foundation`
Expected base HEAD when written: `0092ed45a9b08048dd668868719804c0e3e9bb19`

## Mission

Perform a second full architecture audit of the current SandPlanet Prototype 0.4 **after** the Phase 1 presentation/navigation refactor and Phase 2A gameplay-runtime refactor.

This task is **AUDIT ONLY**. Do not refactor, clean up, migrate data, redesign gameplay, edit content, or change runtime behavior.

The user has completed a full Day 1–21 manual playthrough of the Phase 2A worktree with no runtime errors. Treat that as the current behavioral baseline.

## Read first

Read these files completely before inspecting code:

1. `AGENTS.md`
2. `docs/REFACTOR_HANDOFF_v1.md`
3. `docs/REFACTOR_AUDIT_REPORT_v1.md`
4. `docs/REFACTOR_PHASE1_RESULT.md`
5. `docs/REFACTOR_PHASE2A_RESULT.md`
6. this prompt

## Scope

Audit the current Prototype 0.4 codebase, especially:

- `Assets/SandPlanet/Runtime/Prototype/`
- `Assets/SandPlanet/Editor/`
- current scene/bootstrap/composition wiring
- data loading and presentation facade boundaries

Do not inspect or restore Prototype 0.3 or older code. It is intentionally removed.

## Questions to answer

### 1. Remaining spaghetti / multiple authority

Find every place where more than one class can still mutate, derive, or present the same conceptual state.

Examples to inspect:

- duplicated UI fallback paths still living in `SandPlanetPrototype04Controller`
- duplicate Quest-role/type formatting logic
- duplicate modal/flow state representations
- multiple log or presentation refresh pathways
- duplicate content queries or sorting policies
- runtime state that can be mutated outside its intended owner
- direct public setters or mutable collections that bypass services

For each finding, identify the exact owner(s), property/state affected, and whether it is active in the approved Phase 1+2A path or only compatibility debt.

### 2. Hidden temporal coupling

Find code whose correctness depends on callback order, Unity lifecycle order, event subscription order, or synchronous re-entrancy.

Inspect especially:

- `PresentationChanged`
- FlowRuntime `Changed / Completed / Cancelled`
- EventService `SilentEventResolved`
- Event queue behavior
- STATE_CHANGE triggers
- `ShowQueuedEvent`
- startup order with OpeningSetup and controller enable/Start
- composition-root initialization

Classify each as safe/explicit, fragile-but-currently-correct, or a real risk.

### 3. Ownership boundaries after Phase 2A

Verify whether the intended owners are real single authorities:

- `Prototype04GameState`
- `Prototype04ConditionEvaluator`
- `Prototype04QuestService`
- `Prototype04InteractionService`
- `Prototype04ScheduleService`
- `Prototype04EventService`
- `Prototype04FlowRuntime`
- `Prototype04NavigationController`
- `Prototype04LocationPresenter`
- `Prototype04NarrativePresenter`

For each, state what it owns, what it still reaches into, and whether any dependency direction should be reversed or isolated later.

### 4. Controller debt

Audit `SandPlanetPrototype04Controller` in detail.

Separate its remaining code into:

- legitimate Unity coordinator responsibilities
- stable facade/adaptation responsibilities
- inactive legacy presentation fallback
- gameplay policy that should eventually move out
- formatting/helper code that belongs elsewhere

Do not delete anything in this audit.

### 5. Data-contract debt

Audit current runtime/data contracts without changing them.

Focus on:

- `QuestAction` versus `ProgressEventID/OnProgressEvent`
- `PlayerDescription` being read outside the core Quest model
- stringly-typed IDs, enum-like strings, condition operators, QuestRole, EntryMode, trigger timing, PresentationMode
- State value typing/normalization
- authored row ordering and implicit priority behavior

For each item, distinguish:

- intentional current compatibility
- actual duplication/problem
- safe future migration candidate
- migration that would require workbook/CSV changes

### 6. Unity / asset / editor risks

Check for:

- generated or IDE files accidentally tracked
- destructive editor tools
- `.meta` / GUID risks
- runtime dependence on `UnityEditor` APIs or editor-only portrait loading
- scene references that would break player builds
- runtime-created UI that is difficult to serialize/test but not necessarily wrong

Do not run `Tools > SandPlanet > Generate Prototype 0.4`.

### 7. Testability

Identify which gameplay services are now unit-testable without Unity and which still depend on Unity/MonoBehaviour/static globals.

Recommend a minimal test seam plan, but do not add tests yet.

### 8. Future-change risk

The user wants to resume design/content changes after this audit. Identify the top areas where future feature work would most likely recreate spaghetti if not governed by a clear rule.

Give concrete guardrails such as:

- "only X may mutate Y"
- "new UI state must enter through Z"
- "do not add patch MonoBehaviours"
- "do not parse display text for identity"

## Required output

Create only:

`docs/POST_REFACTOR_AUDIT_REPORT_v1.md`

Do not modify gameplay code, scene, prefab, workbook, CSV, `.meta`, or existing documentation.

The report must contain:

1. Executive summary
2. Current architecture map
3. Confirmed improvements from Phase 1/2A
4. Remaining spaghetti findings ranked Critical / High / Medium / Low
5. Hidden temporal-coupling findings
6. Controller debt map
7. Data-contract debt map
8. Unity/editor/build risks
9. Testability assessment
10. Recommended next steps in priority order
11. Explicit recommendation on whether Phase 2B should happen now, later, or not at all
12. A short "rules for future SandPlanet changes" section

Every finding must cite concrete files/classes/methods. Avoid generic refactoring advice.

## Stop condition

After writing the report, stop. Do not start Phase 2B or any cleanup implementation.