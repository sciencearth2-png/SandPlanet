# SandPlanet Agent Instructions

## Active target
- 현재 유일한 개발 대상은 **Prototype 0.4**다.
- Unity **6000.4.1f1 / URP**.
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`.
- 과거 프로토타입 버전의 코드/씬/Generated 자산은 의도적으로 제거되었다. 복구하거나 다시 참조하지 않는다.

## Read first
작업 전 아래 순서로 읽는다.
1. `docs/REFACTOR_HANDOFF_v1.md`
2. `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
3. `docs/PROTOTYPE_0_4_RUN.md`
4. `docs/DESIGN_STATE.md`
5. `docs/PROTOTYPE_SCOPE.md`
6. `docs/UNITY_ARCHITECTURE.md`

## Refactor audit rule
사용자가 실제 리팩터링 구현을 승인하기 전에는 **AUDIT ONLY**다.
- Runtime/Editor/gameplay 코드 수정 금지
- scene/prefab/meta 수정 금지
- workbook/CSV 수정 금지
- bug 즉석 수정 금지
- `Tools > SandPlanet > Generate Prototype 0.4` 실행 금지
- 첫 audit의 허용 결과물은 `docs/REFACTOR_AUDIT_REPORT_v1.md`뿐이다.

## Source of truth
- 최신 리팩터링/UX 계약: `docs/REFACTOR_HANDOFF_v1.md`
- Week 1 합의: `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
- 현재 런타임/데이터 계약: `docs/PROTOTYPE_0_4_RUN.md`
- Authoring source: `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`
- Generated CSV: `Assets/SandPlanet/Data/Generated/CSV/`
- 문서와 실제 코드가 충돌하면 임의로 하나를 선택하지 말고 audit report에 기록한다.

## Scope control
- 요청받지 않은 시스템/NPC/퀘스트/UI를 추가하지 않는다.
- 확정과 TEMP/TBD를 구분한다.
- 구현 때문에 기획을 바꿔야 하면 먼저 영향과 선택지를 제시한다.
- 현재 구조의 핵심 개념은 `Interaction / Event / Quest / QuestStep / State`다.

## Narrative rules
- 동기 / 공포 / 정체성은 캐릭터 행동의 직접 원리다.
- Big5는 표현 참고값이며 기계적 결정식이 아니다.
- 플레이어가 직접 선택하는 행동은 Interaction Flow가 담당한다.
- 자동/강제 스토리는 Event Flow가 담당한다.
- Quest는 Main / Character / Side 진행 추적과 UI 역할을 하며 `QuestRole`과 `QuestAction`으로 Interaction/Event에 연결된다.
- Quest 상태는 Day/State 조건만으로 자동 변경하지 않는다.

## Unity principles
- 최종 표현은 **3D 디오라마형 허브 + 2D 장소/내러티브 UI**다.
- 3D 허브 오브젝트는 Location ID만 알고 게임 규칙을 소유하지 않는다.
- 정적 콘텐츠 정의와 런타임 상태를 분리한다.
- 특정 캐릭터/퀘스트 전용 분기를 핵심 매니저에 쌓지 않는다. 승인된 특수 UI는 별도 컴포넌트로 둔다.
- 같은 RectTransform/visibility/input state에 여러 최종 writer를 두지 않는 것을 리팩터링 목표로 한다.

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

`Generate Prototype 0.4`는 scene 구조 자체를 재생성해야 할 때만 사용한다.

## Completion
작업 완료 시 변경 시스템, 테스트 방법, 남은 TEMP, 로컬 Unity 검증 필요 여부를 요약한다.
