# Prototype 0.4 Refactor Phase 1 Result

## Scope and baseline

- Baseline: `prototype-foundation` / `7d1b72e70393fdc188ce39e79b36a52588541fbf`
- Phase: Presentation + Navigation Authority Consolidation
- Golden baseline: the approved Prototype 0.4 UI and play behavior described in `REFACTOR_HANDOFF_v1.md` and `REFACTOR_AUDIT_REPORT_v1.md`
- Not changed: workbook/CSV content, scenes, prefabs, gameplay/data semantics, `ProgressEventID/OnProgressEvent`, `QuestAction`, conditions, time/Will/XP/Affinity, NPC schedules, narrative content, balance, and ending rules
- `Tools > SandPlanet > Generate Prototype 0.4` was not run.

## New architecture

### Composition root

`Prototype04CompositionRoot` is the only runtime bootstrap for the Phase 1 presentation path. It locates the existing Prototype 0.4 controller once, creates a typed `Prototype04RuntimeFacade`, constructs the navigation/presenter components, and connects their dependencies explicitly.

The controller remains the gameplay/data runtime. Its new public surface is a read-only state/view-reference facade plus explicit commands and events needed by the presentation layer. The new path does not use private Reflection.

### Navigation authority

`Prototype04NavigationController` owns the explicit UI modes:

- `Hub`
- `LocationBrowse`
- `TargetSelected`
- `Narrative`
- `ForcedChoice`
- `People`
- `QuestDetail`

All ESC and Back-button input converges on `Back()`. It consumes only the topmost layer. It does not disable another `MonoBehaviour` to suppress duplicate input. Forced Event choices do not close on ESC. Empty Location viewport clicks do not close the location; only ESC/Back returns to the hub.

### Location presentation authority

`Prototype04LocationPresenter` is the sole active owner of:

- Location viewport visibility, opaque per-location background, title layout, and clipping
- Typed target creation using explicit target type/ID bindings
- Character/world-target presentation and known-ID placement
- Quest markers on location targets
- `targetRoot` pan and scale
- browse `1.5x`, focus `2.0x`, and focus pan

No target identity is reconstructed from UI text. `Prototype04TargetBinding` carries the target type and ID directly. No other active Phase 1 component writes `targetRoot.anchoredPosition` or `targetRoot.localScale`.

### Narrative presentation authority

`Prototype04NarrativePresenter` is the sole active owner of:

- The right-side Interaction and Narrative panels
- Interaction and choice button construction, layout, and typography
- Natural interaction text with secondary Quest metadata
- Narrative body/cost/disabled-reason presentation
- QuestAction result chips without changing QuestAction semantics
- Character portraits for selection and dialogue
- Interaction/Narrative panel switching

The selected target remains explicit runtime state while a narrative opens, so the left map composition retains its focus and transform through the Interaction-to-Narrative transition.

### Other typed presentation adapters

- `Prototype04HudPresenter` owns the HUD, progress bars, context card, and their presentation values.
- `SandPlanetPrototype04W1QuestTracker` now consumes typed quest/state APIs and change events while preserving the tracker, hover detail, and reunion checklist.
- `SandPlanetPrototype04PeoplePanel` now consumes typed affinity/state/event APIs and delegates close behavior to Navigation.
- `SandPlanetPrototype04OpeningSetup` now uses an explicit starting-stat command instead of private field mutation through Reflection.
- Log text remains written only by the gameplay controller.

## Removed or reduced legacy patches

Removed after their required responsibilities were transferred:

- `SandPlanetPrototype04EscapeLayerGuard`
- `SandPlanetPrototype04InputBridge`
- `SandPlanetPrototype04MapViewportPolish`
- `SandPlanetPrototype04QuestActionBadge`
- `SandPlanetPrototype04QuestOfferLabels`
- `SandPlanetPrototype04SceneDensityOverride`
- `SandPlanetPrototype04SceneDialogueLayout`
- `SandPlanetPrototype04SceneInteractionFinalizer`
- `SandPlanetPrototype04SceneInteractionPolish`
- `SandPlanetPrototype04UxEnhancer`

Reduced/migrated rather than deleted because they still own required features:

- `SandPlanetPrototype04OpeningSetup`
- `SandPlanetPrototype04PeoplePanel`
- `SandPlanetPrototype04W1QuestTracker`

The gameplay controller retains its legacy fallback button-building code for compatibility, but the consolidated runtime path gates those writers off. The new presenters replace their responsibilities before legacy objects are cleared or hidden.

## Remaining legacy debt

- `SandPlanetPrototype04Controller` still contains inactive compatibility presentation helpers and serialized UI references. Removing that fallback is a later cleanup after Unity regression approval, not a gameplay/data migration.
- The controller facade is intentionally broad for Phase 1. A later boundary cleanup may split immutable runtime snapshots from commands without changing semantics.
- Known location target slots remain code-authored by typed Location/Target IDs. Moving them to authoring data was not part of this phase.
- Portrait loading retains the existing Editor asset-loading approach. Player-build asset delivery was not changed or inferred in this phase.
- `PlayerDescription` remains the existing Quest CSV compatibility read in the Week 1 tracker, as explicitly deferred to the Gameplay/Data phase.
- Quest mutation canonicalization and State trigger chaining were not changed.

## Changed files

Added:

- `Assets/SandPlanet/Runtime/Prototype/Prototype04CompositionRoot.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04PresentationModels.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04NavigationController.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04LocationPresenter.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04NarrativePresenter.cs`
- `Assets/SandPlanet/Runtime/Prototype/Prototype04HudPresenter.cs`
- Matching Unity `.meta` files for the six new scripts
- `docs/REFACTOR_PHASE1_RESULT.md`

Modified:

- `Assets/SandPlanet/Runtime/Prototype/SandPlanetPrototype04Controller.cs`
- `Assets/SandPlanet/Runtime/Prototype/SandPlanetPrototype04OpeningSetup.cs`
- `Assets/SandPlanet/Runtime/Prototype/SandPlanetPrototype04PeoplePanel.cs`
- `Assets/SandPlanet/Runtime/Prototype/SandPlanetPrototype04W1QuestTracker.cs`

Deleted:

- The ten legacy patch scripts listed under “Removed or reduced legacy patches,” plus their `.meta` files

No scene, prefab, workbook, or generated CSV content was changed.

## Static and compile validation

Completed:

- Confirmed `HEAD`, `prototype-foundation`, and `origin/prototype-foundation` all point to `7d1b72e`.
- Searched the remaining Prototype runtime scripts: no `System.Reflection`, private `GetField`/`GetMethod`, or `Canvas.willRenderCanvases` path remains.
- Confirmed ESC polling exists only in `Prototype04NavigationController`.
- Confirmed `targetRoot.anchoredPosition` and `targetRoot.localScale` writes exist only in `Prototype04LocationPresenter`.
- Confirmed no scene/prefab/workbook/CSV content diff remains.
- `git diff --check` reports no whitespace errors.
- Unity `6000.4.1f1` batch script compilation completed with `ExitCode: 0`, `Tundra build success`, no C# errors or warnings, and batch return code 0.

Not claimed:

- No Play Mode or visual runtime regression result is inferred from compilation.
- No narrative/gameplay outcome was manually advanced or validated in Unity.

## Unity regression checklist

Open `Assets/Scenes/SandPlanet_Prototype_04.unity` in Unity `6000.4.1f1` and verify in Play Mode:

### Startup and hub

- [ ] Starting-level allocation still requires a total of 6, applies the selected Personal/Social/Technical levels, and then starts the game.
- [ ] Planet hub appears for the pre-Week-3 period and the ship-interior hub appears from Day 15 under the existing rules.
- [ ] Clicking a 3D Location node opens the correct Location.
- [ ] Clicking an empty part of an open Location viewport does not close it.
- [ ] “허브로” and ESC close LocationBrowse to the hub.

### Location golden baseline

- [ ] Location viewport is opaque, above Quest/Log, and clipped at its invisible boundary.
- [ ] Location title remains at the upper left.
- [ ] Every accessible location retains its approved dark background color.
- [ ] Characters and world targets retain natural non-overlapping placement, labels, Quest markers, and clipping.
- [ ] Browse scale is `1.5x`.
- [ ] Selecting each target moves to `2.0x` focus and pans toward that target.
- [ ] Returning from TargetSelected to LocationBrowse restores browse pan/scale without closing the Location.

### Interaction and narrative

- [ ] Selecting a target opens the right-side Interaction panel.
- [ ] Interaction buttons keep natural action sentences as primary text and Quest type/role/title as secondary metadata.
- [ ] Character selection shows the correct portrait.
- [ ] Starting an Interaction switches the right side to Narrative without reordering, resetting, or jumping the left map composition.
- [ ] Dialogue rows show the correct speaker portrait and name.
- [ ] Choice availability, requirement reasons, Will/time/XP cost labels, and QuestAction chips match the previous UI.
- [ ] Completing or cancelling flows leaves the correct Location/target state and does not change Quest/Event/State outcomes.

### Navigation and forced flow

- [ ] ESC from `TargetSelected` closes only the Interaction layer and leaves `LocationBrowse` open.
- [ ] A second ESC from `LocationBrowse` returns to the hub.
- [ ] ESC closes People or Quest detail first without also closing the underlying layer.
- [ ] ESC cancels an uncommitted Interaction flow under the existing rule.
- [ ] ESC preserves the existing committed-flow skip/finish behavior.
- [ ] ESC cannot bypass a Forced Event choice.
- [ ] One ESC press is consumed once; no double-close occurs.

### Persistent secondary UI

- [ ] HUD values, time/Will/XP progress bars, context card, End Day button, and Log update at the same moments as before.
- [ ] Quest tracker ordering, type colors, hover detail, current objective, and reunion checklist remain correct.
- [ ] People panel opens from its tab, shows the same cards/affinity/future-intent/event information, and closes one layer at a time.
- [ ] Location viewport remains above Quest/Log while People and Narrative retain their intended top-layer behavior.

Stop after this checklist. Phase 2 Gameplay/Data refactoring is not part of this result.
