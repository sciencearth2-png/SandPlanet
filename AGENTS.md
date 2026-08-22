# SandPlanet Agent Instructions

## CURRENT REFACTOR / AUDIT PRIORITY — 2026-08-22

현재 active target은 **Prototype 0.4**다.
코드 전수조사 또는 리팩터링 작업을 시작하기 전에 반드시 아래 문서를 순서대로 읽는다.

1. `docs/REFACTOR_HANDOFF_v1.md`
2. `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
3. `docs/PROTOTYPE_0_4_RUN.md`
4. `docs/DESIGN_STATE.md`
5. `docs/PROTOTYPE_SCOPE.md`
6. `docs/UNITY_ARCHITECTURE.md`

`REFACTOR_HANDOFF_v1.md`는 최근 채팅에서 확정된 UX와 현재 리팩터링 목적을 전달하기 위한 최신 문서다. 과거 Prototype 0.3 문구나 오래된 문서와 충돌하면 **현재 0.4 Handoff를 우선**하고, 충돌 자체를 명시한다.

### 첫 Refactor Audit 실행 규칙

사용자가 명시적으로 리팩터링 구현을 승인하기 전까지:

- Runtime/Editor/gameplay 코드 수정 금지
- scene/prefab/meta 수정 금지
- workbook/CSV 수정 금지
- bug 즉석 수정 금지
- patch/legacy 파일 삭제 금지
- `Tools > SandPlanet > Generate Prototype 0.4` 실행 금지

첫 Audit에서 허용되는 결과물은 원칙적으로 `docs/REFACTOR_AUDIT_REPORT_v1.md`뿐이다.
현재 작업의 목적은 먼저 **실제 기능, 책임, 의존관계, 중복 writer와 execution-order 충돌을 증명하는 것**이다.

## Source of truth

- 현재 리팩터링/UX 계약: `docs/REFACTOR_HANDOFF_v1.md`
- Week 1 최신 합의: `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
- Prototype 0.4 runtime/data 계약: `docs/PROTOTYPE_0_4_RUN.md`
- 기획상 **확정**과 **가안**을 구분한다. 가안을 임의로 확정하지 않는다.
- 현재 프로젝트의 우선순위는 완성형 21일 게임을 한 번에 만드는 것이 아니라, 최종 게임으로 확장 가능한 Unity 3D 기반 프로토타입에서 핵심 루프를 검증하는 것이다.
- 문서와 실제 코드가 충돌하면 조용히 한쪽을 고르지 말고 충돌을 기록한다.

## Scope control

- 요청받지 않은 시스템, NPC, 퀘스트, 자원, UI를 새로 추가하지 않는다.
- 새 기능은 기존 `시간 / 의지 / 개인·대인·기술 / 호감도 / Interaction / Event / Quest / State / 수송선` 구조 안에서 먼저 해결한다.
- 구현 때문에 기획을 바꿔야 한다면 코드에서 임의 변경하지 말고 변경 제안을 먼저 남긴다.
- TBD는 오류가 아니다. 결정되지 않은 사양은 TEMP/PLACEHOLDER임을 명확히 표시하고 정식 사양처럼 굳히지 않는다.

## Narrative rules

- 캐릭터 설정의 `동기 / 공포 / 정체성`은 직접적인 행동 원리다. 캐릭터가 이 원리에서 이유 없이 벗어나는 선택지는 만들지 않는다.
- 캐릭터는 사건을 통해 동기의 충족 방식, 공포에 대한 인식, 자신의 정체성 해석을 바꿀 수 있다.
- Big5 성향 수치는 대략적인 표현 가이드이며 기계적인 행동 결정식으로 사용하지 않는다.
- 플레이어가 선택해 실행하는 구체 행동은 Interaction Flow가 담당한다.
- Quest는 Main / Character / Side의 장기·단기 진행 추적과 UI 역할을 하며, `QuestRole`과 `QuestAction`으로 Interaction/Event에 연결된다.
- 단순 성장/생활 행동은 Quest가 아닐 수 있으며 `QuestRole=NONE` Interaction으로 처리할 수 있다.
- 플레이어가 선택하지 않아도 자동·강제로 호출되는 스토리는 Event로 분리한다.

## Unity 3D principles

- 엔진은 **Unity 6000.4.1f1 / URP**다.
- 최종 표현은 **3D 디오라마형 허브 + 2D 장소/인카운터 UI**를 기준으로 한다.
- 버리는 프로토타입보다 최종 게임으로 확장 가능한 기반 코드를 우선한다.
- 정적 콘텐츠 정의와 런타임 상태를 분리한다.
- NPC, Interaction, Event, Quest, 수송선 시설은 가능한 한 데이터 추가로 확장할 수 있게 만든다.
- 한 NPC/퀘스트/인카운터 전용 로직을 핵심 매니저에 하드코딩하지 않는다. 단, 승인된 특수 UI(예: W1 6인 재회 체크리스트)는 명시적 예외로 분리할 수 있다.
- 3D 허브 오브젝트는 Location ID만 알고 게임 규칙은 소유하지 않는다.

## Prototype 0.4 data principles

Authoring source:

`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Generated runtime CSV:

`Assets/SandPlanet/Data/Generated/CSV/`

- 정상적인 데이터 수정은 기존 scene를 재생성하지 않는다.
- `Tools > SandPlanet > Generate Prototype 0.4`는 실제 generated scene 구조를 다시 만들 필요가 있을 때만 사용한다.
- 일반 authoring 변경은 `Data v1.6 > Export Master Excel to CSV`와 `Validate Generated CSV`를 사용한다.
- Quest 상태는 Day/State 조건만으로 자동 변경하지 않는다.
- Quest mutation은 Interaction/Event 결과의 `QuestAction`을 사용한다.
- `QuestStep`은 Quest 내부 진행, `State`는 다른 콘텐츠가 이후에도 기억해야 하는 사실이다.

## Prototype principles

- 수치와 데이터는 하드코딩보다 조정 가능한 데이터 구조로 분리한다.
- 프로토타입에서는 아트 완성도보다 시스템 상태와 선택 결과가 명확히 보이는 것을 우선한다.
- 하나의 기능을 만들 때 입력 → 비용 → 상태 변화 → 피드백까지 최소한의 완결된 루프로 구현한다.
- 현재 Game State를 기준으로 장소마다 유효한 Interaction/Event가 동적으로 바뀌어야 한다.
- 같은 기계적 효과라도 메인 진행/인물 상태가 다르면 다른 텍스트 Variant가 될 수 있다.
- 시간 또는 의지를 소비하는 플레이어 선택은 원칙적으로 확정 이득을 주거나 예정 손실을 막아야 한다. 강제 Event나 미참여 시 불이익이 있는 긴급 콘텐츠는 예외다.

## Current core rules

- 게임 기간: 21일.
- Week 1 큰 흐름: DAY 1~2 상황 파악·재회 → DAY 3~6 오아시스 조사·샘과 소통 → DAY 7 공개 범위 선택.
- 기본 행동 가능 시간은 현재 Prototype 0.4 구현 기준 08:00~22:00이며, time band는 Morning 06:00~11:59 / Afternoon 12:00~16:59 / Evening 17:00~22:00. Interaction의 time band 판정은 시작 시각 기준이다.
- 기본 최대 의지력: 5.
- 수면 회복: 현재 의지 +2, 최대치 초과 불가.
- 휴식: 3시간 소비, 의지 +1.
- 성장 능력치는 개인 / 대인 / 기술이며 각각 Lv + XP를 가진다.
- 시작 시 세 능력치 Lv 총합 6을 배분한다. 각 Lv 상한은 20.
- 각 능력치 6XP 획득 시 즉시 Lv +1, 초과 XP는 이월한다.
- 기존 일일 경향성 성장 시스템은 폐기한다.
- 초기 밸런스 기준: 12시간 ≈ 의지 4 ≈ 호감도 3 ≈ XP 12 ≈ 능력치 Lv +2.
- 호감도 +1은 평균 약 4시간 가치. 실제 행동은 조건에 따라 약 2~6시간 범위를 사용한다.
- 호감도는 NPC별 0~5. 호감도와 설득은 별개다.
- Soft Requirement는 개인/대인/기술 부족을 의지로 보완할 수 있다. 정확한 공식은 프로토타입에서 검증한다.
- 동료, 정보, 세계 상태, 시설 상태 등 Hard Requirement는 의지로 우회할 수 없다.

## Current UI behavior contract

세부사항은 `docs/REFACTOR_HANDOFF_v1.md`를 우선한다.

핵심만 요약하면:

- Location viewport는 불투명하고 Quest/Log보다 위에 표시.
- 장소명은 좌측 상단.
- 캐릭터/사물은 viewport 안에 자연 배치하고 invisible clip boundary를 사용.
- 기본 map browse 1.5x, 대상 focus 2.0x.
- Interaction 선택 → dialogue 전환에서 장소 배치가 재정렬/급변하면 안 됨.
- 우측 Interaction/Narrative panel 사용.
- 첫 ESC는 우측 Interaction 선택 UI만 닫고 장소는 유지.
- 상호작용 entry의 본문은 자연스러운 행동 문장을 유지하고 Quest 정보는 보조 표시.

## Coding workflow

- 변경 전에 관련 문서를 확인한다.
- Prototype 0.1~0.3은 legacy/reference가 될 수 있다. 현재 active target은 0.4다.
- Audit 단계에서는 legacy를 삭제하거나 현재 0.4와 분리했다고 가정하지 말고 실제 참조를 확인한다.
- 가능한 경우 작은 단위로 구현하고 테스트한다.
- 실제 리팩터링은 단계별 rollback point와 regression test를 둔다.
- 작업 완료 후 변경한 시스템, 테스트 방법, 남은 TEMP 가정을 요약한다.
- Unity 실제 compile/play 검증은 로컬 프로젝트에서 별도로 필요하다.
