# SandPlanet Prototype 0.4 — Data Driven / Authoring v1.6

Prototype 0.4 is the current and only active prototype target. It provides an Excel/CSV-driven vertical slice using the integrated Interaction/Event/Quest/State flow.

## Authoritative content
Development authoring source:
`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Generated runtime CSV:
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

`Choices.csv`와 `ChoiceBeats.csv`는 현재 runtime이 읽지 않는 placeholder다.

## Quest rule
Quest 상태는 Day/State/condition이 참이 되었다는 이유만으로 자동 변경하지 않는다.

합법적인 mutation 경로:
- Interaction 결과의 `QuestAction`
- Event Flow 결과의 `QuestAction`

Event Trigger는 Event 발생 시점만 결정한다.

Interaction `QuestRole`:
- `NONE`: 일반 행동
- `OFFER`: linked Quest가 LOCKED일 때 수락 진입
- `PROGRESS`: linked Quest가 ACTIVE이고 현재 Step이 맞을 때 진행 진입

Quest type UI:
- MAIN: orange
- CHARACTER: green
- SIDE: pale yellow
- OFFER `?`, PROGRESS `!`

## Excel → CSV workflow
정상적인 authoring 수정:
1. `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
2. `Tools > SandPlanet > Data v1.6 > Validate Generated CSV`
3. 기존 `Assets/Scenes/SandPlanet_Prototype_04.unity`에서 테스트

`Tools > SandPlanet > Generate Prototype 0.4`는 scene 구조 자체를 재생성해야 할 때만 사용한다. 일반 데이터/UI/런타임 수정 때문에 실행하지 않는다.

## Prototype scene
`Assets/Scenes/SandPlanet_Prototype_04.unity`

### Day 1–14 planet hub
`PlanetHubRoot_Day01_14`
- LOC_01_SETTLEMENT — 거주지
- LOC_02_SHIP — 수송선
- LOC_03_GRAVEYARD — 묘지
- LOC_04_OASIS — 오아시스

### Day 15–21 ship interior hub
`ShipInteriorHubRoot_Day15_21`
- LOC_05_COMMAND — 지휘·항해
- LOC_06_SUPPLY — 보급·생명유지
- LOC_07_TECH — 기관·연구
- LOC_08_HABIT — 거주

## Runtime scope
Implemented:
- 21-day clock
- sleep/day advance and Will recovery
- Personal / Social / Technical Level + XP
- Interaction/Event integrated narrative flow
- dialogue / narration / choice → next node
- Time / Will / XP / Affinity / State results
- Quest LOCKED → ACTIVE → COMPLETED / FAILED
- explicit `QuestAction`
- QuestRole OFFER/PROGRESS availability
- QuestStep progression
- Event Trigger processing
- ONCE / DAILY / UNLIMITED repeat rules
- NPC schedule location override
- target Quest markers
- Day 15 hub switch
- Day 21 summary hooks
- new Input System world-click bridge
- ESC/back handling
- Week 1 starting stat allocation UI
- People panel
- Quest hover detail and W1 six-person checklist
- character portraits
- location map-style target placement / clipping / focus zoom
- right-side Interaction/Narrative presentation

## Current UI contract
- Location viewport is opaque and sits above Quest/Log while open.
- Location title is upper-left.
- Character/object targets are distributed like a map and clipped by the viewport boundary.
- Browse scale 1.5x; target focus 2.0x.
- Opening narrative from a selected target must not unexpectedly rearrange the left map.
- Interaction selection and Narrative use the right-side panel area.
- Character portrait can appear during Interaction selection and character dialogue.
- ESC closes the topmost interaction layer first rather than collapsing multiple layers in one press.
- Narrative typography has been increased for readability.

## Known technical debt
Several Prototype04 presentation components currently write overlapping UI properties via Reflection, execution order, `LateUpdate`, and `Canvas.willRenderCanvases`. This is the subject of the upcoming audit/refactor. Do not add another patch/override layer before that audit unless explicitly requested.

## Current limitations / TEMP
- final ending evaluator thresholds are not fixed
- Week 3 interior is greybox
- some stance/balance values are TEMP
- final Soft Requirement formula is TBD
- Unity compile/play verification must be performed in the local Unity project

## Smoke test
1. Pull `prototype-foundation` and wait for Unity compile/import.
2. Use the existing Prototype04 scene; do not regenerate it.
3. Confirm game-start stat allocation → GAME_START flow.
4. Enter a location and verify characters/objects, right-side interaction UI, portrait, clipping and focus behavior.
5. Accept a Character Quest and verify `? → !`, time cost, ACTIVE state and tracker update.
6. Verify Interaction/Event results change time/will/xp/affinity/state/quest as authored.
7. Verify ESC closes only the topmost UI layer per press.
8. Continue through Day 15 hub switch and broader Week 3 hooks when regression testing the full loop.
