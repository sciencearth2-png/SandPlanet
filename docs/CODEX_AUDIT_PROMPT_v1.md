# Codex Prompt — SandPlanet Prototype 0.4 Code Audit v1

아래 블록을 Codex 작업 프롬프트로 그대로 사용한다.

---

You are auditing a Unity project before a major cleanup/refactor.

Repository: `sciencearth2-png/SandPlanet`
Branch: `prototype-foundation`
Engine: Unity `6000.4.1f1`, URP
Current active target: Prototype 0.4

## Your task in this run

Perform a **complete codebase audit of the current Prototype 0.4 implementation** so that we can later replace/refactor the code while preserving the same game behavior.

This is an **AUDIT-ONLY run**.

### Before doing anything else

Read these files fully, in this order:

1. `AGENTS.md`
2. `docs/REFACTOR_HANDOFF_v1.md`
3. `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
4. `docs/PROTOTYPE_0_4_RUN.md`
5. `docs/DESIGN_STATE.md`
6. `docs/PROTOTYPE_SCOPE.md`
7. `docs/UNITY_ARCHITECTURE.md`

Then inspect the actual repository. Do not assume the documentation is perfectly up to date. If docs and code conflict, report the conflict rather than silently choosing one.

## Strict modification rule

DO NOT modify gameplay code, runtime code, editor code, scenes, prefabs, `.meta` files, Excel files, or generated CSV files in this run.

Do not fix bugs yet.
Do not delete obsolete-looking scripts yet.
Do not run or invoke `Tools > SandPlanet > Generate Prototype 0.4`.
Do not redesign the authoring/data model yet.

The only repository file you are allowed to create or update in this run is:

`docs/REFACTOR_AUDIT_REPORT_v1.md`

If your environment cannot safely write that report, return the full report in your final response instead.

## What to audit

Audit all relevant code, not only recently edited files.

At minimum inspect:

- `Assets/SandPlanet/Runtime/Prototype/**/*.cs`
- `Assets/SandPlanet/Editor/**/*.cs`
- Prototype 0.4 scene/build/export wiring
- data parser/content/runtime state connections
- files referenced by Prototype04 scene or runtime bootstrap mechanisms
- old Prototype02/03 code enough to determine whether it is truly legacy or still participates in Prototype04

Use repository search, references, scene text/YAML inspection, git history/diff, and static reasoning as needed.

## The core concern

Recent UI work appears to have accumulated multiple patch/polish/override components that can write to the same UI objects and state at different lifecycle stages.

Do not simply label this “spaghetti code.” Prove or disprove it.

Specifically find every writer/reader for shared state and UI properties such as:

- `locationPanel` geometry / sibling order / color / visibility
- `targetRoot` anchors / `anchoredPosition` / `localScale`
- character/object target placement
- interaction panel visibility and dynamic buttons
- modal/narrative panel geometry and typography
- character portrait visibility/position
- quest tracker / quest detail UI
- ESC/back input
- target click / world click routing

Pay special attention to:

- `[RuntimeInitializeOnLoadMethod]`
- `[DefaultExecutionOrder]`
- `Awake`, `Start`, `Update`, `LateUpdate`
- `Canvas.willRenderCanvases`
- reflection into private fields/methods
- components disabling/re-enabling each other
- per-frame geometry reassertion
- multiple scripts inferring the same presentation state indirectly

## Behavior that a later refactor must preserve

Treat `docs/REFACTOR_HANDOFF_v1.md` as the latest UX/design contract.

Important examples include, but are not limited to:

- Excel/CSV-driven Interaction/Event/Quest/State architecture
- explicit QuestAction-based quest mutation
- opening stat allocation
- W1 six-person reunion checklist
- People panel
- quest hover description
- natural action text in interaction entries, with quest info only as secondary metadata
- lower-left system result log without redundant narrative State prose
- opaque location viewport above Quest/Log overlays
- location title at viewport upper-left
- characters/objects distributed naturally inside a clipped map viewport
- location-specific muted dark backgrounds
- base map browse scale 1.5x
- target focus 2.0x with light pan/zoom
- no unexpected rearrangement/scale jump when interaction selection becomes dialogue
- right-side Interaction/Narrative UI
- larger character portrait for character interactions/dialogue
- first ESC closes right interaction selection only; later ESC may close the location

Do not assume the current code actually satisfies all of these. Report mismatches.

## Required report structure

Create `docs/REFACTOR_AUDIT_REPORT_v1.md` with the following sections.

### 1. Executive summary
- Is the current problem primarily architecture debt, local bugs, or both?
- Top 5 risks.
- What should NOT be rewritten.

### 2. File inventory and status
For every relevant C# file:
- path
- responsibility
- active / legacy / uncertain
- key dependencies
- whether it writes shared UI/runtime state

Separate Runtime Prototype, Editor/Exporter, legacy Prototype02/03, and support files.

### 3. Functional ownership map
Map each feature to its actual implementation owner(s):
- time/day
- Will
- XP/Level
- condition evaluation
- Interaction
- Event/Trigger
- Quest/QuestStep/QuestAction
- State
- NPC schedule
- Day15 hub switch
- opening setup
- People panel
- quest tracker/detail/checklist
- result log
- location UI
- target placement
- interaction UI
- narrative/event dialogue UI
- portraits
- zoom/pan
- input/ESC

Highlight features with more than one effective owner.

### 4. Data/runtime flow
Document actual flow, including key methods/classes:

`Master.xlsx -> exporter -> CSV -> parser/content -> runtime state -> target click -> interaction/event flow -> result application -> UI refresh`

### 5. Shared-writer conflict matrix
Create a table for important shared fields/properties.

Columns should include:
- object/property
- all writer files/methods
- lifecycle/order
- intended owner
- conflict risk
- observed/likely symptom

### 6. Lifecycle / execution order graph
List all relevant bootstrap and update paths:
- RuntimeInitializeOnLoadMethod
- DefaultExecutionOrder
- Awake/Start/Update/LateUpdate
- Canvas.willRenderCanvases

Explain which code wins last for major UI properties.

### 7. Reflection/private coupling
List every meaningful reflection dependency into controllers/private members and categorize:
- harmless temporary bridge
- medium debt
- high-risk hidden coupling

### 8. Legacy/dead/superseded candidates
Do NOT delete anything.
For each candidate provide evidence and removal risk.

### 9. Intended behavior vs current implementation
Compare the latest contract in `REFACTOR_HANDOFF_v1.md` with actual code.
List confirmed matches, mismatches, and uncertain items.

### 10. Refactor boundary recommendation
State clearly which layers should be preserved and which should be replaced.
Prefer preserving stable data/authoring logic if evidence supports it.

### 11. Target architecture proposals
Provide:
- **Recommended architecture A**
- **Conservative alternative B**

For each, specify concrete class/file responsibilities and how state transitions would work.

Strongly consider:
- one authoritative location viewport/presentation owner
- one interaction/narrative presentation coordinator
- one back/ESC router
- one quest UI presenter
- event-driven UI refresh instead of multiple per-frame overrides
- explicit APIs instead of reflection where practical

But do not force these ideas if the audit shows a better structure.

### 12. Migration plan
Break the future refactor into small reversible stages.
For each stage include:
- files affected
- behavior preserved
- regression tests
- rollback point

Do NOT execute the migration in this run.

### 13. Test matrix
Give a manual Unity smoke-test checklist covering at least:
- opening stat setup
- Day1 wake-up
- location enter/exit
- target click
- interaction selection
- ESC hierarchy
- interaction -> dialogue transition
- event dialogue
- quest offer/progress/complete
- quest hover/checklist
- result log
- portrait behavior
- 1.5x browse / 2.0x focus / clip boundary
- Day15 hub switch

### 14. Estimated refactor effort
Estimate by stage and identify the riskiest stage.

### 15. Questions for the user
Only include questions that genuinely cannot be answered from code or the supplied design docs.
Do not block the audit on these questions.

## Quality bar

Be evidence-driven.
Use exact file paths, classes, methods, fields, and execution order where possible.
Distinguish:
- confirmed by code
- documented intent
- inference
- unknown / requires Unity play test

The user is not asking for a generic code review. The goal is to make the next refactor safe enough that we can remove the accumulated patch layers without changing the actual game experience.

When finished, summarize the three most important findings and point to `docs/REFACTOR_AUDIT_REPORT_v1.md`.

---
