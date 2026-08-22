# SandPlanet Agent Instructions

## Active target
- 현재 유일한 개발 대상은 **Prototype 0.4**다.
- Unity **6000.4.1f1 / URP**.
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`.
- 과거 Prototype 0.1~0.3 코드/씬/Generated 자산은 의도적으로 제거되었다. 복구하거나 다시 참조하지 않는다.

## Read first
작업 전 아래 순서로 읽는다.
1. `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
2. `docs/PROTOTYPE_0_4_RUN.md`
3. `docs/DESIGN_STATE.md`
4. `docs/PROTOTYPE_SCOPE.md`
5. `docs/UNITY_ARCHITECTURE.md`

## Source of truth
- Week 1 서사/시스템 합의: `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
- 현재 런타임/데이터 계약: `docs/PROTOTYPE_0_4_RUN.md`
- 현재 기획 상태: `docs/DESIGN_STATE.md`
- 현재 Prototype 범위: `docs/PROTOTYPE_SCOPE.md`
- 현재 코드 책임 경계: `docs/UNITY_ARCHITECTURE.md`
- Authoring source: `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`
- Generated CSV: `Assets/SandPlanet/Data/Generated/CSV/`

문서와 실제 코드가 충돌하면 임의로 새 패치 코드를 추가하지 말고 먼저 충돌 지점을 설명한다.

## Work classification before implementation
모든 요청은 구현 전에 아래 중 하나로 분류한다.
- **DATA/CONTENT**: 대사, 수치, 조건, Quest/Event/Interaction/State/Schedule 등 authoring 데이터로 해결 가능.
- **PRESENTATION**: 기존 UI/표현 authority 내부 변경.
- **GAMEPLAY RULE**: 기존 gameplay service의 규칙 변경.
- **NEW SYSTEM**: 기존 authority로 표현할 수 없는 신규 상태/규칙/흐름.

가능하면 가장 작은 기존 authority만 수정한다. DATA로 가능한 요청 때문에 새 MonoBehaviour나 하드코딩 분기를 만들지 않는다.

## Architecture ownership guardrails
### Gameplay/runtime
- `Prototype04GameState`: mutable runtime state의 유일한 저장소.
- `Prototype04ConditionEvaluator`: 조건 판정의 유일한 구현.
- `Prototype04QuestService`: Quest 상태/Step mutation 및 event progression authority.
- `Prototype04ScheduleService`: NPC 위치/schedule 판정 authority.
- `Prototype04InteractionService`: Interaction availability authority.
- `Prototype04EventService`: Event/Trigger/queue/STATE_CHANGE authority.
- `Prototype04FlowRuntime`: Interaction/Event flow, staged effects, commit authority.
- `SandPlanetPrototype04Controller`: 위 서비스를 Unity scene/presentation에 연결하는 coordinator. 새로운 gameplay 규칙의 만능 저장소로 사용하지 않는다.

### Presentation/navigation
- `Prototype04CompositionRoot`: runtime presentation dependency 조립.
- `Prototype04NavigationController`: UI mode와 Back/ESC의 유일한 authority.
- `Prototype04LocationPresenter`: Location viewport, target placement, clipping, pan/zoom의 유일한 authority.
- `Prototype04NarrativePresenter`: Interaction/Narrative panel, portrait, choice presentation authority.
- `Prototype04HudPresenter`: HUD/context/progress presentation authority.
- Quest tracker와 People panel은 gameplay state를 읽어 표현만 하며 상태 mutation을 소유하지 않는다.

### One-owner rule
- 같은 RectTransform, visibility, selected target, UI mode, flow state, Quest state에 복수 final writer를 두지 않는다.
- 기존 authority가 있으면 그 클래스/API를 확장한다. 새 `Polish`, `Finalizer`, `Override`, `Guard`, execution-order patch를 추가하지 않는다.
- Reflection으로 다른 component private state를 읽거나 UI text를 파싱해 identity/state를 역추론하지 않는다.
- `Update/LateUpdate/Canvas.willRenderCanvases/DefaultExecutionOrder` 경쟁으로 최종 상태를 결정하지 않는다.

## New-system rule
신규 시스템이 정말 필요하면 구현 전에 최소한 다음을 명시한다.
1. 소유 state가 무엇인지
2. 그 state의 단일 owner가 누구인지
3. 입력과 출력/event가 무엇인지
4. 기존 service/presenter 중 어디와 연결되는지
5. workbook/CSV 계약 변경 여부
6. 회귀 테스트 항목

이 항목이 정의되지 않은 상태에서 새로운 manager/helper/patch component를 추가하지 않는다.

## Scope control
- 요청받지 않은 시스템/NPC/퀘스트/UI를 추가하지 않는다.
- 확정과 TEMP/TBD를 구분한다.
- 구현 때문에 기획을 바꿔야 하면 먼저 영향과 선택지를 제시한다.
- 특정 캐릭터/Quest 전용 로직을 core manager/service에 하드코딩하지 않는다. 가능한 경우 authoring data로 표현한다.

## Narrative/data rules
- 동기 / 공포 / 정체성은 캐릭터 행동의 직접 원리다.
- Big5는 표현 참고값이며 기계적 결정식이 아니다.
- 플레이어가 직접 선택하는 행동은 Interaction Flow가 담당한다.
- 자동/강제 스토리는 Event Flow가 담당한다.
- Quest는 Main / Character / Side 진행 추적과 UI 역할을 한다.
- 현재 runtime은 explicit `QuestAction`과 기존 `ProgressEventID/OnProgressEvent`를 모두 지원한다. 둘 중 하나를 제거/통합하려면 authoring migration을 별도 승인받는다.
- Quest 상태를 임의의 Day/State 하드코딩으로 직접 변경하지 않는다.

## Current core rules
- 21일 / 3주.
- Week 1: D1~2 재회·상황 파악 → D3~6 오아시스 조사·보고 → D7 공개 범위 선택.
- Week 2: 폭풍 대비, 수송선 생존 준비, 정착지/오아시스/묘지/수송선 외부 보존 선택.
- Week 3: 수송선 내부 4구역, 폭풍 생존, 관측/미래 대화, D21 결말.
- 기본 행동 가능 시간 08:00~22:00.
- 시간대: Morning 06:00~11:59 / Afternoon 12:00~16:59 / Evening 17:00~22:00.
- 최대 의지 기본 5, 수면 +2, 휴식 3시간 → +1.
- 개인 / 대인 / 기술: Lv + XP, 시작 Lv 합계 6, 6XP마다 즉시 Lv +1.
- 호감도 0~5, 설득과 별개.
- Soft Requirement는 의지로 보완 가능, Hard Requirement는 불가.

## Current UI contract
- Location viewport는 불투명하고 Quest/Log보다 위에 표시.
- 장소명은 좌측 상단.
- 캐릭터/사물은 viewport 안에 자연 배치하고 invisible clipping boundary를 사용.
- 기본 map browse 배율 1.5x, target focus 2.0x.
- Interaction 선택 → dialogue 전환에서 장소 배치가 재정렬되거나 급변하면 안 된다.
- 우측 panel은 Interaction 선택과 Narrative를 담당한다.
- ESC는 가장 위 UI 계층부터 한 단계씩 닫는다.
- 캐릭터 초상은 Interaction 선택 및 해당 캐릭터 대화에서 표시한다.
- Interaction 버튼의 주 문구는 자연스러운 행동 표현이며 Quest 정보는 보조 정보다.

## Data workflow
일반 authoring 수정:
1. `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
2. `Validate Generated CSV`
3. 기존 Prototype04 scene에서 테스트

`Tools > SandPlanet > Generate Prototype 0.4`는 scene 구조 자체를 재생성해야 할 때만 사용한다. 일반 기획/데이터/UI 수정에서는 실행하지 않는다.

## Completion
작업 완료 시 다음을 짧게 보고한다.
- 변경한 데이터/authority
- 새 owner 또는 새 state를 만들었는지 여부
- 테스트 방법
- 남은 TEMP/TBD
- 로컬 Unity 검증 필요 여부
