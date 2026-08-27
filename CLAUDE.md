# SandPlanet Claude Instructions

현재 유일한 개발 대상은 **Prototype 0.4**다.
과거 프로토타입 버전의 코드/씬/Generated 자산은 의도적으로 제거되었으며 다시 복구하거나 참조하지 않는다.

## 먼저 읽을 문서
- @docs/REFACTOR_HANDOFF_v1.md
- @docs/W1_NARRATIVE_SYSTEM_v1_7.md
- @docs/PROTOTYPE_0_4_RUN.md
- @docs/DESIGN_STATE.md
- @docs/PROTOTYPE_SCOPE.md
- @docs/UNITY_ARCHITECTURE.md

## 현재 기술 기준
- Unity 6000.4.1f1 / URP
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`
- 3D 디오라마형 허브 + 2D 장소/내러티브 UI
- Authoring: `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`
- Generated CSV: `Assets/SandPlanet/Data/Generated/CSV/`

## 작업 원칙
- 확정과 TEMP/TBD를 구분한다.
- 요청받지 않은 시스템을 임의로 확장하지 않는다.
- 구현 중 기획 충돌을 발견하면 임의로 바꾸지 말고 영향과 선택지를 제시한다.
- 정적 콘텐츠 정의와 런타임 상태를 분리한다.
- 특정 NPC/퀘스트 전용 로직을 핵심 Controller에 계속 추가하지 않는다.
- 플레이어 선택은 Interaction Flow, 자동/강제 스토리는 Event Flow, 진행 추적은 Quest/QuestStep, 장기 기억은 State가 담당한다.
- Quest 상태는 Day/State 조건만으로 자동 변경하지 않고 Interaction/Event 결과의 QuestAction으로 변경한다.

## 현재 핵심 규칙
- 21일 / 3주.
- Week 1: 재회·상황 파악 → 오아시스 조사·보고 → D7 공개 범위 결정.
- Week 2: 폭풍 대비와 생존 준비, 물리적/정서적 보존 선택.
- Week 3: 수송선 내부 4구역에서 폭풍 생존, 관측, 미래 선택, D21 결말.
- 08:00~22:00 기본 행동 시간.
- 최대 의지 기본 5, 수면 +2, 휴식 3시간 → 의지 +1.
- 개인 / 대인 / 기술 Lv + XP, 시작 Lv 합계 6, 6XP마다 즉시 레벨업.
- 호감도 0~5. 호감도와 설득은 별개.
- Soft Requirement는 의지 보완 가능, Hard Requirement는 불가.

## 현재 UI 계약
- Location viewport는 불투명하고 Quest/Log보다 위.
- 장소명 좌측 상단.
- 캐릭터/사물은 자연 배치 + invisible clipping.
- 기본 map browse 1.5x, target focus 2.0x.
- Interaction 선택에서 dialogue로 넘어갈 때 장소 화면의 배치가 재정렬/급변하면 안 됨.
- 우측 panel은 Interaction/Narrative 용도.
- ESC는 위 UI부터 한 단계씩 닫음.
- 캐릭터 초상은 Interaction 선택과 해당 캐릭터 대화에 사용.

## Audit 모드
사용자가 실제 리팩터링을 승인하기 전에는 코드/씬/데이터를 수정하지 않고 `docs/REFACTOR_AUDIT_REPORT_v1.md`만 작성한다.
`Tools > SandPlanet > Generate Prototype 0.4`를 실행하지 않는다.
