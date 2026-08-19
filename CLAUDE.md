# SandPlanet Claude Instructions

프로젝트의 현재 기획 상태는 다음 문서를 우선 참고한다.

- @docs/DESIGN_STATE.md
- @docs/PROTOTYPE_SCOPE.md
- @docs/UNITY_ARCHITECTURE.md

## 작업 원칙
- 확정된 기획과 가안을 구분한다.
- 사용자가 요청하지 않은 시스템을 임의로 확장하지 않는다.
- 엔진은 **Unity 6000.4.1f1 / URP**다.
- 버리는 프로토타입이 아니라 최종 게임으로 확장 가능한 기반 프로토타입을 만든다.
- 완성형 21일 게임을 한 번에 만들지 말고, 현재 Prototype 범위 안에서 핵심 루프를 검증한다.
- 구현 중 기획 충돌을 발견하면 임의로 고치지 말고 영향과 선택지를 제시한다.
- TBD는 TEMP/PLACEHOLDER로 남겨도 된다.

## Narrative rules
- 캐릭터의 `동기 / 공포 / 정체성`은 직접적인 행동 원리다. 이유 없이 여기서 벗어나는 선택지를 만들지 않는다.
- Big5는 대략적인 표현 가이드다.
- 사건을 통해 캐릭터의 상황 인식, 동기 충족 방식, 공포 극복, 정체성 해석은 변할 수 있다.
- Main / Character / Activity / World는 별도 퀘스트 데이터 구조가 아니라 Encounter의 태그/비주얼 분류다.
- 자동/강제 스토리는 Event로 분리한다.

## Unity 설계 원칙
- 정적 정의와 런타임 상태를 분리한다.
- 특정 NPC/인카운터 전용 로직을 핵심 매니저에 박아 넣지 않는다.
- 현재 Game State를 평가해 장소의 Encounter Pool을 동적으로 구성한다.
- Encounter / Choice / Condition / Action / Event 구조가 향후 Excel 데이터로 이전 가능하도록 한다.
- 기존 0.1/0.2는 깨지 않고 0.3을 별도 버전으로 추가한다.

## 현재 핵심 규칙
- 21일 / 3주.
- Week 1: D1~2 상황 파악·추모 → D3~6 오아시스 조사·샘과 소통 → D7 공개 범위 결정.
- 기본 08:00~22:00.
- Morning 06:00~11:59 / Afternoon 12:00~16:59 / Evening 17:00~22:00, 시작 시각 기준.
- 휴식 3시간 → 의지 +1, 수면 +2, 최대 의지 기본 5.
- 개인 / 대인 / 기술 각각 Lv + XP.
- 시작 Lv 총합 6, 각 Lv 상한 20.
- 6XP → 즉시 Lv +1, 초과 이월.
- 기존 경향성 시스템은 폐기.
- 초기 환율: 12시간 ≈ 의지 4 ≈ 호감도 3 ≈ XP 12 ≈ Lv +2.
- 시간/의지를 쓰는 일반 선택은 확정 효용 또는 손실 방지가 있어야 한다. Main/강제 Event는 예외 가능.
- 호감도 +1은 평균 약 4시간. 조건에 따라 2~6시간 범위.
- Soft Requirement는 스탯 부족을 의지로 보완 가능, Hard Requirement는 불가.

## Prototype 0.3 우선순위
1. Lv + XP 성장과 HUD.
2. 최신 시간/의지 규칙.
3. State/Flag 기반 동적 Encounter Pool.
4. Choice의 시간/의지/Soft·Hard Requirement.
5. Event 자동 호출.
6. 샘/지나/페이/벤자민/보리치/디야 포트레이트 자동 표시.
7. 추모 전후 / 오아시스 조사 전후 / 보고 전후 대표 Variant 콘텐츠.
8. 전체 Week 1을 채우기보다 구조 검증에 필요한 소량의 실제 콘텐츠.
