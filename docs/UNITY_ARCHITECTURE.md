# SandPlanet Unity Architecture

> 상태: **기반 아키텍처 가안**. 확정된 게임 규칙을 Unity 3D에서 버리지 않고 확장하기 위한 최소 구조다.

## 현재 기술 기준
- Unity: **6000.4.1f1**
- 렌더 파이프라인: **URP**
- 표현: **3D 디오라마형 허브 + 2D 장소/Encounter UI**
- WASD 직접 이동형 3D 어드벤처가 아니다.

## 목표
- 0.x 프로토타입을 버리지 않고 21일 게임으로 확장한다.
- 콘텐츠 추가가 핵심 코드 수정으로 이어지지 않게 한다.
- 현재 Game State를 평가해 `지금 이 장소에서 가능한 Encounter Pool`을 구성한다.
- Excel 기반 Encounter/Choice/Condition/Action 데이터로 이후 이전 가능한 구조를 우선한다.

## 화면/플레이 계층
### Week 1~2: 3D 행성 허브
핵심 장소:
- ship
- graveyard
- settlement
- oasis

랜드마크는 Location ID만 가진 얇은 컴포넌트로 유지한다.

### 장소 화면
`3D 행성 허브 → 장소 → 현재 유효한 Encounter 카드`

장소가 Encounter를 직접 고정 소유하기보다 Encounter Resolver가 현재 상태를 평가해 목록을 만든다.

### 2D Encounter 화면
대표 표현:
- 장소/상황 비주얼
- Primary NPC 포트레이트
- Narrative Type 비주얼 마커
- 상황 텍스트
- Choice 목록
- 시간/의지 비용
- Soft/Hard Requirement
- 결과 피드백

### 수송선 정비 화면
플레이어에게 트리가 아니라 실제 수송선 부위/도면을 수리하는 감각으로 표현한다.

### Week 3: 수송선 내부 허브
같은 `허브 → 구역 → Encounter` 문법을 재사용한다.

## 데이터 / 상태 계층
### 1. Static Definitions
플레이 중 변하지 않는 콘텐츠 정의.

최종 후보:
- `CharacterDefinition`
- `LocationDefinition`
- `EncounterDefinition`
- `ChoiceDefinition`
- `EventDefinition`
- `SkillDefinition`
- `ShipFacilityDefinition`

`QuestDefinition`은 현재 필수 구조로 두지 않는다. Main/Character/Activity는 Encounter의 분류/태그로 처리한다.

Prototype 0.3에서는 C# 직렬화 데이터로 먼저 검증할 수 있으나, 특정 콘텐츠 전용 로직이 아니라 정의 데이터를 추가하는 방식이어야 한다.

### 2. Runtime State
세이브에 들어갈 현재 상태 후보:

#### GameState
- Day
- CurrentHour
- Week/Phase
- Global Flags
- Main Scenario State

#### PlayerState
- Personal Lv / XP
- Interpersonal Lv / XP
- Technical Lv / XP
- Current / Max Willpower
- SP / Perks (향후)

#### CharacterState
- Affinity 0~5
- Companion 여부
- Character Flags
- 설득/개인사 상태

#### ShipState
- 시설별 0~3 상태
- 파손/보강 상태

기존 `DailyTrendState`는 폐기한다.

### 3. State / Flag registry
Encounter/Choice/Event 조건과 Action에서 사용하는 상태는 가능한 한 ID로 관리한다.

예:
- `MEMORIAL_DONE`
- `W1_INVESTIGATION_OPEN`
- `WATER_RISK_KNOWN`
- `W1_REPORT_DONE`
- `W1_DISCLOSURE`

Prototype에서는 enum/bool로 단순화할 수 있지만, 최종 데이터 구조가 특정 C# 분기문에 묶이지 않게 한다.

## Core Systems
- `TimeSystem`: 시간 소비, Day 경계, Morning/Afternoon/Evening 시작 시간 판정.
- `WillpowerSystem`: 소비/회복/최대치, 3시간 휴식, 수면 +2.
- `GrowthSystem`: 개인/대인/기술 XP 및 6XP 레벨업.
- `RequirementSystem`: Soft / Hard Requirement 평가, TEMP 의지 강행 공식.
- `EncounterSystem`: 현재 상태에 맞는 Encounter 필터링, Choice 실행.
- `EventSystem`: 조건 충족 시 자동/강제 Event 호출.
- `ActionSystem`: XP/호감도/플래그/시설/세계 상태 등 결과 적용.
- `RelationshipSystem`: 호감도/동료/인물 플래그.
- `ShipSystem`: 시설 상태/수리.
- `SkillSystem`: 향후 시간 범위/비용/기존 규칙 변형.

Prototype 0.3에서는 이들을 한 Controller 안의 작은 모듈성 메서드로 검증할 수 있다. 단, 콘텐츠 ID별 거대한 switch를 최종 해법으로 만들지 않는다.

## Encounter Definition 책임
최소 표현 항목:
- ID
- Variant Group
- Narrative Type: Main / Character / Activity / World 등
- Visual Priority
- Scope: Global / Location
- Location ID
- Primary NPC ID
- Week / Open Day / Close Day
- Allowed Time Slots
- Repeat Rule
- Show Conditions
- Hide Conditions
- Title / Body
- Choice Definitions

### Variant
같은 기계적 효과라도 문맥이 달라지면 별도 Encounter로 둘 수 있다.

예:
- `GRAVE_CLEAN_PRE_MEMORIAL`
- `GRAVE_CLEAN_POST_MEMORIAL`

둘 다 Personal XP +1일 수 있지만 Condition과 텍스트가 다르다.

## Choice Definition 책임
- ID
- 표시 문구
- Time Cost
- Base Will Cost
- Soft Stat / Required Lv
- Hard Conditions
- Result Text
- Actions

### Soft Requirement
개인/대인/기술처럼 의지로 부족분을 보완할 수 있는 요구.

Prototype 0.3 TEMP 공식 후보:
- 부족 Lv 1당 의지 1.
- 현재 의지가 부족하면 선택 불가.

최종 공식은 플레이 테스트 후 재검토한다.

### Hard Requirement
예:
- 특정 Flag
- Affinity
- 특정 NPC/Companion
- Ship Facility State
- 다른 Encounter 결과

의지로 우회하지 못한다.

## Action 책임
Choice/Event 결과는 작은 Action들의 조합으로 표현하는 방향을 우선한다.

후보:
- `ADD_XP`
- `ADD_AFFINITY`
- `SET_FLAG`
- `SET_SCENARIO_STATE`
- `SET_COMPANION`
- `ADD_FACILITY_STATE`
- `SET_FACILITY_STATE`

일반 선택의 Time/Will 비용은 Choice의 가시 필드로 두고, 상태 변화는 Action으로 관리하는 방식이 기획 테이블 가독성에 유리하다.

## Event Definition 책임
Event는 플레이어가 지도에서 고르는 Encounter와 진입 방식만 다르다.

최소 항목:
- ID
- Trigger 조건
- Once/Repeat
- Title / Body
- Choice 또는 자동 Actions

대표 0.3 Event:
- DAY 1 기상.
- DAY 3 조사 단계 개방.
- DAY 7 종료 시 공개 범위 선택.

## Dynamic Encounter Pool
장소 진입 또는 Hub 상태 갱신 시 다음을 평가한다.

1. Scope / Location 일치.
2. Week / Day 범위.
3. 현재 시각이 허용 Time Slot에 해당.
4. Repeat Rule.
5. Show Conditions 충족.
6. Hide Conditions 미충족.
7. 다른 Hard availability 조건 충족.

그 결과만 카드로 표시한다.

따라서 Main 진행에 따라 일반 생활/캐릭터 Encounter도 자연스럽게 추가·삭제·Variant 변경될 수 있다.

## Growth rules for 0.3
- 세 능력치 각각 `Lv + XP`.
- 시작 Lv 합계 6.
- 6XP마다 즉시 Lv +1.
- Lv 상한 20.
- 기존 Trend enum/누적/EndDay 성장 로직 제거.
- HUD에서 `Lv / XP`를 상시 표시한다.

## Time / Will rules for 0.3
- 기본 활동 범위 08:00~22:00.
- Time Slot:
  - Morning 06:00~11:59
  - Afternoon 12:00~16:59
  - Evening 17:00~22:00
- 판정은 Choice/Encounter 시작 시각 기준.
- 휴식 3시간 → 의지 +1.
- 수면 후 +2.
- 향후 SkillSystem이 시작/종료 시간을 07→06, 23→24로 바꿀 수 있도록 시간 상수를 한 곳에 둔다.

## Portrait architecture
현재 PNG:
- 샘
- 지나
- 페이
- 벤자민
- 보리치
- 디야

Prototype 0.3 Builder가 PNG TextureImporter를 Sprite로 맞춘 뒤 Controller의 Character ID ↔ Sprite 매핑에 연결하는 방식을 사용할 수 있다.

Primary NPC가 없으면 포트레이트 영역은 장소/상황 비주얼로 남긴다.

## Prototype version isolation
- 0.1 / 0.2 코드는 회귀 확인용으로 유지한다.
- 0.3은 별도 `SandPlanetPrototype03Controller` / `SandPlanetPrototype03Builder` / Scene으로 추가한다.
- 기존 프로토타입 규칙을 최신 기획으로 억지로 수정하지 않는다.

## 아직 결정하지 않는 것
- 최종 ScriptableObject/CSV/Excel Import 파이프라인.
- 최종 세이브 포맷.
- 완성 UI Toolkit 전환 여부.
- 최종 포트레이트 연출/표정 시스템.
- 전체 Week 1 Encounter 데이터.
- 최종 Soft Requirement 공식.
- Addressables.
