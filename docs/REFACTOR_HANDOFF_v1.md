# SandPlanet Prototype 0.4 — Refactor Handoff v1

> 대상 브랜치: `prototype-foundation`
> 현재 유일한 개발 대상: **Prototype 0.4**
> 목적: 현재 기능을 유지하면서 코드 전수조사 후 안전하게 구조를 교체하기 위한 인수인계 문서.

## 0. 작업 순서
1. 코드 전수조사
2. 조사 결과 사용자 검토
3. 새 구조 설계 승인
4. 승인 후 실제 리팩터링

첫 Codex 작업은 **AUDIT ONLY**다. 게임 코드/씬/데이터를 고치지 않는다. 허용 결과물은 `docs/REFACTOR_AUDIT_REPORT_v1.md`뿐이다.

## 1. 현재 프로젝트
- Repository: `sciencearth2-png/SandPlanet`
- Branch: `prototype-foundation`
- Unity: 6000.4.1f1 / URP
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`
- 표현: 3D 디오라마형 허브 + 2D 장소/내러티브 UI
- 이동: WASD가 아니라 허브 장소 클릭 → 장소 UI → 캐릭터/사물 클릭
- 기간: 21일, Day 15부터 수송선 내부 4구역 허브

과거 프로토타입 버전의 코드, 씬, Generated 자산은 저장소에서 의도적으로 제거되었다. **찾아 복원하거나 비교 대상으로 삼지 않는다.** 현재 조사 대상은 0.4 코드뿐이다.

## 2. Source of truth
1. 이 문서
2. `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
3. `docs/PROTOTYPE_0_4_RUN.md`
4. `docs/DESIGN_STATE.md`
5. `docs/PROTOTYPE_SCOPE.md`
6. `docs/UNITY_ARCHITECTURE.md`
7. `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`
8. 실제 0.4 Runtime/Editor/Scene wiring

문서와 코드가 충돌하면 임의로 고치지 말고 보고서에 기록한다.

## 3. 데이터 계약
Authoring source:
`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Generated runtime CSV:
`Assets/SandPlanet/Data/Generated/CSV/`

핵심 시트/CSV:
- 장소 / 캐릭터 / 사물
- Quest / QuestStep
- Interaction Flow
- State
- Event Flow / Event Trigger
- NPC Schedule

Quest 상태는 Day/State만으로 자동 변경하지 않는다. Interaction/Event 결과의 `QuestAction`만 합법적 mutation 경로다.

## 4. 핵심 게임 규칙
- Week 1: D1~2 재회·상황 파악, D3~6 오아시스 조사·보고, D7 공개 범위 선택.
- Week 2: 폭풍 접근, 수송선 생존 준비, 정착지/오아시스/묘지/수송선 외부 보존 선택.
- Week 3: 수송선 내부 4구역, 폭풍 생존, 관측, 미래 대화, D21 결말.
- 기본 행동 가능 시간 08:00~22:00.
- 최대 의지 5, 수면 +2, 휴식 3시간 → +1.
- 개인 / 대인 / 기술 Lv + XP, 시작 Lv 합계 6, 6XP마다 즉시 레벨업.
- 호감도 0~5. 설득과 별개.
- Soft Requirement는 의지 보완 가능. Hard Requirement는 불가.

## 5. 현재 UI/UX 보존 계약
- Location viewport는 화면 좌측~중앙의 큰 불투명 패널이며 Quest/Log보다 위에 온다.
- 장소명은 viewport 좌측 상단.
- 캐릭터/사물은 맵처럼 자연 배치하며 viewport 밖은 `RectMask2D` 등으로 잘린다.
- 기본 browse 배율은 1.5x, target focus는 2.0x.
- Interaction 선택 → dialogue 전환에서 장소 배치/크기/위치가 재정렬되거나 급변하면 안 된다.
- 우측 panel은 Interaction 선택과 Narrative에 공통 사용한다.
- 캐릭터 초상은 Interaction 선택과 해당 캐릭터 대화에서 크게 표시한다.
- ESC는 가장 위 UI부터 한 단계씩 닫는다. Interaction 선택이 열려 있으면 첫 ESC는 그것만 닫고 장소는 유지한다.
- Interaction 버튼의 본문은 자연스러운 행동 문장. Quest 메타데이터는 보조 표시다.
- Quest tracker에는 W1 도입의 6인 재회 이름 체크리스트가 특수 UI로 들어간다.
- Quest hover card는 제목 → PlayerDescription → 현재 목표.
- 로그는 서사를 중복 설명하지 않고 실제 수치/상태 변화 확인을 돕는다.

## 6. 현재 구조상 우려
최근 0.4 UI 수정 과정에서 여러 보정 컴포넌트가 같은 UI를 다시 쓰는 구조가 누적됐다. 예:
- `SandPlanetPrototype04UxEnhancer`
- `SandPlanetPrototype04SceneDialogueLayout`
- `SandPlanetPrototype04SceneInteractionPolish`
- `SandPlanetPrototype04SceneInteractionFinalizer`
- `SandPlanetPrototype04SceneDensityOverride`
- `SandPlanetPrototype04MapViewportPolish`
- `SandPlanetPrototype04EscapeLayerGuard`
- Quest 관련 UI 보정 컴포넌트들

일부는 Reflection, `DefaultExecutionOrder`, `Update/LateUpdate`, `Canvas.willRenderCanvases`를 사용한다. **이것이 실제 충돌 원인인지 전수조사로 증명해야 한다.**

## 7. Audit에서 반드시 조사할 것
- 모든 0.4 Runtime/Editor 파일의 책임
- scene에 직렬화된 참조와 RuntimeInitialize bootstrap
- 같은 UI property를 쓰는 writer 목록과 실행 순서
- `locationPanel`, `targetRoot`, interaction/modal panels, portrait, Quest/Log, ESC 입력
- Reflection 사용 목록과 숨은 coupling
- Update/LateUpdate/Canvas callback의 프레임별 재쓰기
- Controller의 gameplay/data 책임과 presentation 책임 분리 가능성
- 삭제 가능한 0.4 patch/override 후보
- 동일 기능을 유지한 새 구조 제안

## 8. 매우 중요한 Unity 규칙
`Tools > SandPlanet > Generate Prototype 0.4`를 일반 수정 과정에서 실행하지 않는다. 이 메뉴는 scene 구조를 새로 만들 수 있다.

일반 데이터 수정은:
`Data v1.6 > Export Master Excel to CSV` → `Validate Generated CSV` → 기존 scene 테스트.

Unity 실제 compile/play 검증은 사용자 로컬 프로젝트에서 필요하다.
