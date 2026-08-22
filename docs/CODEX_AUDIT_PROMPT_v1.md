# Codex Prompt — SandPlanet Prototype 0.4 Code Audit v1

아래 지시를 그대로 수행한다.

You are auditing the current Unity implementation before a major cleanup/refactor.

Repository: `sciencearth2-png/SandPlanet`
Branch: `prototype-foundation`
Engine: Unity `6000.4.1f1`, URP
Active target: **Prototype 0.4 only**

## Important repository fact
Older prototype versions were intentionally removed from the repository. Do not search for them, restore them, compare against them, or propose bringing them back. Audit only the current Prototype 0.4 implementation and shared code that Prototype 0.4 actually uses.

## Before doing anything else
Read fully, in this order:
1. `AGENTS.md`
2. `docs/REFACTOR_HANDOFF_v1.md`
3. `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
4. `docs/PROTOTYPE_0_4_RUN.md`
5. `docs/DESIGN_STATE.md`
6. `docs/PROTOTYPE_SCOPE.md`
7. `docs/UNITY_ARCHITECTURE.md`

Then inspect the actual repository. Documentation may be imperfect; report conflicts rather than silently resolving them.

## This run is AUDIT ONLY
DO NOT modify gameplay/runtime/editor code, scenes, prefabs, `.meta`, Excel, or generated CSV.
Do not fix bugs.
Do not delete scripts.
Do not invoke `Tools > SandPlanet > Generate Prototype 0.4`.

The only repository file you may create/update is:
`docs/REFACTOR_AUDIT_REPORT_v1.md`

## Audit scope
Inspect all code relevant to Prototype 0.4, including at minimum:
- `Assets/SandPlanet/Runtime/Prototype/**/*.cs` that remain in the repository
- `Assets/SandPlanet/Editor/SandPlanetPrototype04Builder.cs`
- `Assets/SandPlanet/Editor/SandPlanetSpreadsheetExporter04.cs`
- `Assets/Scenes/SandPlanet_Prototype_04.unity` wiring/YAML as needed
- data loading, content definitions, runtime state, Interaction/Event/Quest/State flow
- every `RuntimeInitializeOnLoadMethod` bootstrap
- every Reflection access into `SandPlanetPrototype04Controller`

## Main concern to prove or disprove
Recent UI work may have accumulated multiple components writing the same objects at different lifecycle stages. Build a writer/readership map for at least:
- `locationPanel`: geometry, color, sibling order, active state
- `targetRoot`: anchors, placement, `anchoredPosition`, `localScale`
- character/object target cards
- interaction panel visibility/buttons
- modal/narrative panel geometry/typography/buttons
- speaker portrait visibility/geometry
- Quest tracker/detail UI
- log UI
- ESC/back input
- world click / target click routing

For every writer, report:
- file/class/method
- lifecycle (`Awake`, `Start`, `Update`, `LateUpdate`, `Canvas.willRenderCanvases`, callbacks)
- `DefaultExecutionOrder`
- whether it writes every frame or only on state change
- other writers to the same property
- likely conflict/race/maintenance risk

## Also audit
- responsibilities concentrated in `SandPlanetPrototype04Controller`
- Reflection and hidden coupling
- duplicated UI layout/style constants
- state inferred from UI text rather than explicit model state
- dynamic component injection
- code that is obsolete inside 0.4 itself
- generated scene assumptions vs current manually evolved scene
- data model consistency with workbook/CSV contract
- risk of `Generate Prototype 0.4` overwriting current UI work

## Required report structure
Write `docs/REFACTOR_AUDIT_REPORT_v1.md` with:
1. Executive summary
2. Current feature inventory
3. Runtime architecture map
4. UI/input writer map
5. Data/quest/event/state flow map
6. Confirmed spaghetti/conflict findings with evidence
7. Reflection/bootstrap/execution-order inventory
8. 0.4-only obsolete/redundant code candidates
9. What must be preserved exactly
10. Proposed replacement architecture
11. Migration plan with rollback points
12. Regression checklist
13. Risks / unknowns / questions requiring user decision

Do not implement the proposed architecture in this run. Stop after the report.
