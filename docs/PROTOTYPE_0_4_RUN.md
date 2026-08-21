# SandPlanet Prototype 0.4 — Data Driven / Authoring v1.6

Prototype 0.4 keeps Prototype 0.1–0.3 intact and provides an Excel/CSV-driven vertical slice. The scene name remains 0.4, while the current authoring contract is **v1.6 Quest Roles**.

## Authoritative content

The development authoring source is:

`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Generated runtime CSV lives in:

`Assets/SandPlanet/Data/Generated/CSV/`

Runtime sheets:

- `01_장소` → `Locations.csv`
- `02_캐릭터` → `Characters.csv`
- `03_사물·상호작용 대상` → `WorldTargets.csv`
- `04_퀘스트` → `Quests.csv`
- `05_퀘스트 단계` → `QuestSteps.csv`
- `06_상호작용 플로우` → `Interactions.csv`
- `07_상태·플래그` → `States.csv`
- `08_이벤트 플로우` → `Events.csv`
- `09_이벤트 트리거` → `EventTriggers.csv`
- `10_NPC 일정` → `NpcSchedules.csv`

`Choices.csv` and `ChoiceBeats.csv` are legacy placeholder assets only. v1.6 runtime does not read them.

The exporter searches for each sheet's machine header key (`LocationID`, `QuestID`, `FlowID`, etc.) instead of depending on a fixed row number.

## v1.6 quest rule

Quest state is never mutated merely because a Day/State/condition became true.

Legal mutation paths are:

- player selects an Interaction result with `QuestAction`, or
- an Event Flow result has `QuestAction`.

Event Trigger only decides when an Event occurs. QuestStep may still react to a fired Event through `ProgressEventID`, which is an Event-driven update rather than an implicit condition update.

Interaction `QuestRole`:

- `NONE`: ordinary action
- `OFFER`: linked Quest must be `LOCKED`; UI shows `?`
- `PROGRESS`: linked Quest must be `ACTIVE` and its current Step must match; UI shows `!`

Target/entry UI derives type and role automatically:

- MAIN orange: `?` / `!`, entry `[MAIN ?]` / `[MAIN !]`
- CHARACTER green: `?` / `!`, entry `[CHAR ?]` / `[CHAR !]`
- SIDE light yellow: `?` / `!`, entry `[SIDE ?]` / `[SIDE !]`
- ordinary action: `[일상]`

Character Quest is reserved for one long-running quasi-main arc per major character. Smaller one-off content should be SIDE.

## Excel → CSV workflow

Saving/replacing `SandPlanet_Master.xlsx` under `Assets` automatically runs the exporter on Unity reimport.

Manual commands:

- `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
- `Tools > SandPlanet > Data v1.6 > Validate Generated CSV`

Compatibility aliases under `Tools > SandPlanet > Data 0.4` are still available.

The exporter reads XLSX OpenXML directly using .NET ZIP/XML APIs. No Excel installation, Python package, or external converter is required by Unity.

Validation additionally checks:

- `QuestRole` is `NONE / OFFER / PROGRESS`
- OFFER has an explicit `ACTIVATE_QUEST` result
- Interaction/Event QuestAction targets an existing Quest
- `SET_QUEST_STEP` points to a Step owned by that Quest
- quest-acceptance Interaction costs at least 1 hour
- Character Quest IDs follow `QST_CHAR_...`

## Prototype scene

Existing scene:

`Assets/Scenes/SandPlanet_Prototype_04.unity`

The v1.6 exporter deliberately keeps the same runtime CSV asset paths, so **normal v1.6 content changes do not require regenerating the scene**.

Use `Tools > SandPlanet > Generate Prototype 0.4` only when the actual generated scene structure needs rebuilding, such as changes to the eight greybox location nodes.

### Map roots

Day 1–14 uses `PlanetHubRoot_Day01_14`:

- LOC_01_SETTLEMENT — 거주지
- LOC_02_SHIP — 수송선
- LOC_03_GRAVEYARD — 묘지
- LOC_04_OASIS — 오아시스

Day 15–21 uses `ShipInteriorHubRoot_Day15_21`:

- LOC_05_COMMAND — 지휘·항해
- LOC_06_SUPPLY — 보급·생명유지
- LOC_07_TECH — 기관·연구
- LOC_08_HABIT — 거주

## Runtime scope

Implemented:

- 21-day clock, 08:00–22:00 actions
- sleep/day advance and Will recovery
- Personal / Social / Technical Level + XP
- progress-bar HUD for time, Will, and all three XP tracks
- integrated Interaction/Event narrative nodes
- dialogue / narration / choice → next-node flow
- Time / Will / XP / affinity / State results
- Interaction affinity up to two characters; Event affinity across six principal characters
- Quest LOCKED → ACTIVE → COMPLETED / FAILED
- explicit Interaction/Event `QuestAction`
- `QuestRole` OFFER/PROGRESS availability rules
- QuestStep Event progression
- Event Trigger processing
- ONCE / DAILY / UNLIMITED repeat rules
- NPC schedule-based location override
- target UI quest `? / !` markers and typed interaction labels
- Day 15 planet → ship interior map switch
- Day 21 preservation/stability summary
- new Input System world-click bridge
- ESC popup/location handling

Current prototype limitations / TEMP:

- final ending evaluator is not fixed yet
- Week 3 interior is greybox only
- WORLD_MARKER is represented in the interaction UI rather than a dedicated 3D marker
- DetectorType/DetectorID are stored, while current prototype trigger execution primarily relies on timing + validation conditions
- some Week 3 character starting stances are still TEMP in the authoring workbook
- final compile/play verification must be done in the local Unity 6000.4.1f1 project

## v1.6 smoke test

1. Pull `prototype-foundation` and wait for Unity compile/import.
2. Put the v1.6 workbook at `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`.
3. Run `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV` and confirm validation passes.
4. Open the existing `Assets/Scenes/SandPlanet_Prototype_04.unity`; do not regenerate it for this test.
5. Play Day 1 and confirm the Main wake-up Event/Interaction flow still works.
6. Advance to Day 2. An unaccepted Character Quest target should show a **green `?`**.
7. Click that target and confirm an entry such as `[CHAR ?] ...` appears.
8. Accept it. The authored 1 hour should pass, Quest should become ACTIVE, and the same target should refresh to a **green `!`** with `[CHAR !] ...` progress entry.
9. Confirm MAIN markers are orange and SIDE markers are light yellow.
10. Confirm the Quest tracker contains the six long Character arcs rather than separate Week-by-Week Character quests.
11. Continue through Day 15 map switch and Day 21 summary for broader regression testing.
