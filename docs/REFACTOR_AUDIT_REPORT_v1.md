# SandPlanet Prototype 0.4 Refactor Audit Report v1

> 감사 대상: 현재 로컬 `prototype-foundation` 기반 worktree의 Prototype 0.4
>
> 감사 방식: 정적 코드/scene YAML/Generated CSV 조사. Unity Editor 실행, scene 재생성, 코드·scene·data 수정은 하지 않았다.
>
> 판정 기준: `REFACTOR_HANDOFF_v1.md` → `W1_NARRATIVE_SYSTEM_v1_7.md` → `PROTOTYPE_0_4_RUN.md` → 나머지 설계 문서 → 현재 코드/scene/data. 충돌은 임의 해결하지 않고 아래에 기록한다.

## 1. Executive summary

현재 Prototype 0.4는 데이터 기반 21일 루프, Interaction/Event 공용 flow, Quest/State, NPC schedule, Week 1 시작 능력치 배분, 인물/Quest/UI 기능을 한 scene에서 연결하고 있다. 핵심 gameplay loop는 존재하며 Generated CSV에도 Week 1 재회, 조사, 공개 결정과 Week 2/3 골격이 들어 있다.

그러나 최근 UI 기술 부채에 대한 우려는 **확인됨**이다. 현재 scene에는 `SandPlanetPrototype04Controller`만 직렬화되어 있고, 실행 시 12개의 `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` bootstrap이 보정 컴포넌트를 동적으로 주입한다. 이 컴포넌트들은 controller private field/method를 Reflection으로 읽고 호출하며, `Update`, `LateUpdate`, `Canvas.willRenderCanvases`와 `DefaultExecutionOrder`로 최종 writer 경쟁을 조정한다.

핵심 확인 사항은 다음과 같다.

- `targetRoot.localScale`은 `SceneInteractionPolish.LateUpdate`와 `SceneDensityOverride.LateUpdate`가 같은 프레임에 서로 다른 목표값(1.30x 대 browse 1.50x/focus 2.00x)으로 쓴다. 후자의 실행 순서 40000이 최종값을 이기도록 설계되어 있다.
- `locationPanel` geometry/color/sibling order는 builder/scene 값 외에 `UxEnhancer`, `SceneDialogueLayout`, `SceneInteractionPolish`, `MapViewportPolish`가 중복 소유한다. 색은 한 render cycle 안에서 최소 세 값으로 덮어써진다.
- Interaction panel 활성 상태, modal geometry/typography/button height, portrait geometry/visibility도 복수 writer가 매 frame 또는 canvas render마다 쓴다.
- ESC는 `InputSystem.onAfterUpdate`, 두 개의 `Update`, controller reflect-call 경로가 얽혀 있으며, 한 handler가 다른 컴포넌트를 한 프레임 disable하여 중복 처리를 막는다. 이는 명시적 navigation state가 아니라 실행 타이밍에 의존한다.
- `SceneInteractionPolish`는 선택 target을 명시적 ID가 아니라 `locationHintText` 문구를 파싱해 역추론한다. UI 문구가 model state 역할을 한다.
- 문서의 “Quest mutation은 명시적 Interaction/Event `QuestAction`만” 계약과 달리 controller의 `FireEvent → ProgressQuestsFromEvent`는 `QuestStep.ProgressEventID/OnProgressEvent`만으로 Step/Quest를 자동 변경한다. 현재 CSV에는 이 경로를 사용하는 활성 Step이 다수 존재한다. 이것은 단순 구현 세부가 아니라 source-of-truth 충돌이므로 사용자 결정이 필요하다.
- scene generator는 현재 runtime 보정 구조를 만들지 않고 기본 uGUI scene만 새로 저장한다. bootstrap 때문에 일부 외형은 다시 생기지만, scene 자체를 통째로 교체하고 CSV export까지 선행하므로 현재 scene 수동 변경을 덮어쓸 위험이 높다.

따라서 리팩터링은 controller를 즉시 교체하는 방식보다, 현 동작을 golden baseline으로 고정한 뒤 presentation/navigation부터 단일 authority로 옮기고 마지막에 gameplay state/flow를 분리하는 단계적 strangler migration이 안전하다. 이 감사에서는 구현하지 않았다.

## 2. Current feature inventory

### Runtime/gameplay

- 21일 clock, 행동 가능 시각 08:00~22:00, 수면 시 Day 증가 및 의지 +2.
- 개인/대인/기술 Lv+XP, 6XP 즉시 level-up, Lv 20 cap.
- 최대 의지 5, node의 Time/Will/XP/Affinity/State 결과 누적 후 flow 종료 시 commit.
- Hard condition 2개 및 Soft Requirement 부족분을 의지로 보완하는 TEMP 공식.
- Interaction availability: day/time slot/repeat/QuestRole/current Step/condition.
- Interaction/Event 공용 narrative node UI: dialogue, narration, 선택지, next-node chain.
- ONCE/DAILY/UNLIMITED tracking.
- State/affinity/quest/event occurrence dictionaries.
- Event trigger timing: 현재 CSV 기준 `GAME_START`, `DAY_START`, `STATE_CHANGE`; controller는 추가로 `DAY_END`, `INTERACTION`도 호출 가능.
- NPC schedule priority에 따른 character location override.
- Day 15 planet hub → ship interior hub switch, Day 21 TEMP summary.

### Content/data

- 런타임 입력: 8 Locations, 6 Characters, 20 WorldTargets, 20 Quests, 33 QuestSteps, 42 States, 124 Interaction rows, 79 Event rows, 32 EventTriggers, 7 NPC schedules.
- `Choices.csv`, `ChoiceBeats.csv`는 빈 placeholder이며 runtime이 읽지 않는다.
- Week 1 시작 능력치 배분 2/2/2 default, 합계 6 확인 후 GAME_START 진행.
- Day 1~2 6인 재회 State 및 특수 checklist.
- Benjamin Character Quest Day 1 offer; 나머지 5인 Character Quest offer는 Day 2부터.
- 오아시스 조사 다중 경로와 Day 7 공개/제한 공개 다인 flow.
- Week 2 preservation State와 Week 3 4구역 stability/future-talk 골격.

### Presentation/navigation

- 3D hub location click, location viewport, map-style target cards, clipping, focus pan/zoom.
- target Quest marker, Interaction label 보정, narrative Quest action chip.
- Quest tracker 재구성, hover detail, 6인 checklist.
- HUD progress bars/context card/log.
- speaker portrait와 People panel.
- layered ESC/back 처리와 world click bridge.

## 3. Runtime architecture map

### Scene-authored base

`Assets/Scenes/SandPlanet_Prototype_04.unity`에는 다음 기반이 직렬화되어 있다.

- `Prototype04GameController` + `SandPlanetPrototype04Controller` 1개.
- `PrototypeLocationNode` 8개.
- `Canvas_Prototype04` 아래 HUD, QuestTracker, Log, LocationPanel, Targets/TargetButtons, Interactions/InteractionButtons, ModalPanel/ModalButtons.
- controller의 CSV 12개 TextAsset, hub camera/root, UI field reference.
- presentation patch 컴포넌트는 scene YAML에 직렬화되어 있지 않다.

### Runtime-created layer

AfterSceneLoad bootstrap이 controller GameObject에 presentation/input 컴포넌트를 주입한다. 각 컴포넌트는 Start 시 scene object를 Reflection 또는 이름 검색으로 얻고, 일부는 `RectMask2D`, `LayoutElement`, target binding, portrait, HUD bars, People panel, Quest rows 등을 다시 동적 생성한다.

### Controller responsibility concentration

`SandPlanetPrototype04Controller`(1,039 lines)는 아래를 동시에 담당한다.

- CSV content load와 runtime state 초기화.
- 시간/의지/성장/affinity/State/Quest mutation.
- Interaction availability 및 condition evaluator.
- Event trigger/queue/silent resolution.
- flow traversal, pending effect transaction, modal semantics.
- character schedule/location 결정.
- hub switch와 Day 21 summary.
- target/interaction/modal button 생성 및 파괴.
- HUD/Quest tracker/log 문자열 생성.
- location/modal active state와 ESC semantics.

Gameplay state authority와 UI construction/presentation/navigation이 같은 private surface 안에 있어, 외부 patch들이 private field 이름에 결합할 수밖에 없는 구조다.

## 4. UI/input writer map

아래 “매 frame”은 state 변경 여부와 무관하게 Update/LateUpdate/render callback에서 반복 쓰는 경우다.

### `locationPanel`

| Writer | Lifecycle/order | Writes | 다른 writer 및 위험 |
|---|---|---|---|
| Builder/CreateUi + scene YAML | editor generation / serialized | anchors `.53,.06`~`.99,.92`, base color, inactive | runtime 값과 불일치. Generate 실행 시 scene 전체 기준값 복원 |
| Controller `Awake/OpenLocation/CloseLocation/EndDay` | Awake/callback | active state | gameplay와 presentation ownership 결합 |
| `UxEnhancer.RestyleHudAndPanels` | Start/default order | anchors `.53,.06`~`.99,.84` | 곧 `SceneDialogueLayout`이 덮어씀 |
| `SceneDialogueLayout.ApplyLocationLayout/EnforceFinalGeometry` | Start, LateUpdate, canvas callback / 20000 | anchors `.019,.026`~`.728,.837`, color alpha .90, initial sibling index | geometry/color를 매 frame + render마다 강제 |
| `SceneInteractionPolish.EnforcePresentation` | LateUpdate, canvas callback / 30000 | opaque dark color, last sibling | `SceneDialogueLayout` color/ordering을 다시 덮음 |
| `MapViewportPolish.FinalizePresentation` | LateUpdate, canvas callback / 50000 | location별 opaque color, last sibling | 최종 color/order를 실행 순서로 승리. 문서상 “polish only”이나 실질 final authority |

판정: active state는 주로 controller가 소유하지만 geometry/color/order는 3~4개 writer가 경쟁한다. 유지보수 위험 **매우 높음**.

### `targetRoot`와 target cards

| Writer | Lifecycle/order | Writes | 다른 writer 및 위험 |
|---|---|---|---|
| Controller `RefreshTargets/CreateButton` | location/state callback | child 생성/삭제, label, click callback | presentation view를 gameplay controller가 생성 |
| `UxEnhancer.RefreshTargetBadges` | canvas callback | label 재작성, binding 동적 주입, click listener 추가 | controller label을 다시 쓰고 정렬 순서로 identity를 매칭 |
| `SceneDialogueLayout.DecorateSceneTargets` | LateUpdate + canvas / 20000 | card anchors/position/size, portrait child, label/style, `SceneTargetView04` 주입 | target placement의 실제 매-frame authority. target ID를 최초 label에서 식별 |
| `SceneInteractionPolish.AnimateSceneCamera` | LateUpdate / 30000 | `anchoredPosition`, `localScale`(focus 1.30x) | scale이 DensityOverride와 직접 충돌 |
| `SceneDensityOverride.LateUpdate` | LateUpdate / 40000 | `localScale` browse 1.50x/focus 2.00x | 같은 frame 뒤에서 1.30x 값을 덮음. `originalScale` capture timing에도 의존 |

카드 배치는 `SceneDialogueLayout`의 특정 ID별 hard-coded slot/fallback slot에 의존한다. 위치는 하나의 주 writer가 있으나, root scale은 명백한 2-writer race이며 root pan은 `SceneInteractionPolish`가 소유한다. `SceneDialogueLayout` 주석의 “scale 단일 소유” 계약은 실제 코드와 맞지 않는다.

### Interaction panel/buttons

| Writer | Lifecycle/order | Writes | 위험 |
|---|---|---|---|
| Controller `SelectTarget/FinalizeActiveFlow/CloseLocation` | callbacks | dynamic button 생성/삭제, hint text | UI view와 gameplay selection 결합 |
| `QuestOfferLabels` | LateUpdate + canvas / 12000 | Interaction label을 natural action + quest hint로 매 render 재작성 | button 순번을 available interaction 순번과 결합 |
| `SceneDialogueLayout.RefreshBeforeRender` | LateUpdate + canvas / 20000 | parent active state, sibling order, header, button style | panel visibility의 주 presentation writer |
| `SceneInteractionPolish.CloseInteractionSelectionOnly/EnforcePresentation` | ESC Update/coroutine, LateUpdate + canvas / 30000 | children clear, panel inactive, sibling order | 별도의 close 경로, controller state는 명시적으로 바꾸지 않음 |
| `EscapeLayerGuard.OnAfterInputUpdate` | InputSystem callback / -30000 | child inactive/destroy, parent inactive, hint, UX selected fields | Reflection으로 타 컴포넌트 state까지 직접 수정 |
| `MapViewportPolish` | LateUpdate + canvas / 50000 | sibling order, button height/font | SceneDialogueLayout의 78px/15pt를 84px/17pt로 덮음 |

판정: visibility/label/style/close routing 모두 중복 writer다. **매우 높음**.

### Modal/narrative panel

| Writer | Lifecycle/order | Writes | 위험 |
|---|---|---|---|
| Controller `Begin*/ShowCurrentFlowNode/ShowMessage/CloseModalInternal` | callbacks | active, title/body, button 생성/삭제 | semantic authority는 적절하나 presentation까지 직접 소유 |
| `SceneDialogueLayout` | Start, LateUpdate + canvas / 20000 | right panel geometry, color, title/body rect/font, button root/style/order | 매 frame geometry writer |
| `QuestActionBadge` | canvas / 15000 | modal choice chip 및 LayoutElement/label geometry 동적 주입 | button view 내부를 별도 시스템이 소유 |
| `SceneInteractionPolish` | LateUpdate + canvas / 30000 | sibling order | portrait/layer finalization과 결합 |
| `SceneInteractionFinalizer` | canvas / 31000 | Reflection으로 Polish private methods 재호출 | 같은 render pass에 동일 작업을 의도적으로 한 번 더 수행 |
| `MapViewportPolish` | LateUpdate + canvas / 50000 | title 25/body 19, rect, choice root, 66px buttons | SceneDialogueLayout 23/17/58px 값을 최종 덮음 |

판정: semantic 내용 외의 최종 geometry/style이 실행 순서에 의존한다. **매우 높음**.

### Speaker portrait

- `SceneDialogueLayout.Create/Position/RefreshSpeakerPortrait`가 portrait를 만들고 modal speaker에 따라 active/sprite/name/150x210 geometry를 LateUpdate+canvas에서 쓴다(order 20000).
- `SceneInteractionPolish.PrepareSharedPortrait/RefreshPortraitPresentation`가 같은 `UX18_SpeakerPortraitFrame`을 Canvas로 reparent하고 225x315 geometry로 바꾼 뒤 Interaction selection과 modal 상황에 따라 active/order를 쓴다(order 30000).
- `SceneInteractionFinalizer`가 canvas callback에서 위 private refresh를 다시 호출한다(order 31000).

판정: 동일 GameObject를 서로 다른 의미(대화 speaker와 선택 target portrait)로 두 presentation system이 공유한다. **매우 높음**.

### Quest tracker/detail

- Controller `RefreshUi`가 legacy `questTrackerText.text`를 callback마다 쓴다.
- `W1QuestTracker.Start`는 legacy Text를 비활성화하고 별도 rows root/hover card를 생성한다.
- `W1QuestTracker.LateUpdate`(order 11000)는 reflected `questStatus`, `questStep`, `states` snapshot이 달라지면 rows 전체를 재생성한다.
- checklist는 `QST_W1_MAIN_01_AWAKE`와 6개 `STA_W1_MET_*` ID를 코드에 hard-code한다. 특수 UI 요구에는 부합하지만 generic tracker와 특수 renderer 경계가 클래스 내부에 섞여 있다.
- hover description은 runtime `SandPlanetQuest04`에 없는 `PlayerDescription`을 `csvAssets`에서 다시 직접 파싱해 보충한다. 동일 CSV에 대해 별도 data access path가 생긴다.

판정: visible final output의 writer는 사실상 W1QuestTracker 하나지만 controller가 숨은 legacy Text를 계속 갱신하고, data access가 이중화되어 **중간~높음**.

### Log UI

- Controller `Log/RefreshUi`만 `logText.text`를 쓴다.
- `UxEnhancer`는 LogPanel geometry/style을 Start에 조정하고, `SceneInteractionPolish/MapViewportPolish`의 locationPanel last-sibling 정책이 location open 시 이를 가린다.

판정: 텍스트 writer 충돌은 낮다. visibility는 explicit active state가 아니라 sibling/불투명 panel로 달성되어 **중간 위험**.

### ESC/back input

1. `EscapeLayerGuard`는 `InputSystem.onAfterUpdate`에서 interaction selection을 먼저 닫고, 같은 frame `UxEnhancer`를 disable했다가 coroutine으로 다음 frame 재활성화한다.
2. `SceneInteractionPolish.Update`도 동일 ESC와 동일 interaction selection을 처리하고 같은 방식으로 UxEnhancer를 disable한다.
3. `UxEnhancer.Update`는 modal이면 controller private `HandleEscapeFromUx`, location이면 private `CloseLocation`을 Reflection 호출한다.
4. controller `HandleEscapeFromUx`는 미commit Interaction 취소, committed linear remainder 자동 진행, forced Event choice 보호, simple modal close를 처리한다.
5. Back button과 world-background click은 별도 `CloseLocation` 경로다.

판정: 계층 UX 결과를 지키기 위한 장치는 있으나, “한 handler가 다른 handler를 disable”하는 timing protocol이다. old/new Input System compile symbol과 callback order에 민감하다. **매우 높음**.

### World click / target click routing

- `InputBridge.Update`(order 10000)가 UI 위 클릭을 제외하고 world raycast → `PrototypeLocationNode.LocationId` → reflected `OpenLocation`을 호출한다.
- location이 열려 있는 상태의 UI 밖 world click은 hit test 없이 즉시 `CloseLocation`한다.
- target click은 controller가 생성한 Button listener → private `SelectTarget`으로 간다.
- UxEnhancer는 같은 Button에 추가 listener를 달아 자체 `selectedTargetType/Id`를 유지한다.
- SceneInteractionPolish는 이 binding을 사용하지 않고 `locationHintText`의 “이름 — 가능한 상호작용”을 파싱해 focus target을 찾는다.

판정: 한 클릭이 controller state, UX local state, UI text-derived state 세 표현으로 복제된다. 이름 중복/문구 변경/locale 변경 시 focus와 portrait가 깨질 수 있다. **높음**.

## 5. Data/quest/event/state flow map

### Static load

`SandPlanetContent04.Load`가 scene의 TextAsset 배열을 파일명으로 색인하고 Locations → Characters → WorldTargets → Quests/Steps → States → Interactions → Events → Triggers → Schedules 순으로 읽는다. Interaction flow의 target type은 `Characters.ContainsKey(TargetID)` 여부로 추론하며, 그 외는 모두 world target으로 간주한다.

CSV reader는 RFC4180형 quote/newline을 처리하고 모든 값을 string으로 둔다. typed conversion은 loader에서 한다. 중복 ID는 dictionary에서 마지막 row가 조용히 승리하며 runtime 자체는 validation을 수행하지 않는다. 정합성 보장은 editor exporter/validator 실행에 의존한다.

### Interaction flow

`target click → GetAvailableInteractions → SelectTarget → BeginInteraction → ShowCurrentFlowNode → ChooseNode → StageNode → FinalizeActiveFlow → ApplyPendingEffects`.

효과는 flow 중 staging되고 종료 시 한 번에 적용된다. 이후 interaction repeat tracking, INTERACTION trigger, target/UI refresh, queued event 표시 순이다. 이 transaction 경계는 보존 가치가 높다.

### Event flow

Trigger 또는 node EmitEvent가 `FireEvent`를 호출한다. Event는 즉시 `eventsOccurred`에 기록되고 queue 또는 silent auto-resolution으로 간다. SILENT이며 선형 단일-row chain이면 UI 없이 effect를 적용한다. branching/복수 row이면 queue를 거쳐 modal flow로 표시한다.

### Quest mutation: 확인된 계약 충돌

명시적 경로는 node meta의 `QuestAction` → `ApplyQuestAction`이며 `ACTIVATE_QUEST`, `COMPLETE_QUEST`, `FAIL_QUEST`, `SET_QUEST_STEP`을 지원한다.

동시에 `FireEvent`는 presentation/resolve 전에 `ProgressQuestsFromEvent(eventId)`를 호출한다. 현재 active step의 `ProgressEventID`가 Event ID와 같으면 `OnProgressEvent=SET_STEP/COMPLETE_QUEST`를 실행한다. 이것은 Event node의 `QuestAction` 유무와 무관한 두 번째 mutation authority다. Generated `QuestSteps.csv`에는 이 방식의 활성 row가 Week 1~3에 다수 있다.

문서와 AGENTS 계약은 “Quest 상태는 조건만으로 자동 변경하지 않으며 Interaction/Event 결과의 명시적 `QuestAction`만 합법적”이라고 명시한다. 반면 `PROTOTYPE_0_4_RUN.md`는 QuestSteps.csv의 ProgressEvent 컬럼 존재도 설명한다. 현재 코드/data는 과거 ProgressEvent 방식과 새 explicit QuestAction 방식을 병행한다. 어느 쪽을 canonical로 할지 사용자 승인 없이 제거/변환하면 안 된다.

### State trigger edge

`SetState`는 `STATE_CHANGE` trigger를 동기 처리하고 재진입 방지 bool을 사용한다. 그 trigger가 SILENT event를 발생시켜 추가 State를 바꿀 때는 `evaluatingStateTriggers=true`라 중첩 State의 trigger 검사가 생략되고, outer 종료 후 재스캔하지 않는다. 따라서 chain reaction이 한 단계에서 멈출 가능성이 있다. 현재 data에서 실제 blocker인지 Unity play trace가 필요하다.

### Data contract observations

- `Quests.csv`의 `PlayerDescription`은 `SandPlanetQuest04`에 로드되지 않으며 W1 tracker가 원본 TextAsset을 다시 파싱한다.
- runtime은 missing CSV만 log error를 내고 계속 빈 collection으로 실행한다.
- Interaction `QuestRole` 빈 값은 linked Quest가 있으면 PROGRESS로 fallback한다. 문서상 explicit authoring 원칙과 backward compatibility가 공존한다.
- `SetQuestStatus` private method는 runtime 호출처가 없다.
- `ProgressQuestsFromEvent`와 `QuestStep.ProgressEventID/OnProgressEvent`는 explicit QuestAction 계약 관점에서 legacy candidate지만, 현재 활성 data가 의존하므로 선삭제 불가다.
- Day 7 flow의 동일 NodeID 다중 row는 의도된 choice 표현이며 loader/controller가 지원한다. 같은 방식이 여러 Week 2/3 flow에 사용된다.

## 6. Confirmed spaghetti/conflict findings with evidence

### C1. `targetRoot.localScale` 직접 writer race — Critical

- `SceneInteractionPolish.AnimateSceneCamera`는 매 LateUpdate에 `targetRoot.localScale`을 1.30 focus goal로 보간한다.
- `SceneDensityOverride.LateUpdate`는 execution order 40000에서 같은 property를 1.50/2.00 목표로 다시 쓴다.
- DensityOverride 주석도 자신이 “final scene-density scale authority”이며 position은 Polish가 맡는다고 설명한다. 즉 충돌은 우연이 아니라 후행 override로 봉합된 구조다.

### C2. Location/modal presentation이 execution order로 결정됨 — Critical

`SceneDialogueLayout`(20000) → `SceneInteractionPolish`(30000) → finalizer(31000) → `MapViewportPolish`(50000)가 동일 canvas pass에서 color/order/geometry/typography를 반복 적용한다. 각 클래스가 “Final”, “single authority”, “finalizer”를 선언하지만 실제 authority는 property별로 분산되어 있다.

### C3. ESC 중복 처리를 component disable로 억제 — Critical

`EscapeLayerGuard`와 `SceneInteractionPolish` 모두 interaction selection ESC를 처리하며, `UxEnhancer.Update`가 같은 key를 나중에 처리하지 못하도록 enhancer를 disable 후 다음 frame 재활성화한다. 실행 타이밍, enable state, coroutine survival에 의존하며 navigation state machine이 아니다.

### C4. Quest mutation source-of-truth 위반/이중화 — Critical, user decision required

Controller 551~555 및 612~621의 Event-ID progress가 explicit QuestAction 외 mutation 경로를 만든다. 현재 CSV가 실제로 의존하므로 문서 또는 data/code 중 하나를 승인 후 migration해야 한다.

### C5. UI text에서 target model state 역추론 — High

`SceneInteractionPolish.SelectedTargetNameFromHint`가 `locationHint.text`를 “ — ”로 자르고 character/world target 이름을 검색한다. `SceneDialogueLayout.SelectedTargetHeader`도 같은 text를 읽는다. 명시적 selection model 없이 표시 문자열이 state bus 역할을 한다.

### C6. Reflection/이름 검색에 의한 hidden coupling — High

Controller private field/method rename은 compile error 없이 presentation 기능을 null/disable/catch로 조용히 망가뜨릴 수 있다. 여러 catch block이 예외를 삼킨다. Canvas에서 `HUD`, `Back`, `TargetButtons`, `UX18_*` 이름을 직접 찾는 경로도 scene hierarchy rename에 취약하다.

### C7. Dynamic component injection이 scene과 runtime reality를 분리 — High

scene YAML만 보면 controller 외 patch가 보이지 않는다. 실제 object hierarchy/layout은 Play 진입 후 bootstrap 순서와 runtime AddComponent 결과다. prefab/scene review, edit-time preview, dependency 확인이 어렵다.

### C8. Duplicated constants/style policy — High

Quest colors/role normalization/target badges는 Controller와 UxEnhancer에 중복되고, Interaction label은 QuestOfferLabels가 다시 변환한다. Button height/font, panel anchors/color는 Builder, UxEnhancer, SceneDialogueLayout, MapViewportPolish에 서로 다른 값으로 존재한다.

### C9. Generator와 현재 UI의 구조적 drift — High

Builder는 legacy split LocationPanel 안에 Interactions를 만들고 center Modal을 만든다. runtime은 Interactions를 Canvas로 reparent하고 right panel로 이동하며, modal도 같은 right geometry로 강제한다. Generator 결과만으로 현재 UI contract가 표현되지 않는다.

### C10. State trigger chain 누락 가능성 — Medium/High

재진입 방지 중 발생한 추가 State 변경을 후속 pass에서 검사하지 않는다. 복합 State-trigger chain을 authoring할 경우 순서에 따라 Event가 발생하지 않을 수 있다.

## 7. Reflection/bootstrap/execution-order inventory

| Component | Bootstrap | Order/lifecycle | Controller/private coupling | 주 책임 |
|---|---|---|---|---|
| `OpeningSetup` | AfterSceneLoad | default; bootstrap에서 controller disable, own Start 후 controller re-enable | levels/xp/states/eventsOccurred | stat allocation 및 GAME_START 지연 |
| `PeoplePanel` | AfterSceneLoad | default Update/LateUpdate | content/affinity/states/events/modalBusy | People UI/ESC/card refresh |
| `UxEnhancer` | AfterSceneLoad | default Update/LateUpdate/canvas | 12 fields, 5 methods | HUD/context/badges/ESC |
| `EscapeLayerGuard` | AfterSceneLoad | -30000; InputSystem callback | controller fields/static ClearDynamic + UxEnhancer fields | top interaction ESC 선점 |
| `InputBridge` | AfterSceneLoad | 10000 Update | modal/location/camera, Open/CloseLocation | world click routing |
| `W1QuestTracker` | AfterSceneLoad | 11000 LateUpdate | content/tracker/quest/state/csvAssets | tracker/detail/checklist |
| `QuestOfferLabels` | AfterSceneLoad | 12000 LateUpdate/canvas | content/interactionRoot | Interaction label rewrite |
| `QuestActionBadge` | AfterSceneLoad | 15000 canvas | active flows/node/modal root, quest getters | narrative choice quest chip |
| `SceneDialogueLayout` | AfterSceneLoad | 20000 LateUpdate/canvas | 18 controller fields | location/target/interaction/modal/portrait layout |
| `SceneInteractionPolish` | AfterSceneLoad | 30000 Update/LateUpdate/canvas | 11 fields + static ClearDynamic | focus pan/scale, portrait, layer, ESC |
| `SceneInteractionFinalizer` | AfterSceneLoad | 31000 canvas | Polish private methods | Polish를 final canvas에서 재호출 |
| `SceneDensityOverride` | AfterSceneLoad | 40000 LateUpdate | 6 fields | final targetRoot scale |
| `MapViewportPolish` | AfterSceneLoad | 50000 LateUpdate/canvas | 10 fields | final color/order/typography/button sizing |

총 13개 표 항목 중 controller를 제외한 12개가 runtime bootstrap으로 주입된다. 별도 bootstrap 간 명시적 설치 순서는 없고 MonoBehaviour execution order는 Update 계열에만 의미가 있으므로, 각 Start에서 다른 주입 컴포넌트가 이미 존재한다고 가정하는 부분은 추가 취약점이다.

## 8. 0.4-only obsolete/redundant code candidates

아래는 **삭제 승인 대상 후보**이며 이번 감사에서 삭제하지 않았다.

1. `SceneInteractionFinalizer`: 다른 patch의 private methods를 canvas에서 재호출하는 순수 override layer.
2. `SceneDensityOverride` 또는 `SceneInteractionPolish`의 scale 부분 중 하나: 동일 property 이중 writer. 최종 1.5/2.0 계약을 새 Location presenter에 통합해야 한다.
3. `MapViewportPolish`와 `SceneDialogueLayout`의 중복 geometry/typography/color/style 부분: property별 최종값을 하나의 presenter/style config로 합친 후 제거.
4. `SceneInteractionPolish.Update`의 ESC 처리: `EscapeLayerGuard`와 중복. 새 navigation authority로 교체 후 둘 다 제거 가능.
5. `UxEnhancer`의 target badge/Quest color/role normalization: controller/label component와 중복. view model 하나로 통합.
6. Controller legacy Quest tracker string writer: 새 Quest presenter가 확정되면 제거 가능.
7. `QuestOfferLabels`: Interaction view model이 처음부터 primary action + secondary quest metadata를 제공하면 사후 label rewrite 불필요.
8. `ProgressQuestsFromEvent`, `QuestStep.ProgressEvent*`: explicit QuestAction을 canonical로 승인하고 data migration이 끝난 뒤에만 legacy 후보.
9. `SetQuestStatus`: 현재 호출처 없는 private method.
10. `Choices.csv`/`ChoiceBeats.csv`의 builder asset load: 문서상 placeholder이며 runtime 미사용. authoring 정책 확인 후 generator에서 제외 후보이지만 파일/data 자체는 이번 범위에서 수정 금지.

## 9. What must be preserved exactly

### Gameplay/data semantics

- 21일/3주, 08:00~22:00, time-slot 경계, 수면 +2, max Will 5.
- 시작 총 Lv 6과 2/2/2 default UI, 확정 전 GAME_START 미실행.
- 6XP 즉시 level-up 및 overflow, affinity 0~5.
- Interaction/Event node traversal과 flow 종료 시 staged effect commit.
- Soft Requirement는 Will 보완, Hard Requirement는 불가. 정확 공식은 TEMP임을 유지.
- QuestRole OFFER/PROGRESS availability와 `?`/`!`, Main/Character/Side 색.
- QuestStep과 cross-content State의 의미 구분.
- Week 1: Benjamin awakening, 6인 재회 checklist, Benjamin Day 1 Character Quest, 조사 리듬, Day 7 다인 공개 선택.
- Day 15 hub 전환 및 Week 2/3 현재 authored hooks.
- NPC schedule priority와 repeat rules.

### UI/UX

- 불투명 Location viewport가 Quest/Log보다 위.
- 장소명 좌측 상단.
- target 자연 배치, invisible clipping boundary.
- browse 1.5x / target focus 2.0x와 focus pan.
- Interaction 선택 → narrative 전환 때 왼쪽 map composition 급변 금지.
- 오른쪽 Interaction/Narrative 공용 영역.
- target character 선택 및 해당 speaker dialogue portrait.
- ESC 한 단계씩: interaction selection → location; modal/event choice bypass 규칙 유지.
- Interaction primary text는 자연스러운 행동 문장, Quest 정보는 secondary.
- Quest hover 순서: 제목 → PlayerDescription → 현재 목표.
- Log는 수치/State 변화 확인용이며 narrative 중복을 늘리지 않음.
- People panel은 숨은 percentage/Motive/Fear/Identity 원문을 노출하지 않음.

### Migration invariants

- 기존 scene에서 검증하며 Generate Prototype 0.4를 일반 migration step으로 사용하지 않는다.
- authoring source는 workbook, runtime source는 Generated CSV라는 계약을 유지한다.
- 현재 TEMP/TBD를 리팩터링 편의로 확정값으로 바꾸지 않는다.

## 10. Proposed replacement architecture

### A. `Prototype04GameSession` / state authority

Day/time/will/stats/affinity/State/Quest/repeat/event occurrence만 소유한다. mutation command와 read-only snapshot/event를 공개한다. RectTransform, Text, Button을 모른다.

### B. `Prototype04ContentRepository`

CSV를 한 번 load/validate하고 typed definition과 `PlayerDescription`까지 제공한다. UI가 TextAsset을 재파싱하지 않는다. Interaction target type도 schema에서 명시하거나 validated resolver 하나로 제한한다.

### C. `Prototype04FlowRunner`

Interaction/Event의 node traversal, pending effect transaction, choice availability를 소유한다. UI에는 `FlowViewModel`과 commands만 제공한다. Quest mutation 정책은 사용자 결정 후 explicit `QuestAction` 단일 경로로 정규화하는 방향을 권장한다.

### D. `Prototype04LocationPresenter`

Location viewport geometry/color/order/clipping, target view pool, ID 기반 placement, pan/zoom을 단독 소유한다. scale writer는 이 클래스 하나이며 browse/focus 값은 config asset/serialized style에서 읽는다.

### E. `Prototype04NarrativePresenter`

오른쪽 Interaction/Narrative panel, modal typography/buttons, portrait를 단독 소유한다. Interaction selection과 narrative 상태는 동일 panel state machine의 명시적 mode로 표현한다.

### F. `Prototype04NavigationController`

`Hub`, `LocationBrowse`, `TargetSelected`, `Narrative`, `ForcedChoice`, `People`, `QuestDetail` 등 명시적 UI layer stack을 소유한다. Input action은 한 곳에서 `Back()` command로 변환하고 top layer가 consume한다. 컴포넌트 disable로 key 중복을 막지 않는다.

### G. `Prototype04QuestPresenter`

generic tracker/hover와 W1 reunion checklist renderer를 분리한다. gameplay Quest state를 mutate하지 않고 snapshot/event만 읽는다.

### H. `Prototype04LogPresenter`

structured state-change events를 표시 문자열로 바꾼다. narrative text 생성은 하지 않는다.

### I. Scene composition root

필요 presenter와 serialized references를 scene/prefab에 명시적으로 둔다. runtime bootstrap은 하나의 composition root로 제한하고, private Reflection 및 hierarchy-name lookup을 제거한다. edit-time scene과 play-time hierarchy 차이를 최소화한다.

## 11. Migration plan with rollback points

### Phase 0 — Baseline capture

- 사용자 로컬 Unity에서 current scene compile/play smoke test와 주요 화면 screenshot/video를 확보.
- 16:9 기준 location별 target 배치, browse/focus transition, Interaction→Narrative continuity, 각 ESC layer를 기록.
- Week 1 flow와 대표 Week 2/3 flow의 state snapshot을 저장.
- **Rollback:** 코드 변경 전 tag/commit + scene/data checksum.

### Phase 1 — Read-only public facade

- controller private state를 바꾸지 않고 typed read-only snapshot/API를 추가.
- Reflection reader를 facade로 한 컴포넌트씩 전환.
- **Rollback:** facade commit만 revert하면 기존 Reflection path 유지.

### Phase 2 — Navigation authority

- 명시적 UI mode/back stack을 도입하고 ESC/world/target click을 한 router로 이동.
- 결과 parity 후 EscapeLayerGuard, Polish ESC, UxEnhancer ESC를 비활성/제거.
- **Rollback:** old handler enable flag로 즉시 복귀 가능한 한 commit 단위 유지.

### Phase 3 — Location presentation authority

- scene/current runtime의 final geometry를 serialized presenter config로 옮김.
- target ID binding을 생성 시 직접 전달하고 text parsing 제거.
- pan/scale 단일 writer로 1.5/2.0 구현.
- parity 후 DensityOverride와 location 관련 patch writer를 제거.
- **Rollback:** 새 presenter와 old patch set을 feature toggle로 상호 배타 실행.

### Phase 4 — Narrative/Interaction/portrait authority

- right panel mode, button style, portrait ownership을 한 presenter로 이동.
- Quest action badge는 button view model의 secondary metadata로 통합.
- finalizer 제거.
- **Rollback:** 새 presenter disable 시 old patch set 복귀.

### Phase 5 — Quest/Log presentation

- typed quest view model에 PlayerDescription 포함.
- generic tracker와 W1 checklist renderer 분리.
- hidden legacy tracker writer와 CSV 재파싱 제거.
- **Rollback:** legacy Text를 다시 enable하는 단일 switch.

### Phase 6 — Gameplay/flow extraction

- pending effects, condition evaluation, trigger/event queue, Quest/State mutation을 순서대로 service로 추출.
- 매 추출 단계에서 before/after deterministic state trace 비교.
- **Rollback:** service별 작은 commit으로 controller inline path 복구 가능.

### Phase 7 — Quest mutation contract migration

- 사용자 결정 후에만 수행.
- explicit QuestAction canonical이면 모든 ProgressEvent dependency를 inventory하고 workbook에서 equivalent Event QuestAction으로 migration → export/validate → runtime fallback 제거.
- 기존 ProgressEvent semantics 유지 결정이면 문서 계약을 수정하고 두 방식의 precedence/validation을 명시.
- **Rollback:** workbook/CSV/code를 같은 migration commit으로 묶고 이전 data bundle로 복귀.

### Phase 8 — Builder alignment

- 현재 approved scene/presentation composition과 builder output을 일치시키거나, builder를 destructive recovery tool로 명확히 격리.
- Generate 결과를 별도 temporary scene에 만든 뒤 diff하는 검증을 추가하고 active scene 직접 overwrite 방지 검토.
- **Rollback:** 기존 builder 보존; production scene에는 실행하지 않음.

## 12. Regression checklist

### Boot/data

- [ ] Unity 6000.4.1f1 / URP compile/import 오류 없음.
- [ ] Prototype04 scene에서 runtime dependency가 중복 주입되지 않음.
- [ ] 모든 10개 runtime CSV load count와 reference validation 통과; placeholder 2개 정책 확인.
- [ ] stat allocation 전 controller Start/GAME_START가 진행되지 않음.
- [ ] 2/2/2 default, 합계 6 자유 배분, XP 0으로 시작.

### Core rules

- [ ] 행동 시작 시각 기준 Morning/Afternoon/Evening 경계 확인.
- [ ] 22:00 초과 행동 차단.
- [ ] sleep +2/max clamp, Day 15 hub switch, Day 21 TEMP summary.
- [ ] 6XP level-up/overflow/Lv20 cap.
- [ ] Soft requirement Will 보완과 Hard requirement 차단.
- [ ] ONCE/DAILY/UNLIMITED 및 schedule priority.

### Interaction/Event/Quest/State

- [ ] target별 available Interaction과 DIRECT_CLICK.
- [ ] flow cancel 전 effect 미적용, commit 후 effect 1회 적용.
- [ ] Event queue/forced choice/SILENT linear resolve.
- [ ] Quest OFFER 수락, PROGRESS current-step gate, `? → !`.
- [ ] 승인된 단일 Quest mutation policy에 따라 Step/complete가 정확히 1회 발생.
- [ ] STATE_CHANGE chain과 nested State event를 trace.
- [ ] Day 1 Benjamin offer만 먼저, Day 2 나머지 Character offer.
- [ ] 6인 reunion checklist의 이름별 State 및 완료 조건.
- [ ] 오아시스 조사 다중 경로와 Day 7 public/limited 양 분기.

### Location/UI

- [ ] 8 location title/background/target set.
- [ ] viewport 불투명, Quest/Log 위, target clipping.
- [ ] title 좌측 상단.
- [ ] browse 1.5x, focus 2.0x; selection 해제 시 원복.
- [ ] Interaction→Narrative 전환 시 왼쪽 map position/scale 급변 없음.
- [ ] character/object card ID와 label/portrait가 정확히 대응.
- [ ] natural action primary + Quest secondary metadata.
- [ ] modal long body/choice typography와 button overflow.
- [ ] dialogue speaker 및 target selection portrait visibility.
- [ ] Quest tracker/hover/checklist, People panel, log layering.
- [ ] 16:9 외 최소 16:10/ultrawide에서 anchors/clipping 확인.

### Input/navigation

- [ ] Hub world click opens 정확한 Location ID.
- [ ] UI click이 world raycast로 누출되지 않음.
- [ ] target click이 한 번만 selection을 바꿈.
- [ ] ESC: interaction selection만 닫음 → 다음 ESC location 닫음.
- [ ] uncommitted Interaction 취소, committed linear flow skip/finish semantics.
- [ ] forced Event choice는 ESC로 우회 불가.
- [ ] People/Quest detail 등 topmost overlay 우선 close.
- [ ] Back button/world-background click/ESC가 동일 navigation state를 유지.

### Generator safety

- [ ] active Prototype04 scene에서 Generate를 실행하지 않음.
- [ ] 별도 test scene 생성 결과가 approved composition과 동등한지 diff.
- [ ] generator가 workbook export/data 변경을 동반한다는 경고/rollback 확인.

## 13. Risks / unknowns / questions requiring user decision

1. **Quest mutation canonical policy:** 문서대로 explicit `QuestAction`만 남길지, `QuestStep.ProgressEventID/OnProgressEvent`를 합법 경로로 유지할지 결정이 필요하다. 권장은 explicit QuestAction 단일화지만 workbook migration 범위가 크다.
2. **Current visual golden baseline:** 정적 조사로 최종 frame의 실제 appearance와 callback 호출 순서까지 확정할 수 없다. 사용자 로컬 Unity screenshot/play trace가 필요하다.
3. **State trigger chaining:** nested State change가 실제 콘텐츠에서 누락 Event를 만드는지 play trace가 필요하다.
4. **Generator future role:** destructive recovery generator로 유지할지, current production scene과 동등한 composition generator로 갱신할지 결정이 필요하다.
5. **Quest `PlayerDescription` schema:** core content model에 정식 포함할지, presentation-only side table로 둘지 결정이 필요하다. 현재 UI 직접 CSV 재파싱은 유지하면 안 된다.
6. **Runtime dynamic UI policy:** People panel/W1 checklist처럼 승인된 특수 UI를 scene/prefab으로 직렬화할지 composition root가 명시 생성할지 선택이 필요하다. 어느 경우든 분산 bootstrap은 제거하는 방향이 안전하다.
7. **World click close behavior:** location open 중 UI 밖 아무 world click이 location을 닫는 현재 동작을 보존 계약으로 확정할지 확인이 필요하다.
8. **Committed flow ESC semantics:** controller는 committed linear remainder를 자동 stage/finish한다. 이것이 의도된 “skip”인지, 단순 다음 node 표시인지 UX 확인이 필요하다.
9. **People panel ESC precedence:** PeoplePanel도 독립 Update에서 ESC를 읽는다. 새 navigation stack에서 exact precedence를 golden baseline으로 확인해야 한다.
10. **TEMP 유지:** ending threshold, Week 3 greybox, stance/balance, Soft Requirement 공식은 이 리팩터링에서 확정하거나 재설계하지 않는다.

---

감사 결론: 리팩터링 필요성은 충분히 입증되었으나, 현재 기능 보존을 위해 먼저 Unity golden baseline과 Quest mutation 정책 승인이 필요하다. 이 보고서 이후 구현은 시작하지 않는다.
