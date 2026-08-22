# SandPlanet Unity Architecture — Prototype 0.4

> 현재 구현을 설명하고 리팩터링 목표 경계를 정의하는 문서. 세부 구현은 audit 후 확정한다.

## 기술 기준
- Unity 6000.4.1f1 / URP
- 3D 디오라마형 허브 + 2D 장소/내러티브 UI
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`
- Authoring: Excel → Generated CSV

## 현재 데이터 계층
### Static content
`SandPlanetContent04`가 Generated CSV를 읽어 다음 정의를 제공한다.
- Location
- Character
- WorldTarget
- Quest / QuestStep
- Interaction / InteractionFlow / FlowNode
- State definition
- EventFlow / EventTrigger
- NPC Schedule

### Runtime state
현재 `SandPlanetPrototype04Controller`가 다음 상태를 보유한다.
- Day / Hour / Will
- Personal / Social / Technical Lv + XP
- States
- Affinity
- QuestStatus / QuestStep
- occurred/used/last-day tracking
- active Interaction/Event flow
- current location / modal state

## 현재 플레이 흐름
1. 3D 허브의 `PrototypeLocationNode` 클릭.
2. `SandPlanetPrototype04InputBridge`가 Location ID를 Controller로 전달.
3. Controller가 LocationPanel과 현재 캐릭터/사물을 구성.
4. Target 선택 후 가능한 Interaction 목록 구성.
5. Interaction Flow 또는 Event Flow가 동일한 narrative node UI를 사용.
6. 결과가 Time / Will / XP / Affinity / State / QuestAction을 적용.
7. 상태 변화에 따라 target/quest/event가 갱신.

## 현재 UI 계층
핵심 UI 객체:
- HUD
- Quest tracker/detail
- Log
- Location viewport
- TargetRoot
- Interaction panel/root
- Narrative modal/root
- Speaker portrait

최근 시각/입력 보정을 위해 여러 0.4 컴포넌트가 추가되어 있으며, 일부가 같은 객체를 다시 쓴다. 이것은 audit의 핵심 대상이다.

현재 관련 클래스 예:
- `SandPlanetPrototype04Controller`
- `SandPlanetPrototype04InputBridge`
- `SandPlanetPrototype04UxEnhancer`
- `SandPlanetPrototype04SceneDialogueLayout`
- `SandPlanetPrototype04SceneInteractionPolish`
- `SandPlanetPrototype04SceneInteractionFinalizer`
- `SandPlanetPrototype04SceneDensityOverride`
- `SandPlanetPrototype04MapViewportPolish`
- `SandPlanetPrototype04EscapeLayerGuard`
- `SandPlanetPrototype04OpeningSetup`
- `SandPlanetPrototype04PeoplePanel`
- `SandPlanetPrototype04W1QuestTracker`
- `SandPlanetPrototype04QuestOfferLabels`
- `SandPlanetPrototype04QuestActionBadge`

## 리팩터링 목표 경계
Audit 결과가 확인되면 다음 책임 분리를 우선 검토한다.

### 1. Game/Flow state authority
시간, 의지, 성장, State, Quest, Event/Interaction 진행의 유일한 권한.
UI RectTransform/색/폰트 같은 presentation 값은 소유하지 않는다.

### 2. Location presentation authority
Location viewport, 장소 배경, title, target placement, clipping, focus pan/zoom을 한 계층에서 소유한다.
같은 `targetRoot` transform을 여러 LateUpdate writer가 동시에 쓰지 않는다.

### 3. Narrative/Interaction presentation authority
우측 Interaction 선택과 Narrative modal, portrait, 버튼 스타일/크기를 한 계층에서 소유한다.

### 4. Navigation/input authority
World click, Target click, Back/ESC 계층을 명시적 UI state로 처리한다.
ESC 처리를 여러 컴포넌트에서 경쟁시키지 않는다.

### 5. Quest presentation authority
Quest marker, tracker, hover detail, 특수 W1 체크리스트를 명확히 분리하되 gameplay Quest state의 소유권은 가지지 않는다.

### 6. Logging presentation
게임 상태 변화 이벤트를 읽어 로그로 보여주고, 서사 문장을 중복 생성하지 않는다.

## 피해야 할 구조
- Reflection으로 UI 상태를 서로 추측하는 컴포넌트 증가.
- UI 텍스트를 읽어 선택된 target/state를 역추론.
- `DefaultExecutionOrder` 숫자로 최종 writer 경쟁을 해결.
- 같은 RectTransform을 `Update`, `LateUpdate`, `Canvas.willRenderCanvases`에서 반복 덮어쓰기.
- 새 요청마다 별도 `Polish/Finalizer/Override`를 추가.
- Scene generator가 현재 수동 UI 변경을 무심코 덮어쓰기.

## 데이터 원칙
- Quest mutation은 Interaction/Event의 명시적 `QuestAction`만 허용.
- QuestStep은 Quest 내부 진행.
- State는 다른/후속 콘텐츠가 기억해야 하는 사실.
- EventTrigger는 Event 발생 시점만 결정.
- 정적 정의와 런타임 상태를 분리.

## 현재 UI 계약
- 불투명 Location viewport, Quest/Log보다 위.
- 장소명 좌측 상단.
- 자연 배치 + clipping.
- browse 1.5x / target focus 2.0x.
- Interaction 선택 → dialogue에서 장소 배치 급변 금지.
- 우측 Interaction/Narrative panel.
- 캐릭터 portrait.
- UI 계층 순서에 맞춘 ESC.

## Scene generation 주의
`Tools > SandPlanet > Generate Prototype 0.4`는 scene 구조를 다시 생성한다. 일반 코드/UI/data 수정 과정에서는 실행하지 않는다.
