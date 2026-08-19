# SandPlanet Unity Architecture

> 상태: **기반 아키텍처 가안**. 현재 확정된 게임 규칙을 Unity 3D에서 버리지 않고 확장하기 위한 최소 구조다.

## 목표
- Phase 0의 짧은 프로토타입을 만들더라도 이후 21일 게임으로 확장 가능해야 한다.
- 콘텐츠 추가가 핵심 코드 수정으로 이어지지 않도록 데이터와 런타임 로직을 분리한다.
- 카메라/이동/아트가 바뀌어도 시간, 의지, 관계, 설득, 수송선 시스템은 유지되게 한다.

## 권장 계층

### 1. Static Definitions
플레이 중 변하지 않는 콘텐츠 정의.

후보 데이터:
- `CharacterDefinition`
- `EncounterDefinition`
- `SkillDefinition`
- `ShipFacilityDefinition`
- `LocationDefinition`

Unity에서는 ScriptableObject를 우선 후보로 사용하되, 대량 시나리오 데이터의 편집 방식은 프로토타입 후 재평가한다.

### 2. Runtime State
세이브에 들어가는 현재 상태.

후보 상태:
- `GameState`: Day, 현재 시간, 주차/페이즈, 글로벌 플래그
- `PlayerState`: 개인/대인/기술, 현재/최대 의지, SP, 획득 스킬
- `CharacterState`: 호감도 0~5, 동료 여부, 정보/퀘스트/설득 플래그
- `ShipState`: 시설별 단계와 파손 상태
- `DailyTrendState`: 오늘의 개인/대인/기술 경향성 누적

런타임 상태는 ScriptableObject 원본 자체를 수정하는 방식보다 별도의 직렬화 가능한 상태 객체로 유지한다.

### 3. Core Systems
- `TimeSystem`: 행동 시간 소비, 하루 경계, 활동 가능 시간 판정
- `WillpowerSystem`: 소비/회복/최대치, 낮잠/수면
- `StatTrendSystem`: 행동 경향 기록, 하루 종료 시 능력치 +1
- `RequirementSystem`: Soft / Hard Requirement 평가
- `EncounterSystem`: 인카운터 조건, 선택지, 비용, 결과 실행
- `RelationshipSystem`: 호감도/동료/인물 플래그
- `ShipSystem`: 시설 상태, 수리, 작업 지시, 이후 파손 확장
- `SkillSystem`: 획득 조건과 기존 규칙 수정 효과

### 4. 3D World Interaction
Phase 0에서는 그레이박스 공간에 상호작용 지점을 배치한다.

예:
- NPC 상호작용 지점
- 수송선 시설/작업 지점
- 낮잠/휴식 지점
- 탐색 인카운터 진입 지점

3D 오브젝트는 `InteractionTarget` 같은 얇은 인터페이스를 통해 핵심 시스템에 요청을 보내고, 게임 규칙을 직접 소유하지 않는다.

## 인카운터 데이터의 기본 책임
하나의 인카운터 정의는 최소한 다음을 표현할 수 있어야 한다.
- ID
- 표시 제목/텍스트
- 발생 위치/기간/플래그 조건
- 기본 시간 비용
- 기본 의지 비용
- 선택지 목록
- 선택지별 Soft Requirement
- 선택지별 Hard Requirement
- 결과: 호감도, 정보 플래그, 설득 플래그, 시설 상태, 경향성 태그 등

특정 인카운터 때문에 새로운 전용 C# 클래스를 만드는 것을 기본 해법으로 삼지 않는다.

## Requirement 원칙
### Soft Requirement
- 개인/대인/기술처럼 의지력으로 일부 부족분을 보완할 수 있는 요구치.
- 정확한 부족분→의지 비용 공식은 아직 TBD.

### Hard Requirement
- 특정 동료
- 특정 정보 플래그
- 특정 퀘스트 상태
- 특정 수송선 시설 상태
- 기타 세계 상태

Hard Requirement는 의지력으로 우회할 수 없다.

## Phase 0 콘텐츠 샘플
실제 21일 콘텐츠를 모두 만들지 않는다.
- 기존 NPC 2명 정도
- 호감도 인카운터 소수
- 조건형 설득 1건
- 수송선 시설 1종
- 개인/대인/기술 스킬 각 1~2개
- DAY 1/21 형태의 하루 루프 3회 정도

시스템은 이후 NPC/인카운터/시설을 데이터 추가로 확장할 수 있어야 한다.

## 아직 결정하지 않는 것
- 정확한 Unity 에디터 버전
- URP/HDRP/Built-in 선택
- 플레이어 이동 방식
- 카메라 시점 및 조작
- 입력 시스템 세부 구성
- 세이브 포맷 최종안
- 대화 UI 최종 연출
- Addressables 등 콘텐츠 로딩 전략

이 항목들은 실제 3D 플레이 감각과 제작 파이프라인을 확인한 뒤 확정한다.
