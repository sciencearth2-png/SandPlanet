# Narrative Vertical Slice — D1 UI integration

Branch target: `prototype-foundation`

## Scope

This slice changes presentation only. `Prototype04FlowRuntime` remains the authority for conditions, staged effects, choice execution, quest actions, and commits.

- Right-side narrative becomes a scrollable accumulated reading log.
- A clicked choice is appended into that log before the next node appears.
- Choice buttons show visible requirements/costs: time, explicit Will cost, soft-stat requirement and Will supplement, readable Affinity/Stat hard conditions.
- Hover shows deterministic immediate mechanical results available from the current node: time, Will, XP, Affinity.
- Locked choices remain visible and hoverable.
- Internal STATE/quest/ending flags are intentionally not exposed in hover previews.
- Character portraits and existing interaction/quest badges are preserved.

## Deliberate non-goals for this first slice

- No new dialogue manager or gameplay authority.
- No W3 Timeline/Emergency implementation.
- No ending evaluator migration.
- No generated CSV rewrite in this commit. Existing D1 content is used to validate reading flow first; the newly authored D1 text should be migrated only after the UI interaction feels correct in play.

## Local test checklist

1. Pull `prototype-foundation`, open `SandPlanet_Prototype_04` and enter Play Mode.
2. Finish the opening stat allocation.
3. Click a D1 character and start any available dialogue/interaction.
4. Confirm previous dialogue remains above the current line and the panel can scroll.
5. Hover each choice and confirm the preview card shows time / Will / XP / Affinity changes when present.
6. Confirm locked stat choices remain visible and show why they are locked.
7. Click a meaningful choice and confirm the chosen text is appended to the reading log before the next response.
8. Confirm portraits still appear and ESC/back behavior still follows the existing navigation rules.

If this passes, migrate the authored D1 scenes into the authoring workbook/CSV pipeline instead of hardcoding them in presentation code.
