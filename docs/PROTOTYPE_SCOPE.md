# SandPlanet Prototype 0.4 Scope

> 목적: 21일 전체 게임으로 확장 가능한 데이터 기반 vertical slice에서 핵심 루프, 시간 압박, Character/Main/Side 진행, Event/State 반응을 검증한다.

## 기술 기준
- Unity 6000.4.1f1 / URP
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`
- 표현: 3D 디오라마형 허브 + 2D 장소/내러티브 UI
- WASD 직접 이동이 아니라 장소 클릭형 허브 구조
- 정적 콘텐츠는 Excel → CSV, 런타임 상태는 Controller/state dictionaries에서 관리

## 플레이 공간
### Day 1~14
행성 허브 4장소:
- LOC_01_SETTLEMENT — 거주지
- LOC_02_SHIP — 수송선
- LOC_03_GRAVEYARD — 묘지
- LOC_04_OASIS — 오아시스

### Day 15~21
수송선 내부 허브 4구역:
- LOC_05_COMMAND — 지휘·항해
- LOC_06_SUPPLY — 보급·생명유지
- LOC_07_TECH — 기관·연구
- LOC_08_HABIT — 거주

## 핵심 루프
`허브 → 장소 진입 → 캐릭터/사물 선택 → Interaction → 비용/결과 → State/Quest/Event 반응 → 다음 행동`

자동/강제 서사는 Event Flow가 호출한다.

## 데이터 구조
- Location
- Character
- World Target
- Quest
- Quest Step
- Interaction Flow
- State / Flag
- Event Flow
- Event Trigger
- NPC Schedule

Quest 상태는 조건 만족만으로 자동 변경하지 않는다. 명시적 `QuestAction`이 있는 Interaction/Event 결과만 Quest를 변경한다.

## 시간/성장
- 게임 기간 21일.
- 기본 행동 가능 시간 08:00~22:00.
- Morning 06:00~11:59 / Afternoon 12:00~16:59 / Evening 17:00~22:00.
- 최대 의지 기본 5.
- 수면 +2, 휴식 3시간 → 의지 +1.
- 개인 / 대인 / 기술 Lv + XP.
- 시작 Lv 합계 6.
- 6XP마다 즉시 Lv +1, 초과 XP 이월.
- Soft Requirement는 의지 보완 가능, Hard Requirement는 불가.

## Week 1 검증 범위
- 시작 능력치 배분.
- 벤자민이 제이를 깨우는 도입.
- 여섯 동료와 다시 만나기 + 이름 체크리스트.
- 벤자민 Character Quest의 초기 진입.
- 오아시스 조사에서 관찰 → 가설 → 확인 구조.
- 조사 후 샘에게 보고.
- 공개/제한 공개의 위험을 미리 체험.
- Day 7 다인 Event.

세부 기준은 `W1_NARRATIVE_SYSTEM_v1_7.md`를 따른다.

## Week 2 방향
- 폭풍 접근.
- 수송선의 생존/대피용 정비와 출항용 수리를 구분.
- 정착지/오아시스/묘지/수송선 외부를 무엇까지 보존할지 선택.
- 선택지 과포화를 통해 시간 투자 우선순위를 검증.

## Week 3 방향
- 수송선 내부 4구역 안정성 관리.
- 폭풍 관측과 종료 판단.
- Character/Main future talk.
- Day 21 이후 출항/정착 방향의 결과 확인.

## UI 검증 범위
- Location viewport 안에 캐릭터/사물을 맵처럼 배치.
- viewport 밖 clipping.
- 장소별 저채도 어두운 배경.
- 장소명 좌측 상단.
- 우측 Interaction/Narrative panel.
- 캐릭터 portrait.
- Quest marker / tracker / hover detail.
- 변화 확인용 로그.
- 계층형 ESC 동작.
- target focus 줌/팬.

## 현재 제한 / TEMP
- 최종 ending evaluator threshold는 확정하지 않음.
- Week 3 interior는 greybox.
- 일부 수치/stance는 TEMP.
- 최종 Soft Requirement 공식은 플레이 테스트 후 확정.
- 현재 UI 코드에는 여러 patch/polish writer가 누적되어 있어 리팩터링 전수조사 대상이다.

## 성공 기준
- 데이터 추가로 새로운 Interaction/Event/Quest 진행을 표현할 수 있다.
- 시간/의지/성장이 선택 압박을 만든다.
- Main만 진행하면 optional content를 모두 챙기기 어렵다.
- Week 1 선택이 Week 2/3의 상태와 대사에 돌아온다.
- 캐릭터/사물/퀘스트 UI가 서사를 대신하지 않고 플레이어의 현재 선택을 이해시키는 보조 장치로 작동한다.
