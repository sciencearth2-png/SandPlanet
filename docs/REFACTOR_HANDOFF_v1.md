# SandPlanet Prototype 0.4 — Refactor Handoff v1

> 작성일: 2026-08-22
> 대상 브랜치: `prototype-foundation`
> 작성 기준 HEAD: `9b46ce2eb595ff3cb3a70c841bc53dfe98281087`
> 목적: 현재 Prototype 0.4의 기능을 유지하면서 코드 전수조사 및 향후 구조 재작성(refactor/replacement)을 안전하게 수행하기 위한 최신 인수인계 문서.

## 0. 가장 중요한 작업 규칙

현재 사용자가 원하는 순서는 다음과 같다.

1. **코드 전수조사**
2. 조사 결과를 사용자와 검토
3. 새 구조 설계 승인
4. 그 다음에만 실제 코드 교체/리팩터링

따라서 **첫 Codex 작업에서는 게임 코드, 데이터, 씬을 수정하지 않는다.**
첫 작업에서 허용되는 저장소 변경은 조사 보고서 문서 작성뿐이다.

현재 문제를 보고 임의로 고치기 시작하지 말 것. 먼저 실제 기능, 호출 흐름, 중복 책임, 충돌 지점을 증명해야 한다.

---

## 1. Source of truth 우선순위

이번 전수조사/리팩터링에서는 아래 순서로 참고한다.

1. `docs/REFACTOR_HANDOFF_v1.md` — 현재 리팩터링 목적과 최신 UX 보존 계약
2. `docs/W1_NARRATIVE_SYSTEM_v1_7.md` — Week 1 내러티브/시스템 합의
3. `docs/PROTOTYPE_0_4_RUN.md` — 현재 Prototype 0.4 데이터/런타임 계약
4. `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx` 및 `Assets/SandPlanet/Data/Generated/CSV/`
5. 실제 `Assets/SandPlanet/Runtime/Prototype/` 코드와 현재 scene wiring
6. `docs/DESIGN_STATE.md`, `docs/PROTOTYPE_SCOPE.md`, `docs/UNITY_ARCHITECTURE.md` — 배경 설계 문서

`AGENTS.md`와 일부 과거 문서는 Prototype 0.3 시절 문구를 포함할 수 있다. **현재 0.4와 충돌하면 이 Handoff와 `PROTOTYPE_0_4_RUN.md`를 우선하고, 충돌 사실을 보고서에 명시한다.**

확정되지 않은 사양을 임의로 보정하거나 일반적인 게임 개발 상식으로 덮어쓰지 않는다.

---

## 2. 프로젝트 현재 상태

- Repository: `sciencearth2-png/SandPlanet`
- Active branch: `prototype-foundation`
- Unity: **6000.4.1f1 / URP**
- Active prototype: **Prototype 0.4**
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`
- 장르/표현: **3D 디오라마형 허브 + 2D 장소/내러티브 UI**
- 이동: WASD 이동이 아니라 3D 허브에서 장소를 클릭해 진입하고, 장소 안에서는 캐릭터/사물 UI를 클릭한다.
- 기간: 21일, Week 3(Day 15+)는 수송선 내부 4구역 허브로 전환한다.

### 매우 중요한 Unity 작업 규칙

일반 데이터/UI/런타임 코드 변경 때문에 아래 메뉴를 실행하지 않는다.

`Tools > SandPlanet > Generate Prototype 0.4`

이 메뉴는 generated scene 구조를 다시 만들 수 있어 기존 scene 수정 내용을 덮어쓸 위험이 있다.

일반 authoring 데이터 반영은:

1. `SandPlanet_Master.xlsx` 교체/저장
2. `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
3. `Tools > SandPlanet > Data v1.6 > Validate Generated CSV`
4. 기존 `SandPlanet_Prototype_04.unity`에서 테스트

순서를 사용한다.

첫 전수조사에서는 위 메뉴들을 실행할 필요가 없다.

---

## 3. 현재 데이터 계약 — 가능한 한 보존 대상

Authoring source:

`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Runtime generated CSV:

`Assets/SandPlanet/Data/Generated/CSV/`

주요 시트/CSV:

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

최근 Quest UI를 위해 `Quests.csv`에 `PlayerDescription`이 추가되어 있다. Audit 시 Master와 Generated CSV 사이에 차이가 발견되면 **조용히 동기화하지 말고 차이로 보고**한다.

### 핵심 모델

- 플레이어가 캐릭터/사물/장소 대상을 클릭하면 Interaction 후보가 열린다.
- `06_상호작용 플로우` 한 Node의 여러 행 = 여러 선택지.
- Interaction 결과가 Time / Will / 3 XP / affinity / State / Event / QuestAction을 적용할 수 있다.
- Event Flow도 같은 Node 기반 narrative/dialogue/choice 구조를 사용한다.
- Event Trigger는 Event 발생 시점을 결정한다.
- Quest 상태는 단순 Day/State 조건만으로 자동 변경하지 않는다.
- Quest 상태 변화는 Interaction/Event 결과의 `QuestAction`을 통해 일어난다.

QuestRole:

- `NONE`: 일반 상호작용
- `OFFER`: LOCKED Quest 제안
- `PROGRESS`: ACTIVE Quest의 현재 Step 진행

QuestAction:

- `ACTIVATE_QUEST`
- `SET_QUEST_STEP`
- `COMPLETE_QUEST`
- `FAIL_QUEST`
- `NONE`

QuestStep은 해당 Quest 안의 진행도이며, State는 Quest 종료 후에도 다른 콘텐츠가 기억해야 하는 세계/인물 사실이다.

이 데이터 구조는 현재 프로젝트에서 비교적 안정적인 기반으로 보고 있으며, **UI 코드가 스파게티라는 이유만으로 데이터 모델까지 처음부터 다시 만들지 않는다.** Audit이 필요성을 증명할 경우에만 제안한다.

---

## 4. 반드시 보존해야 할 게임 기능

전수조사 보고서는 아래 기능 각각의 실제 담당 파일과 호출 흐름을 찾아야 한다.

### Core runtime

- 21일 시간 진행
- 행동 TimeCost
- Morning/Afternoon/Evening 판정
- Sleep / day advance
- Will 최대치 및 회복
- Personal / Social / Technical Lv + XP
- 6 XP마다 즉시 Lv +1 및 초과 XP 이월
- Soft Requirement의 Will 대체
- Hard Requirement
- affinity
- State/Flag
- Interaction Flow
- Event Flow / Event Trigger
- Quest / QuestStep / QuestAction
- ONCE / DAILY / UNLIMITED
- NPC schedule
- Day 15 planet hub → ship interior hub 전환
- Day 21 결과/요약 관련 현재 동작

### Player-facing UI/runtime features

- 게임 시작 전 3스탯 총 Lv6 배분 화면
- GAME_START에서 벤자민이 제이를 깨우는 도입
- People Panel: 초상/역할/현재까지 알게 된 정보/호감도/현재 미래 입장 등
- Quest tracker
- Quest type은 색으로 구분하고 불필요한 `MAIN/CHAR/SIDE` 텍스트를 반복하지 않음
- Quest hover detail: `PlayerDescription` + 현재 목표
- W1 Main `내가 잠든 동안` 전용 6인 이름 체크리스트
- Quest OFFER/PROGRESS 표시 `? / !`
- 상호작용 목록의 본문은 **실제 행동/대사 문장**이어야 함
- 퀘스트 관련 상호작용은 실제 행동 문장 아래 작은 보조 정보로 `? 퀘스트명` / `! 퀘스트명`을 표시
- `[퀘스트명] 수락/진행/완료` 같은 시스템 결과 표현은 narrative choice의 작은 결과 chip에는 사용 가능하지만, 실제 행동 문장을 대체하면 안 됨
- 좌하단 결과 로그: 시간/Will/XP/Lv/호감도/QuestAction/Event 등 시스템 결과 확인용
- `보리치와 다시 만났다`, `벤자민이 잃은 가족의 이름을 알게 됐다` 같은 **서술형 State 설명 알림은 플레이어 로그에 불필요**. 전후 맥락은 dialogue가 전달해야 함
- Portrait 표시
- 일반 Interaction과 Event는 동일한 우측 narrative/dialogue presentation 원칙 사용

---

## 5. 최신 장소/내러티브 UX 보존 계약

최근 반복 수정에서 가장 혼선이 많았던 부분이다. 리팩터링 시 아래를 **의도된 동작**으로 취급한다.

### 장소 화면

- 장소 진입 시 큰 Location viewport가 화면 왼쪽/중앙을 차지한다.
- Location UI는 **불투명**하다.
- Location UI는 좌측 상단 Quest UI와 좌측 하단 Log UI보다 위에 렌더링된다.
- 장소명(`거주지`, `묘지`, `오아시스`, `수송선...`)은 맵 중앙이 아니라 **viewport 좌측 상단**에 표시한다.
- 화면에 `인물`, `사물` 접두어를 붙이지 않는다.
- 캐릭터는 초상+이름, 사물은 간결한 표식으로 맵 내부에 배치한다.
- 사물을 하단 한 줄에 고정하지 않는다. 캐릭터/사물 모두 장면 안에 자연스럽게 분산 배치한다.
- 각 장소는 채도가 낮고 어두운 서로 다른 임시 배경 톤을 사용한다. 쨍한 색 금지.
- Location viewport에는 **보이지 않는 clipping boundary**가 있다. 확대/이동으로 캐릭터 일부가 경계 밖으로 나가면 밖의 부분은 보이지 않는다. 실제 빨간 테두리 같은 시각적 선은 없다.
- 최초 browse 상태의 캐릭터/사물은 경계 밖에서 시작하면 안 된다.

### 현재 승인된 맵 밀도/줌 기준

- 최신 의도: 재배치된 캐릭터/사물 composition을 첫 장소 프레임부터 사용한다.
- 기본 Browse scale: **1.5x**
- 대상 클릭 Focus scale: **2.0x**
- Focus 시 선택 대상 쪽으로 가벼운 pan/zoom 연출
- Interaction 선택 화면에서 실제 dialogue/narrative로 넘어갈 때 **장소 배치가 새로 재정렬되거나 갑자기 다른 크기로 바뀌면 안 된다.** 선택한 대상에 이미 focus된 상태라면 그 focus composition을 유지한다.
- Interaction이 끝나거나 선택을 취소하면 Browse 1.5x composition으로 복귀한다.

중요: 최근 대화에서 `2번 화면`이라고 표현된 것은 숫자 배율 2.0을 의미한 것이 아니라 **요청한 재배치가 제대로 적용된 화면**을 뜻했다. 숫자 2.0을 기본 BrowseScale로 올리는 것은 잘못된 해석이었고 현재 1.5로 되돌렸다.

### 우측 Interaction / Narrative UI

- 캐릭터/사물 클릭 시 우측에 Interaction 선택 UI가 열린다.
- 일반 dialogue와 Event dialogue도 같은 우측 영역에서 전개한다.
- narrative title/body/choice 글자는 최근 가독성 개선값을 유지한다(대략 title 25pt, body 19pt, choice 17pt 수준; audit에서 실제 최종 writer를 확인할 것).
- 캐릭터 대화 시 초상은 우측 narrative panel 왼쪽에 살짝 튀어나오는 Disco Elysium 유사 배치.
- 초상은 이전보다 약 1.5배 크게 표시.
- 캐릭터를 클릭해 Interaction 선택지를 고르는 단계에서도 캐릭터 초상을 표시한다.
- 사물은 당장은 초상 불필요.

### ESC hierarchy

의도된 동작:

1. 장소 + 우측 Interaction 선택 UI가 동시에 열려 있으면 첫 ESC는 **우측 Interaction UI만 닫는다. 장소는 유지한다.**
2. 그 다음 ESC에서 장소 UI를 닫을 수 있다.
3. dialogue/event modal의 ESC 규칙은 실제 현재 동작을 audit해 별도로 명확히 문서화한다.

ESC 처리 권한이 여러 스크립트에 나뉘는 것은 리팩터링 주요 개선 후보다.

---

## 6. Week 1에서 코드가 알아야 할 특별 UX/콘텐츠

`docs/W1_NARRATIVE_SYSTEM_v1_7.md`를 자세히 읽을 것.

특히:

- Jay는 지질학자이며 시작 스탯 배분은 과거 직업을 재작성하지 않는다.
- Benjamin이 Jay를 깨운다.
- Day 1~2 Main `내가 잠든 동안`은 핵심 동료 6인 재회.
- 이 Main만 특수 이름 체크리스트를 사용한다.
- Day 1에는 Benjamin Character Quest `남겨진 이름들`이 먼저 열린다.
- Benjamin의 아내와 아이는 불시착 당시 사망한 설정이 최신 합의다.
- Day 3~6 Oasis investigation은 관찰 → 가설 → 확인 구조.
- 결론은 `행성에서 절대 못 산다`가 아니라 `현재 오아시스와 현재 생활 방식으로는 장기 거주를 보장할 수 없다`.
- Day 7은 public/limited disclosure를 선택하는 다자간 대형 Event.

리팩터링은 이 콘텐츠의 의미를 바꾸는 작업이 아니다.

---

## 7. 현재 의심되는 기술 부채 — Audit에서 증명할 것

최근 UI 수정 과정에서 단일 책임이 아닌 **런타임 patch/polish/override 계층이 누적**되었다.

현재 확인된 예시는 최소 다음과 같다.

- `SandPlanetPrototype04Controller.cs`
- `SandPlanetPrototype04UxEnhancer.cs`
- `SandPlanetPrototype04InputBridge.cs`
- `SandPlanetPrototype04QuestActionBadge.cs`
- `SandPlanetPrototype04OpeningSetup.cs`
- `SandPlanetPrototype04PeoplePanel.cs`
- `SandPlanetPrototype04W1QuestTracker.cs`
- `SandPlanetPrototype04SceneDialogueLayout.cs`
- `SandPlanetPrototype04SceneInteractionPolish.cs`
- `SandPlanetPrototype04SceneInteractionFinalizer.cs`
- `SandPlanetPrototype04SceneDensityOverride.cs`
- `SandPlanetPrototype04MapViewportPolish.cs`

이 목록은 **완전한 목록이 아니다.** Codex가 Runtime/Prototype 전체를 조사해야 한다.

특히 다음 패턴을 전수조사한다.

- 동일한 `RectTransform` / `GameObject` / `Text` / `Image`를 여러 MonoBehaviour가 변경
- `RuntimeInitializeOnLoadMethod`로 자동 부착되는 patch component
- `DefaultExecutionOrder` 경쟁
- `Update`, `LateUpdate`, `Canvas.willRenderCanvases`에서 매 프레임 geometry를 재assert
- Reflection으로 `SandPlanetPrototype04Controller` private field를 읽거나 private method를 호출
- ESC/input 권한 분산
- scale/pan/layout 권한 분산
- UI 표시 상태와 gameplay state가 서로 간접 추론으로 연결
- legacy Prototype02/03 코드가 현재 0.4 scene/runtime에 실제로 연결되는지 여부
- dead code / superseded patch / duplicate responsibility

**단순히 파일 수가 많다는 이유로 스파게티라고 결론내리지 말고, 누가 같은 상태를 쓰는지 실제 writer/read dependency로 증명한다.**

---

## 8. 리팩터링의 목표 방향 — 아직 승인된 구현안은 아님

첫 Audit 이후 제안할 target architecture는 아래 원칙을 우선 검토한다.

- Game rules/data runtime과 UI presentation 분리
- UI geometry/layout의 owner를 화면별로 하나로 제한
- Location viewport + target layout + focus camera/pan을 하나의 명시적 presenter/controller로 통합
- Interaction/dialogue/event presentation의 상태 전환을 하나의 coordinator가 관리
- ESC/back navigation을 하나의 stack/router에서 관리
- Quest tracker/hover/checklist는 하나의 Quest UI presenter 아래로 통합
- 가능하면 매 프레임 값 덮어쓰기를 없애고 state change/event 기반 refresh
- Reflection private-field coupling 제거 또는 최소화하고 명시적 API/read-only state 제공
- Opening/People/DebugLog 같이 독립 기능은 독립 view로 유지 가능하되 핵심 Controller private state를 서로 추측하지 않게 함
- 데이터 모델/CSV authoring contract는 가능한 한 유지
- 기존 scene를 재생성하지 않고 교체 가능한 구조 우선

하지만 **첫 Audit에서 이 구조로 곧바로 구현하지 않는다.** 실제 dependency 조사 후 더 적절한 안이 있으면 제안한다.

---

## 9. 첫 Codex Audit 산출물 요구사항

첫 작업 결과는 `docs/REFACTOR_AUDIT_REPORT_v1.md` 하나로 작성한다.

보고서에 반드시 포함:

1. **파일 인벤토리**
   - `Assets/SandPlanet/Runtime/Prototype/**/*.cs`
   - 관련 `Assets/SandPlanet/Editor/**/*.cs`
   - Prototype04 scene/build/export 관련 파일
   - 각 파일: active / legacy / uncertain 구분

2. **기능 → 담당 코드 매핑**
   - 시간, Will, XP/Lv, conditions, interaction, event, quest, state, schedule, hub switch, input, logs, opening setup, people panel, quest UI, location UI, narrative UI, portrait, ESC, zoom/pan

3. **런타임 호출/상태 흐름**
   - Excel/CSV → parser/content → runtime state → interaction/event → UI
   - 클릭 → target → interaction list → flow → result → refresh

4. **동일 상태/동일 UI writer 표**
   - 예: `targetRoot.localScale`을 쓰는 모든 코드
   - `targetRoot.anchoredPosition`
   - `locationPanel` anchors/sibling/color
   - modal geometry/fonts
   - interactionRoot dynamic buttons
   - ESC
   - quest tracker text/UI

5. **Execution-order / lifecycle audit**
   - RuntimeInitializeOnLoadMethod
   - DefaultExecutionOrder
   - Awake/Start/Update/LateUpdate
   - Canvas.willRenderCanvases
   - 서로의 최종값을 덮는 순서

6. **Reflection coupling audit**
   - private field/method 접근 목록과 이유

7. **Legacy/dead/superseded 후보**
   - 삭제 여부를 결정하지 말고 근거와 위험도만 제시

8. **현재 기능 보존 계약과 실제 코드의 불일치**
   - 이 Handoff의 intended behavior와 현재 code behavior 차이

9. **리스크 등급**
   - High / Medium / Low
   - 어떤 수정이 어떤 기능을 깨뜨릴 가능성이 있는지

10. **권장 target architecture 1안 + 대안 1안**
    - 파일/클래스 책임 수준까지
    - 기존 데이터 모델 유지 여부
    - 단계별 migration 순서

11. **예상 리팩터링 범위/시간**
    - 단계별

12. **사용자에게 결정 받아야 할 질문**
    - 코드 조사만으로 확정할 수 없는 것만

---

## 10. 첫 Audit에서 하지 말 것

- `.cs` gameplay/runtime/editor 파일 수정 금지
- `.unity`, prefab, `.meta`, workbook, CSV 수정 금지
- 새 UI 구현 금지
- 기존 patch 삭제 금지
- `Generate Prototype 0.4` 실행 금지
- 발견한 bug를 즉석에서 수정 금지
- 데이터 구조 재설계 금지
- 사용자의 UX 의도를 코드의 현재 우연한 동작으로 대체 금지

첫 작업의 성공 기준은 **코드를 더 깨끗하게 만드는 것**이 아니라, **현재 게임이 어떻게 작동하고 왜 수정 충돌이 발생하는지를 재현 가능한 문서로 설명하는 것**이다.
