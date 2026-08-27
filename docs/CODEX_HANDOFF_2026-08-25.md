# SandPlanet Codex Handoff — 2026-08-25

이 문서는 `prototype-foundation`에서 SandPlanet 작업을 이어받는 Codex용 **최우선 인수인계 문서**다.

> **중요:** 이 문서와 과거 `docs/*.md`가 충돌하면 이 문서와 `Assets/SandPlanet/Data/Design/SandPlanet_ContentDesign.xlsx`의 최신 Canon을 우선한다. 과거 문서를 근거로 최신 Canon/데이터를 되돌리지 않는다.

---

## 0. 작업을 시작하기 전에 반드시 할 일

Codex는 구현을 시작하기 전에 아래를 순서대로 확인한다.

1. 현재 branch가 `prototype-foundation`인지 확인.
2. `git status` 확인.
   - 로컬 `SandPlanet_Master.xlsx` / Generated CSV / Unity scene/code 변경이 남아 있으면 **절대 덮어쓰지 않는다.**
   - 특히 2026-08-25 기준 사용자가 Unity에서 확인한 최신 W1 Master는 **v3.7 Repair Visibility hotfix**다. 원격 Master가 그것과 동일한지 먼저 확인한다.
3. 아래 Source of Truth를 순서대로 확인한다.
   1. `Assets/SandPlanet/Data/Design/SandPlanet_ContentDesign.xlsx`
   2. `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`
   3. `Assets/SandPlanet/Data/Generated/CSV/`
   4. C# Runtime / Validator
4. 세 자료가 충돌하면 계산/구현 전에 원인을 먼저 규명한다. 임의로 새로운 시스템을 만들어 메우지 않는다.
5. 일반 데이터 작업에서 `Tools > SandPlanet > Generate Prototype 0.4`를 실행하지 않는다.

---

# 1. 프로젝트 기준

- Repository: `sciencearth2-png/SandPlanet`
- Working branch: `prototype-foundation`
- Unity: **6000.4.1f1 URP**
- Main prototype scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`
- 로컬 기준 경로: `D:\SandPlanet`
- 장르: **3D 디오라마형 허브 + 2D 내러티브/시간관리 어드벤처**
- WASD 이동 없음. 장소 / 인물 / 사물을 클릭하여 상호작용한다.
- Week 1~2는 행성 허브, Day15부터 Week3는 수송선 내부 4구역 허브로 전환한다.
- 총 21일.

---

# 2. 가장 중요한 Source of Truth 규칙

## 2.1 ContentDesign

`Assets/SandPlanet/Data/Design/SandPlanet_ContentDesign.xlsx`

이 파일은 다음의 **설계 원본**이다.

- 엔딩 역산
- 수송선 Facility Integrity
- W1/W2 Standard Repair Budget
- Action Budget
- Route Simulation
- FacilityTransition
- W3 Boss/Rebalance
- EndingEvaluator
- 최신 Canon 동기화 메모

현재 설계 버전: **ContentDesign v3.1 Canon + Ship Baseline Sync**.

중요 시트:

- `06_ActionBudget`
- `07_RouteSimulation`
- `08_ShipDesign`
- `13_FacilityTransition`
- `15_W3BossRebalance`
- `17_CharacterFinals`
- `18_EndingEvaluator`
- `19_W2ReverseDesign`
- `20_W1ReverseAudit`
- `21_IntegratedRouteSim`
- `26_W1_NarrativeAuthoring`
- `28_W2_NarrativeAuthoring`
- `29_W3_NarrativeAuthoring`
- `30_EndingNarrative`
- `31_CanonSync_v3.1`

**수치/엔딩/루트 계산 전에 반드시 ContentDesign을 먼저 본다.**

## 2.2 Master

`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Unity CSV Export용 실제 Authoring 원본이다.

ContentDesign에 설계가 있다고 해서 Runtime에 이미 존재한다고 가정하면 안 된다. ContentDesign → Master 이식 여부를 반드시 확인한다.

## 2.3 Generated CSV

`Assets/SandPlanet/Data/Generated/CSV/`

Master에서 Export된 Runtime 입력이다. **Generated CSV를 직접 Source처럼 수정하지 않는다.** 수정은 Master에서 하고 다시 Export한다.

## 2.4 Runtime / Validator

현재 Runtime은 Prototype04 계열이 authority다.

- `Prototype04GameState`: mutable runtime state
- `Prototype04ConditionEvaluator`: 조건 판정
- `Prototype04QuestService`: Quest/Step mutation
- `Prototype04ScheduleService`: NPC schedule
- `Prototype04InteractionService`: Interaction availability
- `Prototype04EventService`: Event/Trigger/STATE_CHANGE
- `Prototype04FlowRuntime`: Interaction/Event flow 및 commit
- `SandPlanetPrototype04Controller`: coordinator
- `Prototype04NavigationController`: UI mode / Back / ESC
- `Prototype04LocationPresenter`: 장소/target presentation
- `Prototype04NarrativeLogPresenter`: 현재 통합 Narrative UX presentation
- `Prototype04HudPresenter`: HUD

Data Validator:

- `Assets/SandPlanet/Editor/SandPlanetSpreadsheetExporter04.cs`
- Narrative UX 규칙에 맞춰 이미 수정됨.
- 과거의 `ChoiceID가 있으면 TimeCost >= 1` 규칙은 폐기됨.
- 0시간 질문/대화/관찰/Quest 수락 허용.
- inactive row는 runtime validation에서 제외.
- 음수 TimeCost만 오류.
- `SET_STEP`은 `NextStepID` 필수, 존재해야 하며 같은 Quest 소속이어야 함.

관련 커밋: `1b625a8` (`Align data validator with Narrative UX rules`).

---

# 3. 현재 Canon — 절대 구설정으로 되돌리지 말 것

## 3.1 타임라인

- 지구 출항 → 불시착까지 **약 3개월**.
- 불시착 이후 Jay만 약 **10년**을 건너뜀.
- 추락 전 관계는 별도 설정이 없는 한 수년간 누적된 관계로 쓰지 않는다.

## 3.2 Jina

- Jay의 친구/절친이 아니다.
- 약 3개월 항해 중 일이 꼬일 때 Jay가 의지하던 **연상의 실무자 / 큰언니 같은 인물**.
- 사적 추억보다 업무 신뢰 중심.
- W3 성장 방향: Jay가 답을 맡기는 후배가 아니라 **조건을 함께 계산하는 동료**가 된다.

금지:
- 동년배 절친
- 수년간 친구였던 회상
- 과도한 사적 추억

## 3.3 Diya

- 20대.
- 망가진 지구에서 태어남.
- 기성세대가 남긴 실패에 냉소와 불만이 큼.
- 반대로 자신보다 어린 아이들이 잘 지내는 것을 특히 좋아함.
- 핵심 가치: **자신이 물려받은 실패를 다음 세대에 반복하지 않는다.**

따라서 STAY/LEAVE 모두 성립 가능:

- STAY: 아이들에게 또 폐허만 넘기지 않기 위해 실제로 살아갈 기반을 만든다.
- LEAVE: 집/장소 때문에 아이들을 위험에 묶지 않는다.

금지:
- 단순 낙관적 정착파
- 단순히 집을 사랑해서 STAY하는 캐릭터

## 3.4 Benjamin

- 배우자 없음.
- 자녀 없음.
- 불시착 당시 **약혼자 한 사람**을 잃음.
- 묘지 아크의 핵심은 약혼자의 무덤과 기억.
- 최종 성장: 장소를 떠나는 것과 약혼자를 잊는 것은 같은 일이 아니다.

금지:
- 아내
- 아이
- 아내와 아이의 두 무덤

Legacy ID (`STA_W1_BEN_FAMILY_KNOWN` 등)는 호환성 때문에 남아 있을 수 있으나 의미는 최신 Canon을 따른다.

## 3.5 Mira

- 모래지옥/싱크홀에 빨려 들어가지 않는다.
- 모래폭풍 때문에 **발자국이 사라지고 실종**된다.
- Jay가 흔적/지형/정보를 따라 수색해서 찾아낸다.
- 구조 실패 → Jina 희생 Fallback은 폐기.

사건 기능:
- Jay의 능동적 수색
- 현재 Jay에 대한 Jina의 신뢰 변화
- 시간관리 압박

## 3.6 Oasis

- 호수/연못이 아니다.
- **오아시스 = 지하수를 퍼 올리는 급수 펌프 자체의 이름**.
- 취수정 / 배관 / 계측부가 연결된다.
- 지표의 열린 수면, 물가, 호수 수위 묘사를 쓰지 않는다.

W1 조사 요소:
- 펌프 유입량
- 흡입 깊이
- 수질
- 지하수 계측
- 주변 침하/퇴적

D6 결론:

> **현재 지하수와 오아시스 펌프의 취수 방식만으로 장기 거주를 보장할 수 없다.**

절대 `이 행성에서는 살 수 없다`로 확대하지 않는다.

---

# 4. Narrative UX 계약

현재 데이터/대사 작성 시 아래를 지킨다.

1. Narration과 Dialogue를 분리한다.
   - Dialogue에 행동 지문을 넣지 않는다.
   - 사람 발화 표기: `인물 - "대사"` 형태의 의미를 유지한다.
2. Beat 리듬:
   - 정보 → 플레이어 입력 → 반응 → 다음 정보
3. 단순 질문/관찰/생각에는 시간 비용을 강제하지 않는다.
4. 실제 시간이 드는 행동만 TimeCost 사용.
5. 플레이어 선택 로그:
   - 플레이어 발화 → Jay 발화
   - 플레이어 행동 → Narration
6. Hover에는 **즉시 발생하는 기계적 변화만** 표시.
   - 시간 / Will / XP / Affinity 등
   - 미래/숨은 Flag 공개 금지
7. Choice body는 왼쪽, req/cost는 오른쪽.
8. Narrative body + choices는 하나의 세로 ScrollRect 안에서 clipping.
9. Typewriter는 새 Beat만 적용. 이전 로그는 유지.
10. 클릭 시 현재 Beat 즉시 완성. Choice는 body 뒤에 등장.

---

# 5. Week 1 최신 상태

## 5.1 D1 / D2

- D1 Narrative UX migration + QA 완료.
- D2 migration 완료.
- D2에는 새로운 Character Quest를 열지 않는다.

D2 Seed:

- Jina: 10년 시간차 + 항해 업무 신뢰
- Diya: 생활공간 / 집 / 아이들
- Faye: 장비 정리
- Borichi: 관측 기록
- anonymous Night Shift worldbuilding

Character Quest unlock 기본:

- Benjamin: D1 가능
- Sam: D3+, Oasis Investigation Main ACTIVE 후
- Borichi: D3+, D2 관측 Seed + Oasis Main ACTIVE 후
- Jina: D4+, D2 시간차 Seed 후
- Faye: D4+, D2 장비 Seed 후
- Diya: D5+, D2 생활공간 Seed 후

## 5.2 W1 Main Investigation

4-step chain:

1. D3 observation
2. D4 hypothesis
3. D5 verification
4. D6 report

중요 QuestStep 연결:

- `_01 -> _02`
- `_02 -> _03`
- `_03 -> _04`
- `_04` = COMPLETE, Next blank

과거 Master에서 `SET_STEP`인데 NextStepID가 비어 D4가 막히는 버그가 있었고 이미 수정됐다. 다시 되돌리지 않는다.

## 5.3 D6 / D7

- D6 Focus A: Jina / Faye / Borichi 중 하나, 2h.
- 선택한 캐릭터만 W1 Full Route Mastery eligibility 유지.
- 미선택 캐릭터도 W2/W3 Final/Ending 접근 자체는 막지 않는다.
- Sam W1 Full은 D3 + D6 responsibility episode 둘 다 필요.
- D7 Focus B: Benjamin / Diya 중 하나.
- D7에 Investigation 결과 공개 범위 결정.

---

# 6. 수송선 시스템 — 새로 기획하지 말 것

이 부분은 과거에 이미 엔딩으로부터 역산되어 설계되었다. **새 4축 성장 시스템 등을 임의로 만들지 않는다.**

ContentDesign v3.1의 기존 baseline:

- W1 Standard Repair Slots: **5회**
  - D3~D7 하루 1회
- W2 Standard Repair Slots: **7회**
  - D8~D14 하루 1회
- 총 Standard Repair: **12회**

초기 Facility Integrity:

- Hull: `5/20`
- Bridge: `2/5`
- Engine: `2/5`
- Oxygen: `2/5`
- Research: `2/5`
- Freezer: `5/5`
- Greenhouse: `5/5`
- Living: `2/5`

Standard Repair 효과:

- 일반 5-Max Facility: `+1 Integrity`
- Hull: `+4`
- 기존 설계상 Tech Lv3 TEMP 조건 사용
- 일반 수리 2h

중요:

- Research Restoration은 별도 공짜/추가 행동이 아니라 **D3~D5의 Standard Repair 슬롯을 소비**하도록 W1 Master에 통합했다.
- Research Facility Integrity와 Research Restoration의 기록/데이터 복원 진행도는 개념적으로 분리한다.

## 6.1 최신 W1 Master hotfix: v3.7

2026-08-25 사용자 Unity 테스트에서 v3.6은 D3 정비 콘솔에서 Research Restoration만 보이는 문제가 확인되었다.

원인:

- 일반 `FLOW_W1_SHIP_REPAIR_D3~D7`에 ContentDesign에 없던 `STA_W1_SHIP_OVERVIEW_SEEN == TRUE` 선행조건이 잘못 추가되어 있었음.

v3.7 수정:

- 일반 Standard Repair의 D1 Ship Overview 선행조건 제거.
- D3~D7 날짜 + 해당 일자의 `STA_W1_REPAIR_Dx_USED == FALSE`가 핵심 해금 조건.
- D3에서는 정비 콘솔에서 다음 **두 Interaction entry가 동시에 보여야 정상**:
  1. `수송선 시설을 한 곳 복구한다`
  2. `연구실의 죽은 화면을 살펴본다`
- 일반 수리 안에서는 외벽 / 함교 / 엔진 / 산소 / 거주구역 중 선택.
- 어느 쪽을 하든 그 날의 Standard Repair 1회 슬롯을 소비.

사용자는 v3.7 적용 후 노출 문제를 확인했다.

**주의:** 원격 `SandPlanet_Master.xlsx`와 Generated CSV가 이 v3.7과 동일하게 commit/push 되었는지는 Codex가 작업 시작 시 반드시 `git status`와 diff/파일 상태로 확인해야 한다. 로컬 변경이 있으면 먼저 보존한다.

---

# 7. Validator 최신 규칙

`SandPlanetSpreadsheetExporter04.cs`를 과거 규칙으로 되돌리지 않는다.

현재 규칙:

- `Active=False` row는 런타임 참조/중복/시간 검증에서 제외.
- `ChoiceID != blank`라고 해서 TimeCost 1h를 요구하지 않음.
- `ACTIVATE_QUEST`도 자동으로 1h를 요구하지 않음.
- `TimeCost < 0`만 오류.
- `SET_STEP` active QuestStep은 `NextStepID` 필수.
- `NextStepID` 존재 여부 확인.
- 같은 Quest의 Step인지 확인.

---

# 8. 이미 해결된 데이터 문제 — 다시 만들지 말 것

1. W1 QuestStep chain blank NextStepID 문제 해결.
2. InteractionFlow duplicate key 문제 해결.
3. 활성 multi-choice row의 blank ChoiceID 문제 해결.
4. Oasis를 호수처럼 묘사하던 구문 수정.
5. Benjamin wife/child 설정 제거.
6. Jina old-friend 설정 제거.
7. Mira quicksand/sacrifice branch 폐기.
8. W1 Standard Repair 5-slot 설계를 ContentDesign에서 다시 찾아 Master로 복구.
9. D3 일반 수리 visibility의 잘못된 Ship Overview gate 제거(v3.7).

---

# 9. Codex가 지금 해야 할 일 — 우선순위

## P0. 로컬/원격 기준점 확정

첫 작업은 구현이 아니라 상태 확인이다.

- `git status`
- `git log --oneline -n 15`
- ContentDesign binary 존재 확인
- Master와 Generated CSV 변경 여부 확인
- Validator 최신 commit 포함 여부 확인
- 사용자가 마지막으로 테스트한 v3.7 Repair Visibility 데이터가 원격에 반영됐는지 확인

로컬 Master/CSV가 modified 상태라면 **먼저 내용 보존 후 사용자에게 commit 범위를 보고**한다. reset/checkout으로 버리지 않는다.

## P1. v3.7 Master + Generated CSV 기준 commit 정리

아직 commit되지 않았다면:

1. `SandPlanet_Master.xlsx`가 v3.7 hotfix 상태인지 확인.
2. Unity에서
   - `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
   - `Validate Generated CSV`
3. D3 정비 콘솔 테스트:
   - Research entry + 일반 수리 entry 둘 다 보임
   - 일반 수리 시설 선택 가능
   - 수리 2h 소모
   - 같은 날 두 번째 Standard Repair 차단
   - Research Restoration을 하면 일반 수리도 같은 날 차단
   - D4 다음 슬롯 재개
4. 이상 없으면 Master + Generated CSV를 하나의 의미 있는 commit으로 정리.

## P2. W2 Standard Repair 7-slot Master migration

ContentDesign에는 이미 존재하나 현재 Master 이식은 미완료 상태다.

- D8~D14 하루 1회 = 총 7 Standard Repair Slot
- **새로 기획하지 말고** `13_FacilityTransition`, `06_ActionBudget`, `19_W2ReverseDesign`, `21_IntegratedRouteSim`을 기준으로 이식.
- W2의 evacuation / preserve / character action과 동일한 시간 예산에서 경쟁하도록 유지.
- Facility Integrity 및 D15 시작값 계산이 ContentDesign 역산과 일치해야 함.
- W1의 day-used state 패턴을 그대로 복사하기 전에 W2의 existing action/event 구조와 충돌 여부를 확인.
- W2 migration 후 Export → Validator → D8~D14 repair slot playtest.

## P3. W2/W3 시스템/대사 정합성 QA

수리 migration 이후:

- 최신 Canon이 Master의 W2/W3 Interaction/Event/Quest text에서 구설정으로 되돌아간 곳이 없는지 검색.
- Jina / Diya / Benjamin / Mira / Oasis 우선.
- ContentDesign의 `28_W2_NarrativeAuthoring`, `29_W3_NarrativeAuthoring`, `30_EndingNarrative`와 Master 실제 데이터 비교.
- 필요한 대사 migration은 DATA/CONTENT 작업으로 처리. Runtime 하드코딩 금지.

## P4. D1→D21 통합 플레이테스트

- Quest progression
- Time budget
- Repair budget
- Character Full Route eligibility
- Facility integrity
- W2 preserve/evacuation
- W3 stance/ending 조건
- log/hover/choice presentation

모든 계산은 ContentDesign → Master → Runtime 순서로 추적한다.

---

# 10. 작업 중 금지사항

- `Generate Prototype 0.4` 일반 작업에서 실행 금지.
- Generated CSV 직접 authoring 금지.
- ContentDesign에 있는 수송선 시스템을 새로 재설계하지 말 것.
- 최신 Canon을 과거 docs의 구설정으로 되돌리지 말 것.
- DATA로 해결 가능한 것을 새 MonoBehaviour / hardcoded character branch로 만들지 말 것.
- core manager에 특정 Character ID / Quest ID 전용 if문을 임의 추가하지 말 것.
- 여러 component가 같은 UI/state의 final writer가 되게 만들지 말 것.
- local modified binary xlsx를 확인 없이 checkout/reset하지 말 것.
- 1.5h historical design 값을 Unity Runtime에 그대로 넣지 말 것. 현재 Runtime `TimeDelta`는 integer-hours.

---

# 11. 일반 데이터 작업 절차

1. ContentDesign에서 의도 확인.
2. Master에서 실제 이식 상태 확인.
3. Runtime이 해당 표현을 지원하는지 확인.
4. Master 수정.
5. Unity:
   - `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
   - `Validate Generated CSV`
6. 실제 Scene에서 최소 회귀 테스트.
7. `git status` 확인.
8. 하나의 목적에 맞는 파일만 commit.
9. 완료 보고:
   - 무엇을 바꿨는지
   - ContentDesign 수치/의도를 변경했는지 여부
   - 새 state/owner를 만들었는지 여부
   - 테스트 결과
   - 남은 TEMP/TBD

---

# 12. 과거 문서의 지위

기존 `docs/*.md`는 여전히 구조/역사/세부 설계 참고에 유용하다. 하지만 일부는 최신 Canon 이전에 작성되었다.

우선순위:

1. **이 문서 (`CODEX_HANDOFF_2026-08-25.md`)**
2. `Assets/SandPlanet/Data/Design/SandPlanet_ContentDesign.xlsx` + `31_CanonSync_v3.1`
3. 현재 `SandPlanet_Master.xlsx`
4. Runtime/Validator의 실제 구현 계약
5. `Assets/SandPlanet/Data/Design/README.md`
6. 기존 `docs/W1_NARRATIVE_SYSTEM_v1_7.md`, `NARRATIVE_ARCS_AND_INTEGRATION_v0_2.md`, `NARRATIVE_REVERSE_DESIGN_v0_1.md` 등

과거 문서에 `Jina=친구`, `Benjamin=아내/아이`, `Mira=모래지옥`, `Oasis=호수`와 같은 표현이 남아 있어도 **그 표현은 폐기된 역사적 초안**으로 본다.

---

# 13. Codex 첫 응답 권장 형식

작업을 이어받았을 때 바로 코드를 바꾸지 말고 사용자에게 다음처럼 짧게 보고한다.

- 현재 branch / worktree clean 여부
- ContentDesign / Master / Generated CSV 상태
- v3.7 W1 repair visibility 반영 여부
- 지금 하려는 작업이 DATA / PRESENTATION / GAMEPLAY RULE / NEW SYSTEM 중 무엇인지
- 수정할 파일 범위
- 로컬 Unity 확인이 필요한 지점

이후 최소 범위로 작업한다.
