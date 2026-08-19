# SandPlanet Prototype Scope

> 목적: 21일 전체 게임을 구현하기 전에 핵심 루프와 콘텐츠 구조가 실제로 재미와 선택 압박을 만드는지 확인한다.
> 프로토타입 콘텐츠는 최종 시나리오 확정본이 아니며 TEMP/PLACEHOLDER가 포함될 수 있다.

## 기술 방향 — 확정
- 엔진: **Unity 6000.4.1f1**
- 렌더 파이프라인: **Universal 3D / URP**
- 최종형 기반: **3D 디오라마형 허브 + 2D 장소/인카운터 UI**
- 최종 게임으로 확장 가능한 기반 프로토타입으로 제작한다.
- 시스템/콘텐츠 데이터와 런타임 상태를 분리하는 방향을 유지한다.

## 화면 문법
### Week 1~2
`3D 행성 허브 → 장소 화면 → 2D Encounter → 결과 → 장소/행성 복귀`

현재 핵심 장소:
- 수송선
- 묘지
- 거주지
- 오아시스

### 수송선
- 일반 장소 Encounter 외에 별도 수송선 정비도를 가진다.
- 시설 후보: 코어 / 추진 / 거주 / 방호.
- 현재 0~3단계 상태 구조를 유지하되 세부 수리 공식은 TBD.

### Week 3
- 모래폭풍 이후 3D 수송선 내부 허브를 메인 공간으로 사용하는 방향.
- 같은 `허브 → 구역 → Encounter` 문법을 유지한다.

## Prototype 0.1 — 완료
핵심 시스템의 첫 Unity 사이클 검증.

검증 완료:
- DAY / 시간 루프
- 의지력
- 시작 스탯 배분
- 기존 경향성 성장 샘플
- 호감도 / Soft·Hard Requirement
- 설득 샘플
- 기술자 동료
- 방호 0~3 수리
- 3D 랜드마크 클릭

> 0.1의 `낮잠 2시간`, `경향성 성장` 등은 이후 기획 변경으로 폐기/수정되었다.

## Prototype 0.2 — 완료
화면 구조와 정보 흐름 검증.

검증 완료:
- 3D 행성 허브의 수송선 / 묘지 / 거주지 / 오아시스 클릭.
- Hover 피드백.
- `! / … / 인물` 상태 표시.
- 별도 장소 화면.
- 2×3 Encounter 카드 그리드 샘플.
- 2D 비주얼 영역 + 텍스트/선택지 영역.
- 수송선 정비도.
- 방호 실제 작동 + 나머지 시설 PLACEHOLDER.

> 0.2는 기존 0.1 규칙을 유지한 화면 프로토타입이므로 최신 성장/시간 규칙과 다르다.

# Prototype 0.3 — 현재
## 목표
**Week 1 대표 콘텐츠를 소량 실제 배치하면서, SandPlanet의 최종형에 가까운 동적 Encounter 기반을 검증한다.**

0.3의 핵심 질문:
1. 현재 세계 상태에 따라 같은 장소의 Encounter Pool이 자연스럽게 달라지는가?
2. 개인/대인/기술 Lv + XP가 1~6시간 행동을 모두 의미 있게 만드는가?
3. 메인과 캐릭터/생활 콘텐츠가 같은 Encounter 구조 안에서 자연스럽게 경쟁하는가?
4. 캐릭터 포트레이트가 2D Encounter의 서사 구분과 몰입에 도움이 되는가?

## 0.3 반드시 포함
### 1. 새 성장 시스템
- 개인 / 대인 / 기술 각각 Lv + XP.
- 시작 Lv 총합 6 배분.
- 각 능력치 6XP → 즉시 Lv +1.
- XP 초과 이월.
- HUD에 각 Lv + XP 상시 표시.
- **기존 경향성 시스템 완전 제거.**

### 2. 시간 / 의지 최신화
- 기본 08:00~22:00.
- Morning 06:00~11:59 / Afternoon 12:00~16:59 / Evening 17:00~22:00.
- 시간대는 Encounter 시작 시각 기준.
- 휴식 3시간 → 의지 +1.
- 수면 후 현재 의지 +2.
- 07→06 시작 / 23→24 종료 퍽은 향후 확장 가능하게 두되, 0.3에서는 기본 시간만 필수.

### 3. 통합 Encounter 구조
플레이어가 직접 선택하는 Main / Character / Activity / World 콘텐츠는 모두 동일한 Encounter 규칙을 사용한다.

Encounter가 표현할 최소 항목:
- ID
- Variant Group(선택)
- Narrative Type / Visual Priority
- Global / Location Scope
- Location ID
- Primary NPC ID
- Week / Open Day / Close Day
- Morning / Afternoon / Evening 허용
- 반복 규칙
- Show / Hide 조건
- 텍스트
- Choice 목록

### 4. Choice / Requirement / Result
Choice가 표현할 최소 항목:
- 표시 문구
- 시간 비용
- 의지 기본 비용
- Soft Requirement: 개인 / 대인 / 기술
- Hard Requirement: 플래그/호감도/세계 상태 등
- 결과 Action

0.3에서는 Soft Requirement 부족분을 의지로 보완하는 임시 공식을 사용할 수 있으며 반드시 TEMP로 표시한다.

### 5. State / Flag 기반 Pool 변화
대표 테스트:
- 추모 전/후 묘지 Encounter가 추가/제거/텍스트 변경된다.
- 오아시스 조사 전/조사 중/위험 확인 후 Encounter가 달라진다.
- 샘에게 보고 전/후 관련 NPC 콘텐츠가 달라진다.
- 같은 기계적 보상이라도 맥락이 다르면 Variant Encounter로 분리할 수 있다.

### 6. Event 구조
- Encounter: 플레이어가 선택.
- Event: 조건 충족 시 자동/강제 호출.

0.3 대표 Event:
- DAY 1 기상/재회 시작.
- DAY 3 조사 단계 진입 안내.
- DAY 7 종료 시 공개 범위 선택.

Event와 Encounter는 가능한 한 동일한 결과 Action/Flag 시스템을 공유한다.

### 7. 캐릭터 포트레이트
현재 준비된 파일:
- `Assets/SandPlanet/Art/Portraits/샘.png`
- `Assets/SandPlanet/Art/Portraits/지나.png`
- `Assets/SandPlanet/Art/Portraits/페이.png`
- `Assets/SandPlanet/Art/Portraits/벤자민.png`
- `Assets/SandPlanet/Art/Portraits/보리치.png`
- `Assets/SandPlanet/Art/Portraits/디야.png`

Primary NPC가 있는 Encounter는 해당 포트레이트를 표시한다. NPC 없는 조사/독백은 포트레이트를 비울 수 있다.

## 0.3 대표 콘텐츠 — 소량만 구현
전체 Week 1을 완성하지 않는다. 구조를 검증할 수 있는 샘플만 넣는다.

예시 범위:
- DAY 1 기상 Event.
- 샘에게 사고 이후 상황 듣기.
- 묘지 첫 방문.
- 추모.
- 추모 전/후로 텍스트가 달라지는 1시간 개인 성장 행동.
- 페이에게 수송선 상태 듣기.
- 1시간 기술 성장 행동.
- 벤자민 관계 행동: 능력치에 따라 3/4/6시간 접근 샘플.
- DAY 3 오아시스 조사 단계 개방 Event.
- 조사 전/후 오아시스 Pool 변화.
- 여러 접근 선택지가 있는 대표 조사 Encounter.
- 위험 확인 후 샘에게 보고.
- 보고 이후 측근 상담 Encounter 1~2개.
- DAY 7 강제 공개 범위 Event.

## 0.3 보상 원칙
일반 선택은 `시간 또는 의지 소비 → 실질 효용` 원칙을 따른다.

초기 밸런스 기준:
> 12시간 ≈ 의지 4 ≈ 호감도 3 ≈ XP 12 ≈ 능력치 Lv +2

다만 **Main 타입은 서사 진행 자체가 보상**이므로 XP/시간 환율을 의도적으로 깨도 된다.

## 0.3 성공 기준
- 추모 전/후 또는 조사 전/후 같은 상태 변화가 별도 코어 코드 수정 없이 Encounter 조건으로 표현되는가?
- 장소를 다시 방문했을 때 카드 풀이 실제로 달라져 변화가 느껴지는가?
- 1시간 XP 행동, 3~6시간 핵심 행동, 3시간 휴식이 일정 계획에서 서로 다른 역할을 하는가?
- 오전에 Lv이 올라 오후의 효율 선택지가 즉시 열리는 흐름이 가능한가?
- 포트레이트가 Primary NPC에 따라 자동 전환되는가?
- Main / Character / Activity가 데이터상 동일하지만 UI에서 중요도가 구분되는가?
- 향후 Excel Encounter/Choice/Condition/Action 테이블의 데이터를 옮길 수 있는 구조인가?

## 0.3에서 하지 않는 것
- Week 1 전체 40~50개 Encounter 완성.
- Week 2 전체 과포화 콘텐츠 구현.
- Week 3 생존 시뮬레이션.
- 전체 캐릭터 아크 완성.
- 전체 스킬트리.
- 최종 세이브 포맷.
- 최종 UI 아트/애니메이션.
- Excel 자동 임포터 완성.

## 실행 목표
0.3은 기존 0.1/0.2와 별도 씬/메뉴로 생성한다.
- 메뉴: `Tools > SandPlanet > Generate Prototype 0.3`
- 씬: `Assets/Scenes/SandPlanet_Prototype_03.unity`

기존 0.1/0.2는 회귀 확인용으로 유지한다.
