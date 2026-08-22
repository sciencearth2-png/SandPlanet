# SandPlanet Unity Architecture — Prototype 0.4

## 기술 기준
- Unity 6000.4.1f1 / URP
- 3D 디오라마형 허브 + 2D 장소/내러티브 UI
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`
- Authoring: Excel → Generated CSV

## 전체 구조
Prototype 0.4는 **Static Content / Gameplay Runtime / Unity Coordinator / Presentation**을 분리한다.

### Static Content
`SandPlanetContent04`가 Generated CSV를 읽어 다음 정의를 제공한다.
- Location
- Character
- WorldTarget
- Quest / QuestStep
- Interaction / InteractionFlow / FlowNode
- State definition
- EventFlow / EventTrigger
- NPC Schedule

정적 콘텐츠는 가능한 한 workbook/CSV에서 authoring한다. 특정 캐릭터/Quest/Day 분기를 core code에 직접 쌓지 않는다.

## Gameplay Runtime
### `Prototype04GameState`
mutable runtime state의 유일한 저장소.
- Day / Hour / Will / MaxWill
- Personal / Social / Technical Lv + XP
- States
- Affinity
- Quest status / current Quest step
- occurred Events
- Interaction/Trigger repeat tracking

UI/RectTransform/scene object를 알지 않는다.

### `Prototype04ConditionEvaluator`
조건 판정의 유일한 구현.
- STATE
- DAY
- TIME
- AFFINITY
- QUEST_STATUS
- QUEST_STEP
- EVENT_OCCURRED
- INTERACTION_DONE
- STAT_LEVEL
- AND / OR, numeric/string 비교

### `Prototype04QuestService`
Quest mutation/query authority.
- ACTIVATE / COMPLETE / FAIL / SET_STEP
- 현재 Quest status/step
- explicit node `QuestAction`
- 기존 `ProgressEventID/OnProgressEvent` progression

현재 두 Quest progression 계약은 호환성을 위해 병존한다. 하나로 통합하려면 workbook/CSV migration을 별도 설계한다.

### `Prototype04ScheduleService`
NPC 위치와 schedule 판정 authority.
- OpenDay / CloseDay
- Morning / Afternoon / Evening
- authored conditions
- priority

### `Prototype04InteractionService`
Interaction availability authority.
- target type / ID
- active/open/close day
- time slot
- repeat rule
- QuestRole / QuestStep gate
- authored conditions

### `Prototype04EventService`
Event/Trigger authority.
- GAME_START / DAY_START / DAY_END / INTERACTION / STATE_CHANGE trigger
- trigger repeat tracking
- Event occurrence
- Event queue
- SILENT linear Event routing
- STATE_CHANGE re-entry guard

### `Prototype04FlowRuntime`
Interaction/Event Flow authority.
- active flow/node
- pending effects
- committed state
- node availability
- staged Time/Will/XP/Affinity/State/Quest/Event effect
- flow-end commit
- silent Event resolution
- committed ESC linear remainder
- forced Event choice protection

Pending effects의 적용 순서는 현재 게임 규칙의 일부다.
1. Time / Will
2. XP
3. Affinity
4. State + STATE_CHANGE Trigger
5. QuestAction
6. emitted Event

## Unity Coordinator
### `SandPlanetPrototype04Controller`
Gameplay service를 구성하고 Unity scene 및 presentation facade에 연결하는 coordinator/adapter.

주요 책임:
- CSV content load
- gameplay service construction/wiring
- scene serialized reference
- current Location / selected Target coordination
- day transition 및 hub root switch
- presentation change notification
- simple modal / log bridge

새 gameplay 규칙이나 캐릭터별 특수 로직을 이 Controller에 다시 집중시키지 않는다.

### `Prototype04RuntimeFacade`
Presentation이 Controller/gameplay의 필요한 typed API만 읽고 호출하도록 하는 facade.
Presentation component는 gameplay service의 내부 저장소를 직접 수정하지 않는다.

## Presentation / Navigation
### `Prototype04CompositionRoot`
Phase 1 presentation dependency를 명시적으로 조립한다.

### `Prototype04NavigationController`
UI mode와 Back/ESC의 유일한 authority.
- Hub
- LocationBrowse
- TargetSelected
- Narrative
- ForcedChoice
- People
- QuestDetail

다른 component를 disable하거나 execution order 경쟁으로 ESC 우선순위를 만들지 않는다.

### `Prototype04LocationPresenter`
Location presentation의 유일한 authority.
- opaque viewport
- location title/background
- target 생성과 typed binding
- target placement
- clipping
- targetRoot pan/scale
- browse 1.5x / target focus 2.0x

같은 targetRoot RectTransform을 다른 component가 최종 writer로 수정하지 않는다.

### `Prototype04NarrativePresenter`
우측 Interaction/Narrative presentation authority.
- Interaction 목록
- Narrative node/choice
- Quest metadata/chip
- character portrait
- right panel geometry/typography

### `Prototype04HudPresenter`
HUD/context/progress bar presentation authority.

### Quest / People Presentation
- `SandPlanetPrototype04W1QuestTracker`: Quest tracker/hover/checklist 표현.
- `SandPlanetPrototype04PeoplePanel`: 인물/호감도/stance 표현.

이 component들은 gameplay state를 읽지만 Quest/State/Affinity mutation을 직접 소유하지 않는다.

## Input flow
1. 3D hub의 `PrototypeLocationNode`가 Location ID를 가진다.
2. Input bridge가 typed Location ID를 Controller/Navigation으로 전달한다.
3. LocationPresenter가 현재 Location target을 typed binding으로 표시한다.
4. Target 선택은 target type/ID를 직접 전달한다. UI text를 identity로 사용하지 않는다.
5. InteractionService가 가능한 Interaction을 계산한다.
6. FlowRuntime이 선택된 Interaction/Event Flow를 실행하고 effect를 stage한다.
7. flow가 끝날 때 effect를 한 번 적용한다.
8. RuntimeFacade의 change notification을 통해 Presenter들이 갱신된다.

## One-owner matrix
| Concern | Final authority |
| --- | --- |
| Day/Hour/Will/XP/Affinity/State 저장 | `Prototype04GameState` |
| Condition evaluation | `Prototype04ConditionEvaluator` |
| Quest mutation/progression | `Prototype04QuestService` |
| NPC schedule | `Prototype04ScheduleService` |
| Interaction availability | `Prototype04InteractionService` |
| Event/Trigger/queue | `Prototype04EventService` |
| Flow/pending effect/commit | `Prototype04FlowRuntime` |
| UI mode / ESC | `Prototype04NavigationController` |
| Location layout / targetRoot pan-scale | `Prototype04LocationPresenter` |
| Interaction/Narrative/portrait | `Prototype04NarrativePresenter` |
| HUD | `Prototype04HudPresenter` |

## 확장 원칙
새 기획 요청을 구현할 때 다음 순서를 따른다.

1. **Data-only 여부 확인**
   - Interaction/Event/Quest/State/Schedule/조건/수치로 표현 가능하면 workbook/CSV만 수정한다.
2. **기존 authority 확인**
   - UI 변경이면 기존 Presenter, gameplay 규칙이면 기존 Service를 수정한다.
3. **새 state가 필요한지 확인**
   - 필요하다면 owner를 하나만 정한다.
4. **typed input/output 정의**
   - UI text parsing이나 Reflection 대신 ID, DTO/view model, event/API를 사용한다.
5. **회귀 테스트 정의 후 구현**
   - 기존 21일 흐름에서 무엇이 영향받는지 먼저 정한다.

## 금지 패턴
- 같은 RectTransform/visibility/input/gameplay state에 여러 final writer.
- Reflection으로 다른 component의 private field를 읽는 구조.
- UI 문자열에서 Character/Target/Quest ID를 역추론.
- `DefaultExecutionOrder`, `LateUpdate`, `Canvas.willRenderCanvases` 경쟁으로 최종값을 결정.
- 요청 하나마다 `Polish`, `Finalizer`, `Override`, `Guard` MonoBehaviour 추가.
- 캐릭터명/QuestID/특정 Day를 core service에 무분별하게 하드코딩.
- Data로 해결 가능한 콘텐츠 변경을 코드 분기로 구현.
- Scene generator를 일반 수정 도구처럼 실행.

## 의도적으로 남은 기술 부채
다음은 현재 정상 동작하며 즉시 제거 대상이 아니다.
- `QuestAction`과 `ProgressEventID/OnProgressEvent` 병존.
- Quest `PlayerDescription`의 호환성 read path.
- Controller 내부의 consolidated presentation 비활성 시 legacy fallback UI builder.
- 문자열 기반 runtime log.

이 항목을 정리할 때는 기능 개선과 섞지 않고 별도 migration/cleanup으로 다룬다.

## 현재 UI 계약
- Location viewport는 불투명하고 Quest/Log보다 위.
- 장소명 좌측 상단.
- target은 자연 배치 + invisible clipping.
- browse 1.5x / target focus 2.0x.
- Interaction → Narrative 전환에서 장소 배치/zoom이 급변하지 않는다.
- 우측 Interaction/Narrative panel.
- 캐릭터 portrait.
- ESC는 topmost UI layer 하나씩 consume.

## Scene generation 주의
`Tools > SandPlanet > Generate Prototype 0.4`는 scene 구조 전체를 다시 생성하는 destructive recovery/generation 도구로 취급한다.
일반 기획, 데이터, UI, gameplay rule 수정에서는 실행하지 않는다.
