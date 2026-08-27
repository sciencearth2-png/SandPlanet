# SandPlanet Narrative Reverse Design — v0.1

> **Status:** Living design document / narrative-system convergence draft
>
> **Purpose:** 엔딩에서 역산해 Week 3 → Week 2 → Week 1의 서사, Character Quest, Side Event, 시설/생존 시스템을 구체화하기 위한 작업 문서다.
>
> **Audience:** 기획 작업자, ChatGPT, Codex. 구현 전에 이 문서의 **확정 / TEMP / TBD** 구분을 확인한다.
>
> **Current coverage:** 엔딩 구조 + Week 3 상세. Week 1~2의 구체화는 아직 진행 전이며 이후 이 문서에 추가한다.

---

## 0. 문서 사용 원칙

- 이 문서는 기존 `W1_NARRATIVE_SYSTEM_v1_7.md`를 즉시 폐기하는 문서가 아니다. **현재 진행 중인 엔딩 역산 기반 재설계의 상위 작업 문서**다.
- 현재 구현과 충돌하는 새 설계는 임의의 patch/manager/MonoBehaviour로 덧붙이지 않는다. 먼저 owner와 데이터 계약을 정한다.
- 수치 밸런스는 서사/시스템 구조가 닫힌 뒤 별도로 조정한다. 현재 기재된 시간/요구 레벨/내구도 수치는 별도 표기가 없더라도 대부분 **초기 설계값(TEMP)** 이다.
- Week 3의 기존 추상 `NAV/SUPPLY/TECH/HABIT_STABILITY` 구조와 새 시설 내구도 시스템을 동시에 유지하지 않는다. 새 구조를 구현할 때는 기존 추상 Stability W3를 교체하는 방향으로 마이그레이션한다.

---

# 1. 엔딩 역산 — 현재 목표 구조

## 1.1 Bad Ending 1 — 제이 사망 / 수송선 생존 붕괴

**의미:** 플레이 중 발생한 생존 위기를 해결하지 못해 제이 또는 공동체가 직접적으로 죽는다.

대표 경로:
- Week 1~2 Side 생물 인카운터(샌드웜, 개미지옥 등)를 방치해 후속 대형 사고가 발생하고, 마지막 해결 조건을 충족하지 못함.
- Week 3 생명유지/시설 위기를 제한시간 안에 해결하지 못함.
- 수송선 **전체 외벽 내구도 = 0** 이후 발생하는 Global Emergency를 해결하지 못함.
- **거주 구역 본체 내구도 = 0** → `떼죽음` → Bad Ending.

이 엔딩은 모든 세부 루트가 필수는 아니며, 주요 목적은 Week 1~3의 위험 신호와 준비가 실제 생존 결과로 이어지게 하는 것이다.

## 1.2 Bad Ending 2 — 수송선이 폭풍을 버틸 수 없음

**의미:** Week 2 종료 시점의 수송선 준비가 부족해 Week 3의 폭풍 생존 자체가 성립하지 못하거나 매우 빠르게 붕괴한다.

- Week 2의 구체적인 수송선 준비/수리 항목은 아직 재설계 전.
- 기존 `외벽/코어` 초안은 Week 3의 새 시설 구조와 함께 다시 역산한다.
- **TBD:** Week 2 종료 시 Bad Ending 2를 즉시 판정할지, Week 3 초반의 사실상 회복 불가능한 붕괴 상태로 진입시킬지.

## 1.3 Bad Ending 3 — 선택지가 없어 모래 행성에 잔류

**의미:** Day 21까지 출항 가능한 수송선도 만들지 못했고, 행성의 지속가능성도 확보하지 못했다. 사람들은 적극적으로 정착을 선택한 것이 아니라 **다른 선택지가 없어 남는다.**

## 1.4 Normal Ending 1 — 어쩔 수 없이 전원 출항

> 이전 명칭의 Normal Ending 2와 번호를 교체했다.

현재 조건 방향:
- 모래 행성 지속가능성 = 낮음
- 수송선 상태 = 보통 이상
- 샘 생존
- 페이 / 벤자민 중 최소 1명은 사망 상태가 아님

행성에 남을 근거가 없기 때문에, 의견 차이가 있어도 전원이 떠나는 결말이다.

## 1.5 Normal Ending 2 — 일부만 출항

> 이전 명칭의 Normal Ending 1과 번호를 교체했다.

현재 조건 방향:
- 모래 행성 지속가능성 = 보통 또는 높음
- 수송선 상태 = 보통 이상
- 샘 생존
- 페이 / 벤자민 중 최소 1명 이상 `출항`
- True Ending 조건 불만족

선장과 다수의 지지 아래 떠나는 사람과 남는 사람이 갈린다.

## 1.6 Normal Ending 3 — 다수결로 거주

현재 조건 방향:
- 모래 행성 지속가능성 = 보통 또는 높음
- 수송선 상태 = 수리 필요 / 보통 / 좋음 중 허용 범위
- 지나 / 디야 중 최소 1명 생존
- 페이 / 벤자민 중 최소 1명 `거주`
- Normal Ending 2 / Hidden Ending 조건 불만족

## 1.7 True Ending — 전원 출항

핵심 의미는 **전원 찬성**이 아니라 다음 두 가지다.

1. **전원 생존**
2. **강한 반대를 제거**

Character stance 목표:
- 샘: 기본적으로 끝까지 강한 `출항`파.
- 보리치: 기본적으로 끝까지 강한 `출항`파.
- 디야: 강한 `거주`파. 최대 변화는 `고민 중`.
- 지나: 강한 `거주`파. 최대 변화는 `고민 중`.
- 페이: 기본 `거주` 성향이지만 상대적으로 출항 쪽 변화가 쉬움.
- 벤자민: 기본 `거주` 성향. 변화가 어렵지만 `출항`까지 가능.

현재 조건 방향:
- 샘 / 지나 / 페이 / 벤자민 / 보리치 / 디야 전원 생존
- 전원 stance가 `출항` 또는 `고민 중`
- 수송선 상태 = 보통 또는 좋음

## 1.8 Hidden Ending — 전원 거주 / 지속가능성 재점검

핵심은 단순 설득이 아니라 **처음에는 불가능하다고 본 모래 행성 거주 가능성을 다시 검증하는 것**이다.

현재 조건 방향:
- 모래 행성 지속가능성 = 높음
- 보리치 생존
- 페이 / 벤자민 중 최소 1명 `거주`
- 추가 stance/생존 세부조건은 Week 1~2 구체화 이후 재검토.

---

# 2. 모래 행성 지속가능성 — 현재 원칙

지속가능성은 별도의 복잡한 점수를 만들지 않는다.

**핵심 입력:**
- `거주지 보존도`
- `오아시스 보존도`

묘지 보존도는 지속가능성에 포함하지 않는다.

현재 구현의 `STA_PRESERVE_GRAVEYARD`는 정서적/기억 보존 용도로만 남기거나 향후 정리한다. 지속가능성 계산에는 사용하지 않는다.

## 2.1 거주 가능성 재점검 Quest

재점검은 Hidden Ending 전용이 아니다.

- 재점검이 열리지 않으면 행성 지속가능성은 사실상 `낮음`에 고정.
- Normal 2 / Normal 3 / Hidden을 열기 위해서도 핵심적인 공통 경로가 된다.

재점검 요청 입구는 3개 중 하나면 충분하다.

### A. 벤자민 → 보리치
- 벤자민이 아직 `거주` 성향일 때, 제이에게 거주 가능성을 다시 확인해달라고 요청.
- **TBD:** 과거 초안의 `묘지 보존도 높음` 조건은 현재 “묘지 보존도를 핵심 시스템으로 다루지 않는다”는 결정과 충돌한다. Week 1~2 재설계에서 `추모/묘지 관련 Character Quest 완료` 등으로 교체 여부를 결정한다.

### B. 미라 → 보리치
- **미라(TEMP 이름):** 지나의 딸이며 디야가 돌보는 아이 중 한 명. 호감도 시스템은 사용하지 않는 서브 캐릭터.
- Week 2 개미지옥 사건에서 제이가 구조하거나, 조건에 따라 지나가 미라를 살리고 희생할 수 있다.
- 지나 사망 시 미라가 묘지의 지나를 보고 싶어 하는 후속 인카운터 → 거주 가능성에 대한 질문 → 보리치에게 연결.

### C. 디야 → 보리치
- Week 1~2 동안 거주지를 잘 유지했을 때 Day 9~10 전후 어린이/공동체 이벤트.
- 큰 Will 회복 Event이면서, 디야가 제이에게 거주 가능성을 다시 묻게 되는 연계 Quest의 시작점.

세 경로는 공통 상태/이벤트로 합류한다.

보리치가 사망한 경우 재점검은 진행 불가. 플레이어에게 “그 사람이 살아 있었다면 열렸을 미래가 닫혔다”는 후속 Event를 보여준다.

보리치가 생존해도 연구에 필요한 시설 상태가 충분하지 않으면 재점검은 진행할 수 없다.

---

# 3. Week 3 — 내러티브 목표

Week 3의 핵심 경험은 **“정신이 없다 → 버틴다 → 이제는 미래를 결정해야 한다”**다.

기존의 추상적인 4 Stability 관리보다, 실제 장소/시설/사람 배치와 긴급사태를 통해 압박을 체감하게 한다.

## 3.1 Day 15

- 수송선 내부 4구역으로 플레이 공간 전환.
- 폭풍의 본격적인 시설 사고와 긴급수리 시작.
- 플레이어는 각 핵심 인물의 위치와 시설 내구도를 동시에 보기 시작한다.
- Day 15 자체는 “정신이 없다”는 감각을 가장 강하게 주는 날.

### Day 15 하루 종료 이후 — 미래 결정 선언

**기존 Day 17 선언안에서 변경.**

Day 15 하루 평가/종료 직후, 다음 날로 넘어가기 전에 스토리 Event가 발생한다.

1. 보리치가 제이/샘에게 말한다.
   - 이런 모래폭풍을 두 번 견디기 어렵다.
   - 폭풍이 지나면 빠르게 출항 준비를 하거나, 이곳에 남는 결정을 확실히 해야 한다.
2. 샘이 이 의견을 받아 공동체 전체에 선언한다.
   - 폭풍이 끝나는 날, 살아남은 사람들은 남을지 떠날지 반드시 선택한다.
   - 더 이상의 지연은 없다.
3. 방송/선언 후 샘은 제이에게 `저질렀다...` 같은 반응을 보인다.
4. 주요 인물 몇 명의 짧은 반응 텍스트 후 Event 종료.

**Story protection:** 이 선언 Event가 완료되기 전에는 샘과 보리치가 치명적 희생으로 사망하지 않도록 한다. 이를 노골적인 무적으로 보이지 않게 Event 행동을 다르게 authoring한다.

조타실이 이미 폐쇄된 경우 방송을 고집하지 않는다. 살아남은 사람들을 안전한 구역에 모아 직접 선언하는 대체 연출을 사용하며, Main 진행 결과는 동일하게 만든다.

## 3.2 Day 16~19

- 선언 이후에도 폭풍은 계속되고 시설 사고/인물 배치 압박은 지속된다.
- Character Quest, 미래에 대한 반응, 시설 Emergency가 서로 시간을 경쟁한다.
- 핵심 인물의 죽음은 이후 Character/Ending 경로를 실제로 닫거나 바꾼다.

## 3.3 Day 20

- 모래폭풍이 옅어지고 있다는 관측.
- 생존만 보던 시선이 Day 21의 미래 선택으로 이동하기 시작한다.

## 3.4 Day 21

- 폭풍이 그침.
- 살아남은 인물 / 남은 시설 / 행성 지속가능성 / 수송선 항해 가능성 / 각자의 stance를 확인.
- 최종 미래 결정 Sequence로 진입.

---

# 4. Week 3 공간 구조 — 4 Location / 8 Facility

NPC와 플레이어가 실제로 오가는 공간은 **4개만 유지**한다. 사람이 8개 방으로 과도하게 분산되는 것을 방지한다.

| 이동 Location | 내부 관리 Facility | 공간 정체성 |
| --- | --- | --- |
| **조타실** | 조타실 본체 | 지휘 / 항해 / 통신 / 안내방송 / 결정. 의도적으로 하위 시설이 없는 유일한 구역. |
| **엔지니어 구역** | 엔진실 / 산소보급실 / 원격 외벽 관리 패널(전체 외벽) | 동력 / 생명유지 / 선체 대응. |
| **연구 구역** | 연구실 / 냉동창고 / 온실 | 분석 / 장기 생존 자원 / 재배. |
| **거주 구역** | 거주 구역 본체 | 주민 / 의료 / 휴식 / 공황 관리. |

현재 Location ID는 새 ID를 늘리지 않고 다음처럼 재사용하는 방향을 우선한다.

- `LOC_05_COMMAND` → 조타실
- `LOC_06_SUPPLY` → 엔지니어 구역
- `LOC_07_TECH` → 연구 구역
- `LOC_08_HABIT` → 거주 구역

Facility 내구도는 서로 독립 값이다.

---

# 5. 시설 내구도와 일반 수리

모든 주요 Facility에는 독립 `Integrity`가 있다.

**일반 원칙:**
- 수리로 Max를 초과하지 않는다.
- 피해로 0 미만으로 내려가지 않는다.
- 정확한 Max/시작값은 밸런스 단계에서 결정.

## 5.1 제이 일반 수리 — TEMP 수치

모든 수리 가능한 시설에서 무한 반복 가능.

- 1시간 + Technical 5 이상 → 시설 내구도 +1
- 2시간 + Technical 3 이상 → 시설 내구도 +1

## 5.2 벤자민 / 페이에게 일반 수리 요청 — TEMP

일반 시설 수리를 효율적으로 맡길 수 있는 NPC는 **벤자민 / 페이**다.

- 과거 초안의 `보리치` 표기는 오타이며 폐기.
- 벤자민은 현장 정비/구조/수리에 특히 강함.
- 페이는 전기/동력/시스템을 이해하고 수리할 수 있으나, 일반 현장 수리에서는 벤자민이 더 뛰어나다는 텍스트를 사용 가능.
- 정확한 시간/내구도 회복량은 밸런스 단계에서 조정.

---

# 6. NPC 이동 — Week 3 전략 요소

Week 3의 생존한 모든 핵심 인물에게 다른 4개 Location 중 하나로 이동해달라고 요청할 수 있다.

### 요청 조건 — TEMP
- Affinity 3 이상
- 또는 Affinity 2 이상 + Will 1 사용

### 이동 규칙
- 이동 요청 자체는 시간을 소모하지 않는다.
- 해당 NPC는 **1시간 뒤** 목적지에 도착한다.
- 이동 중 NPC에게 말을 걸면 “어디로 이동 중인지” 제이에게 알려준다.
- NPC가 긴급수리/희생 행동 등 `Busy` 상태라면 이동 요청과의 충돌을 방지해야 한다.

NPC Location은 기존 `NpcSchedules.csv`의 Schedule을 삭제하는 것이 아니라:

1. authored 기본 Schedule
2. runtime 이동 override
3. 강제 대피 override

순으로 우선하도록 확장하는 방향이 적절하다.

---

# 7. 외벽 뚫림(Breach) — 공통 Emergency

각 사람이 머무는 Location에는 `외벽 뚫림` Emergency가 발생할 수 있다.

**Tick:** 미해결 상태에서 4시간마다 피해 갱신.

## 7.1 공유 피해

### 조타실 Breach
- 조타실 본체 -1
- 전체 외벽 -1

### 엔지니어 구역 Breach
- 엔진실 -1
- 산소보급실 -1
- 전체 외벽 -1

### 연구 구역 Breach
- 연구실 -1
- 냉동창고 -1
- 온실 -1
- 전체 외벽 -1

### 거주 구역 Breach
- 거주 구역 본체 -1
- 전체 외벽 -1

한 구역의 Breach 때문에 내부 시설 수만큼 전체 외벽을 여러 번 깎지 않는다. **Breach Tick 1회당 전체 외벽 -1**이다.

## 7.2 제이의 긴급수리 — TEMP

- 1시간 + Technical 7 → 긴급수리
- 2시간 + Technical 5 → 긴급수리

성공하면 해당 Breach 종료.

---

# 8. Breach 발생 시 NPC 자동 반응

NPC의 **현재 Location**이 실제 결과를 바꾼다.

## 8.1 벤자민 / 페이

- 둘 중 한 명이 해당 구역에 있음 → 2시간 자동수리
- 둘 다 있음 → 1시간 자동수리
- 수리 중 대화가 별도로 보인다.

## 8.2 보리치 / 지나 — 단독

해당 구역에 둘 중 한 명만 있으면 스스로 파손부를 막으려 한다.

- `보리치의 희생` 또는 `지나의 희생` Emergency 발생.
- 4시간 내에 수리가 끝난다.
- 제이는 1시간 + Will 1을 사용해 해당 인물을 설득하고 다른 구역으로 철수시킬 수 있다.
- 4시간 동안 개입하지 않으면 **수리는 완료되지만 해당 인물은 사망**한다.

## 8.3 보리치 + 지나 — 동시

**확률 사망 초안 폐기.**

- `지나와 보리치의 분투` Event 발생.
- 두 사람이 함께 있으면 4시간 후 **무조건 수리 성공 + 둘 다 생존**.
- 이는 리플레이에서 NPC 배치를 학습한 플레이어가 활용할 수 있는 공략 요소다.

## 8.4 샘 / 디야

- Breach가 난 구역에 있으면 전문 수리를 시도하지 않는다.
- 1시간 뒤 다른 안전 구역으로 이동한다고 알린다.

---

# 9. 산소 공급 중단 — 연쇄 Emergency

산소보급실과 각 Location의 산소 문제는 Breach와 별도 상태다.

- 산소보급실이 Breach 상태로 4시간 방치되면 다른 사람이 머무는 구역에 `산소 공급 중단`이 발생할 수 있다.
- `산소 공급 중단` 역시 미해결 시 4시간마다 효과 갱신.

공유 피해:

### 엔지니어 구역 Oxygen Failure
- 엔진실 -1
- 산소보급실 -1

### 연구 구역 Oxygen Failure
- 연구실 -1
- 냉동창고 -1
- 온실 -1

### 조타실 / 거주 구역
- 해당 본체 Integrity -1

**산소 공급 중단은 전체 외벽을 깎지 않는다.**

NPC 반응:
- 벤자민/페이 한 명 → 2시간 수리
- 둘 다 → 1시간 수리
- 다른 인물 → 1시간 뒤 다른 구역으로 철수

---

# 10. 전체 외벽 — Global Hull

`전체 외벽`은 특정 방이 아니라 수송선 전체가 폭풍에 버틸 수 있는 **Global Integrity**다.

평상시에는 엔지니어 구역의 `원격 외벽 관리 패널`로 상태를 확인/수리할 수 있다.

## 10.1 전체 외벽 = 0

**장소와 무관한 Global Forced Event**로 처리한다.

의미:
- 특정 부위의 구멍이 아니라 수송선 전체가 모래폭풍에 뜯겨나가기 직전.

현재 해결안 — TEMP 수치:
- 2시간 + 벤자민 희생 → 수송선 유지, 벤자민 사망
- 2시간 + 페이 희생 → 수송선 유지, 페이 사망
- 4시간 + Technical 8 → 제이 긴급수리
- 6시간 + Technical 6 → 제이 긴급수리
- 제한 내 미해결 → Bad Ending

엔지니어 구역이 이미 폐쇄되었더라도 이 최종 Emergency는 접근 가능해야 한다.

---

# 11. 본체 0 → Location 영구 폐쇄

다음 **본체** Integrity가 0이 되면 해당 사람이 머무는 Location은 Week 3 종료까지 복구 불가다.

- 조타실 본체 0 → 조타실 폐쇄
- 엔진실 본체 0 → 엔지니어 구역 폐쇄
- 연구실 본체 0 → 연구 구역 폐쇄

구역 폐쇄 시:
- 해당 구역의 모든 핵심 인물이 “이곳은 더 버틸 수 없다. 다른 곳으로 이동하겠다”는 대사를 한다.
- 1시간 뒤 남아 있는 안전 구역으로 대피.
- 제이는 해당 구역을 다시 수리해 열 수 없다.
- 그 구역에서 발생한 신규 Location Quest는 더 이상 Offer되지 않는다.

## 11.1 진행 중인 Location Quest

이미 ACTIVE인 **구역 파생 Quest**가 있다면 즉시 삭제하지 않는다.

1. 해당 Quest의 `진행 불가` Event를 보여준다.
2. 플레이어가 왜 진행할 수 없는지 인지한다.
3. Quest를 `CANCELLED/UNAVAILABLE` 계열 상태로 종료하고 Tracker에서 제거한다.

Character에서 파생된 Quest는 구역 폐쇄만으로 generic 자동삭제하지 않는다. 해당 Character Quest에 필요한 시설이 사라진 경우 캐릭터 전용 실패/변형 Event로 처리한다.

예:
- 페이 공연 준비 중 조타실 폐쇄 → 페이가 방송 장치를 사용할 수 없게 된 상황을 직접 반응.
- 보리치 재점검 연구 중 연구 구역 폐쇄 → 보리치가 재조사를 계속할 수 없음을 직접 설명.

---

# 12. 냉동창고 / 온실 — 항해 가능성 자산

두 시설은 Week 1부터 정상 작동 상태로 시작한다.

## 12.1 냉동창고

- Integrity 0 → `냉동창고 기능 상실` Emergency
- 6시간 안에 복구하지 못함 → `기능 정지`

## 12.2 온실

- Integrity 0 → `온실 기능 상실` Emergency
- 24시간 안에 복구하지 못함 → `기능 정지`

## 12.3 결과

냉동창고 / 온실 중 하나라도 `기능 정지`가 확정되면:

**수송선 상태 = 항해 불가**로 고정.

이 시설들의 Integrity 0은 연구 구역 폐쇄를 직접 의미하지 않는다. 연구 구역 폐쇄 판정은 **연구실 본체 Integrity**가 담당한다.

---

# 13. 거주 구역 — Panic / Bad Ending

거주 구역은 다른 Location과 다르게 본체가 0이 되면 대피 후 폐쇄가 아니라 직접 공동체 붕괴로 간다.

## 13.1 Panic

거주 구역 Integrity ≤ 2:
- `패닉!` Event 발생.
- 1시간 + Social 6으로 진정 가능 — TEMP.

진정하지 못하면:
- 2시간마다 주민이 다른 Location/시설로 몰려다니며 시설 Integrity -1 Event를 발생시킨다.
- 대상에는 거주 구역도 포함된다.

## 13.2 거주 구역 Integrity = 0

- `떼죽음` Event
- Bad Ending

냉동창고/온실/다른 하위 시설 0에는 이 규칙을 적용하지 않는다.

---

# 14. 페이 Character Arc — 전자음악 / 공연

페이의 **개인 서사 핵심은 수송선 수리 자체가 아니다.**

페이는 전자음악/일렉 기타를 연주해보고 싶은 사적인 소망이 있다. 모래 행성 생존 상황에서는 사치처럼 느껴져 쉽게 말하지 못한다.

## 14.1 1차 Character Quest

- 페이의 음악에 대한 꿈/소망을 알게 됨.
- 일단 Character Quest 한 Episode가 완료됨.

## 14.2 공연 Quest 재개

조건 예시:
- 조타실 수리도가 일정 이상(초안: 2 이상)

페이가:
- 조타실의 안내방송 기능을 공연 출력에 활용할 수 있다고 생각.
- 소리를 만들기 위해 엔진실의 동력을 응용할 수 있다고 제안.

따라서 페이의 개인적 꿈을 따라가다 자연스럽게:
- 조타실
- 엔진실

이라는 출항에 중요한 Facility를 수리하게 된다.

## 14.3 공연 결정

준비 후 페이가 샘에게 공연 허가를 요청.

샘:
- 이 시기에 시간을 써도 되는가 고민.
- 모두 지쳐 있으니 도움이 될 수도 있다고 판단.
- 최종 의견을 제이에게 요청.

제이가 허용하면 약 6시간짜리 대형 Event — TEMP.

공연 효과 방향:
- 공동체 Will 대폭 회복
- 페이의 꿈 실현
- 벤자민이 위로받음
- 지나가 미라가 즐거워하는 모습을 봄
- 샘이 “출항 후에도 음악을 들으며 갈 수 있겠다”는 미래를 상상
- 페이 stance → 출항 쪽 1단계
- 벤자민 stance → 출항 쪽 1단계

즉 이 Quest는 “수리 Quest”가 아니라 **삶을 다시 상상하게 하는 Character Quest가 결과적으로 시설과 출항 가능성을 연결하는 구조**다.

---

# 15. 샘 Character Arc — 책임을 들어주는 비용

중요한 Main Quest 직후 반복적으로 샘 Character Quest Episode가 열린다.

샘은 제이에게 지도자로서 책임의 무거움을 털어놓는다.

선택:
- 들어준다
- 들어주지 않는다

어느 쪽이든 해당 Episode 자체는 완료된다.

## 15.1 들어준다 — TEMP 수치

- 3시간
- Will -2
- 반복될수록 Social 요구 증가: 2 → 3 → 4 → 5 ...

## 15.2 반복적으로 들어주지 않는다

누적에 따라 일상 대사와 상태가 악화된다.

초안:
- 2회: “정신이 이상해지는 것 같다.”
- 3회: “모든 걸 포기하고 싶다.”
- 4회 전후: Week 3 본격 붕괴

정확한 횟수는 Main Quest Episode 개수 확정 후 밸런싱.

## 15.3 `정신 차려, 샘!`

샘이 조타실을 떠나 엔진실에 틀어박힌다.

6시간 제한 Emergency Character Quest — TEMP.

해결 선택 초안:
- 1시간 / Personal 8 + Social 10
- 4시간 / Personal 5 + Social 10

실패:
- 엔진실 쪽 큰 파손 Event
- 샘 사망
- 후속 외벽/시설 Emergency

**Story protection:** Day 15 종료 후 미래 결정 선언이 완료되기 전에는 이 치명 루트를 열지 않는다.

---

# 16. Quest 표시 / 상태 원칙

현재 Tracker는 실제 런타임에서 `ACTIVE` Quest만 표시한다. 따라서 “미래 Character Quest가 6개 상시 표시”되는 문제는 단순 LOCKED 표시 문제가 아니라 **장기간 ACTIVE인 Character Quest 구조**에서 발생한다.

새 원칙:
- Character Arc 전체를 하나의 3주짜리 ACTIVE Quest로 두지 않는다.
- Episode 완료 → 다음 Episode는 LOCKED.
- 실제 Offer 조건이 열릴 때만 수락 가능.
- 수락 후 ACTIVE가 되었을 때만 Tracker 표시.

Main / Character / Side / Daily 모두 동일한 원칙을 따른다.

구역 폐쇄 같은 외부 요인으로 진행 불가가 된 Quest는 `FAILED`와 의미가 다르므로 QuestService에 `CANCEL/UNAVAILABLE` 계열 종료 계약을 추가하는 방안을 검토한다.

---

# 17. Quest Deadline 표시 — 필요 시스템

Emergency Quest에는 남은 시간을 표시해야 한다.

표현 방향:
- 24시간 미만 → `남은 시간: N시간`
- 24시간 이상 → `남은 기간: N일` 또는 시간/일 혼합

적용 후보:
- `정신 차려, 샘!`
- Breach
- 산소 공급 중단
- 냉동창고 기능 상실
- 온실 기능 상실
- 캐릭터 구조/희생

**구현 전 데이터 계약 필요:** Quest 자체의 Deadline인지 Event/Incident의 Deadline인지, 상대시간/절대시간을 어떻게 기록할지 먼저 정의한다.

---

# 18. 하루 종료 / 날짜 사이 Narrative

날짜 전환을 단순 버튼 → 다음날로 처리하지 않는다.

## 18.1 Day N 하루 평가

하루 종료 시 일기/회고 형식으로:
- 그날 완료한 Quest
- 중요한 Event
- 사망 / 실패 / 진행불가
- 중요한 선택

을 서사 문장으로 복기한다.

Quest/Event에는 필요 시 `DayRecapText` 계열 authoring 필드를 추가한다.

그 뒤 주요 수치 변화 요약:
- 스탯/XP
- 호감도
- Will
- 시설 Integrity
- 보존도
- Character stance 등

## 18.2 날짜 사이 Main Narrative

하루 평가 후 Day N+1 진입 전/후에 짧은 기본 서사를 넣는다.

- 다음 Main/Character Quest의 도입
- 밤/새벽에 발생한 변화
- 다음 날 플레이어가 반드시 알아야 하는 상황

연출상 전날 밤에 보여도 되고 다음날 아침에 보여도 된다.

## 18.3 Mandatory Main Gate

특정 Main Story Beat를 완료하지 않으면 하루 종료를 막거나, 시간 진행 행동을 제한할 수 있다.

정확한 Gate 규칙은 전체 Week 1~3 Main Beat를 정리한 뒤 확정한다.

---

# 19. 모래폭풍은 밤에도 계속된다 — Overnight Phase 제안

> **현재 제안 / 아직 최종 승인 전.**

`22:00 하루 종료`를 **폭풍이 멈추는 시각**으로 해석하지 않는다.

22:00은 제이의 **직접 지휘/자유 행동 플레이 구간이 끝나는 시점**이다. 수송선은 이후 야간 비상근무 체제로 들어가고 모래폭풍은 밤새 계속된다.

권장 Narrative:
- 야간조가 시설 감시와 기본적인 임시 대응을 이어간다.
- 제이는 모든 일을 24시간 혼자 처리하는 인물이 아니며, 하루 평가 후 잠시 휴식/교대한다.
- Day Start Narrative에서 “밤새 몇 번의 경보가 울렸다”, “야간조가 버텼다”, “새벽에 상태가 더 악화됐다” 같은 결과를 보여준다.

### 시스템적으로는

`End Day` 시 22:00 → 다음날 08:00의 **10시간도 실제 세계시간으로 경과**시키는 방향을 권장한다.

- 24시간 온실 Deadline 등은 밤 시간도 포함해 줄어든다.
- 이미 진행 중인 4h/6h 급성 Emergency는 플레이어가 그냥 잠들어 회피할 수 없도록 **하루 종료를 막는 편**이 자연스럽다.
- 긴 Deadline은 밤을 넘길 수 있지만 아침에 남은 시간이 줄어 있다.
- 밤에 새 Random Emergency를 플레이어가 대응할 기회 없이 즉사시키는 방식은 피한다. 야간 신규 사고가 필요하다면 Day Start에 `이미 발생했지만 아직 대응 가능` 상태로 제공하거나 별도 공정성 규칙을 둔다.

이 구조의 장점:
- “폭풍이 밤에만 쉰다”는 내러티브 모순 제거.
- 기존 08:00~22:00 플레이 리듬 유지.
- NPC 공동체가 제이 없이도 최소한의 야간조를 운영한다는 세계관 강화.
- 긴 Deadline은 실제 시간의 압박을 유지하면서, 짧은 Emergency를 수면으로 스킵하는 exploit을 막는다.

**TBD:** Week 3에서만 `밤샘 대응` 선택을 별도로 허용할지 여부. 현재는 추가 복잡도를 막기 위해 보류.

---

# 20. 현재 런타임과의 충돌 점검

## 20.1 그대로 재사용 가능한 구조

- 4개 Week 3 Location: 기존 ID 재명명/역할 재편 가능.
- Generic State/Condition.
- Quest / QuestStep.
- Interaction / Event Flow.
- QuestAction + ProgressEvent 병존.
- Schedule의 authored 기본 위치.
- Event presentation/queue.
- 기존 Presenter/Navigation 역할 경계.

## 20.2 데이터만으로 처리하면 위험한 부분

현재 런타임은 Flow 종료 시 `TimeCost`를 한 번에 적용하고, 상대시간 예약 개념이 없다.

따라서 다음을 단순 EventTrigger CSV 반복으로 억지 구현하지 않는다.
- 1시간 뒤 NPC 도착
- 2시간 뒤 Panic Tick
- 4시간마다 Breach/Oxygen Tick
- 6시간 Deadline
- 24시간 Deadline

또한 연구 구역 Breach는 한 번에 4개 State를 원자적으로 바꿔야 하므로 기존 Event node의 제한된 StateChange 열과 순차 STATE_CHANGE trigger만으로 처리하면 중간상태가 노출될 위험이 있다.

---

# 21. 권장 Gameplay Architecture 확장

새 MonoBehaviour/Finalizer/Guard를 추가하지 않는다.

## 21.1 `Prototype04ShipService` — 신규 pure C# Service 제안

**Owner 책임:**
- Facility Integrity 규칙
- Global Hull
- Max/0 clamp
- 일반/긴급 수리
- Breach/Oxygen 공유 피해의 atomic batch
- Facility function loss
- Location closure 판정
- 최종 ShipCondition 계산

Mutable 값 자체는 기존 원칙에 맞춰 `Prototype04GameState`가 저장하고, ShipService가 규칙을 소유하는 방향을 우선 검토한다.

## 21.2 `Prototype04TimelineService` — 신규 pure C# Service 제안

**Owner 책임:**
- 미래 시점 예약
- Deadline
- periodic Tick
- 시간 경과 시 due item 실행 순서

예:
- NPC_MOVE +1h
- PANIC_TICK +2h
- BREACH_TICK +4h
- OXYGEN_TICK +4h
- FREEZER_FAILURE +6h
- GREENHOUSE_FAILURE +24h

FlowRuntime/EventService/Controller에 각각 타이머 코드를 흩뿌리지 않는다.

## 21.3 `Prototype04ScheduleService` 확장

- authored 기본 Schedule 유지
- runtime requested location override
- forced evacuation override
- moving/busy/dead 상태와 충돌하지 않는 위치 결정

NPC 위치의 단일 authority는 계속 ScheduleService다.

## 21.4 `Prototype04QuestService` 확장

- `CANCEL_QUEST` / `UNAVAILABLE` 등 진행불가 종료 계약 검토
- Location source 기반 Quest 종료는 QuestService가 소유
- Character Quest는 generic Location cancellation 대신 authored branch 사용

## 21.5 Atomic facility damage

연구 구역 Breach처럼 여러 Facility가 동시에 피해를 입을 때:

1. 모든 Integrity 변경을 먼저 계산/적용
2. 최종 상태를 확정
3. 그 뒤 Trigger/Event를 평가

기존 일반 StateChange의 re-entry semantics를 전역으로 바꾸지 않고 ShipService 내부의 시설 피해 계약으로 한정한다.

---

# 22. 기존 Week 3 데이터 — 교체 예정

현재 다음 추상 State는 새 W3를 구현할 때 교체/폐기 후보다.

- `STA_W3_NAV_STABILITY`
- `STA_W3_SUPPLY_STABILITY`
- `STA_W3_TECH_STABILITY`
- `STA_W3_HABIT_STABILITY`

새 시설 Integrity 시스템 위에 이 추상 Stability를 중복 유지하지 않는다.

다음 항목은 새 Day 15~21 Narrative와 대조해 재사용 여부를 별도 판단한다.

- `STA_W3_PRE_STORM_DATA`
- `STA_W3_POWER_PRIORITY`
- `STA_W3_OBSERVATION_PROGRESS`
- `STA_W3_FUTURE_TALKS_OPEN`
- `STA_W3_FUTURE_TALKS_DONE`

현재 데이터는 아직 수정하지 않는다.

---

# 23. 구현 전에 남은 TBD

1. 각 Facility 시작 Integrity / Max Integrity.
2. Week 3 사고 발생 빈도와 정확한 발생 규칙.
3. 최종 `ShipCondition = 항해 불가 / 수리 필요 / 보통 / 좋음` 계산식.
4. 구역 폐쇄 시 각 NPC의 대피 목적지 결정 규칙.
5. Overnight Phase 최종 승인 및 야간 시간의 Deadline 처리 확정.
6. 벤자민 → 보리치 재점검 요청의 새 Trigger. `묘지 보존도` 조건은 재검토 필요.
7. 샘 하소연 Episode 개수와 붕괴 누적 threshold.
8. Quest/Event Deadline authoring schema.
9. Day End Recap 데이터 schema.
10. Mandatory Main Gate의 정확한 시간/행동 차단 규칙.
11. Week 1~2의 생물 위험 `징후 → 대응 기회 → 사고 → Emergency` 구체화.
12. Week 1~2에서 Week 3 Facility 시작상태로 어떤 준비가 이어지는지.

---

# 24. 구현 순서 권장

이 문서는 아직 **전체 Narrative Reverse Design 작업 중간 단계**다. Week 1~2를 설계하기 전에 Week 3 코드를 전부 구현하는 것을 권장하지 않는다.

권장 순서:

1. 엔딩/Week 3 설계 문서 확정.
2. Week 2를 엔딩 + Week 3 시작상태에서 역산.
3. Week 1을 Week 2 시작조건과 Character Arc에서 역산.
4. 전체 21일 Main / Character / Side Beat를 나란히 검토.
5. 수치/시간 예산 밸런싱.
6. workbook/CSV migration plan 작성.
7. 필요한 Gameplay Service 계약 확정.
8. 기존 W3 Stability 데이터 제거/교체.
9. 데이터 → 코드 → Presentation 순으로 구현.
10. Day 1~21 전체 회귀 테스트.

---

# 25. Week 3 회귀 테스트 초안

구현 시 최소 검증:

- NPC가 1시간 이동 예약 후 정확한 Location에 도착한다.
- Busy NPC를 동시에 두 곳에 배치/수리시키지 못한다.
- 연구 구역 Breach Tick 1회가 연구실/냉동창고/온실 각각 -1, Global Hull -1을 정확히 한 번 적용한다.
- 엔지니어 구역 Breach/Oxygen 피해가 엔진실/산소보급실에 동시에 적용된다.
- 보리치+지나가 같은 Breach에 있으면 RNG 없이 둘 다 생존하며 수리 성공한다.
- 본체 0인 조타실/엔지니어/연구 구역은 Week 3 동안 다시 열리지 않는다.
- 구역 폐쇄 시 Location Quest는 진행불가 Event 후 Tracker에서 제거된다.
- Character Quest는 generic 삭제되지 않고 authored 대체 반응으로 간다.
- 냉동창고 기능 정지 또는 온실 기능 정지 시 ShipCondition이 `항해 불가`로 고정된다.
- 거주구역 0에서만 `떼죽음` Bad Ending이 발생한다.
- Global Hull 0 Event는 현재 Location/엔지니어 구역 폐쇄 여부와 무관하게 열린다.
- Day 15 종료 후 미래 결정 선언 Event가 열린다.
- 선언 이전 샘/보리치는 치명 사망 분기로 들어가지 않는다.
- Day 20 폭풍 약화, Day 21 폭풍 종료 Main Beat가 정상 진행된다.
- Overnight Phase 채택 시 22→08의 시간 경과와 Deadline 감소가 정확하다.

---

## 변경 이력

### v0.1
- 엔딩 역산 구조 기록.
- Sustainability / 거주 가능성 재점검 연결 기록.
- Week 3 4 Location / 8 Facility 구조 확정안 기록.
- Breach / Oxygen / Hull / Panic / Location closure 규칙 기록.
- NPC 이동/자동수리/희생 구조 기록.
- 보리치+지나 25% 사망 초안 폐기 → 둘이 함께면 무조건 수리 성공/생존.
- Global Hull 0 → 장소 무관 Global Event로 확정.
- 미래 결정 선언 Day 17 → **Day 15 하루 종료 직후**로 변경.
- 페이 공연 / 샘 책임부담 Character Arc 기록.
- Quest 표시/진행불가/Deadline/Day Recap 요구 기록.
- 현재 런타임 충돌과 ShipService/TimelineService 제안 기록.
- `Overnight Phase`를 밤에도 폭풍이 지속되는 문제의 해결안으로 제안(TBD).
