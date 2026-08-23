# SandPlanet Route Simulation v0.1

> Branch: `content-design-v2`
>
> 목적: `SandPlanet_ContentDesign_v2.xlsx`의 Day 1~21 Action Budget를 기준으로 Normal / True / Hidden 3개 경로를 종이 플레이하여 시간·Will·수리·보존·캐릭터 선행조건이 물리적으로 성립하는지 검증한다.
>
> 주의: 아래의 XP, Affinity, ShipCondition, Sustainability 임계치는 아직 최종 수식이 없다. 임의로 확정하지 않고 **Simulation Proxy**만 사용한다.

## 1. Simulation Proxy

- 직접 행동시간: 매일 08:00~22:00 = 14h.
- Will: Day1 시작 5, Overnight +2, Max 5. 아직 수치가 없는 Event 회복은 계산하지 않음.
- 일반 수리: TEMP `2h → Facility Integrity +1`.
- 벤자민 W2 전환: TEMP 총 시설 수리 +15, 즉 시작 총합 25에서 약 40 도달을 Proxy로 사용.
- True 디야 축제 Gate: D11 전 거주지 보존 행동 2회 이상을 Proxy로 사용.
- Hidden HIGH: 오아시스 보존 3회 + 거주지 보존 3회 + 보리치 재점검 완료를 Proxy로 사용.
- Hidden 연구 조건: 연구실 시작 2/5에서 연구실 대상 수리 3회 → 5/5.
- ShipCondition: 정확 계산식 미정. 영구 기능 정지/거주구역 0/Global Hull 최종 실패가 없고 수리를 지속하면 `항해 가능성 Proxy`만 통과 처리.
- XP/Stat: 성장 Activity 횟수만 기록. 정확한 Lv 도달 여부는 아직 판정하지 않음.
- Affinity: Character 상호작용은 기록하되 정확한 0~5 도달 여부는 아직 판정하지 않음.

## 2. 결과 요약

| Route | 총 사용시간 | 총 자유시간 | Will 사용 | 성장 슬롯 | 수리 횟수 | Oasis | Settlement | 현재 판정 |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Normal 1 후보 | 178h | 116h | 3 | 6 | 18 | 0 | 0 | 시간 PASS / Will PASS. Sustainability LOW 의도. ShipCondition 산식 대기. |
| True 목표 | 222h | 72h | 5 | 5 | 25 | 0 | 2 | 시간 PASS / Will PASS. True 핵심 서사 Flag 확보. 수치식은 PENDING. |
| Hidden 목표 | 194.5h | 99.5h | 3 | 4 | 16 | 3 | 3 | 시간 PASS / Will PASS. HIGH Proxy + 연구실5 + 재점검 달성. 추가 조건 PENDING. |

21일의 직접 행동 가능 총시간은 294h다. W3는 D15~20에 하루 평균 약 3~4h의 실제 Emergency 대응시간을 별도로 소비하는 대표 케이스로 계산했다.

## 3. Normal / 첫 플레이형

핵심 패턴:

- Main을 꾸준히 진행.
- 수송선을 매주 조금씩 복구.
- 벤자민/샘/페이 등 일부 Character Quest를 자연스럽게 진행.
- 미라는 제이가 직접 구조해 치명적 인물 손실을 막음.
- 오아시스/거주지 보존과 Sustainability 재점검에는 거의 투자하지 않음.
- W3에서도 시설 수리를 지속하고 평균 3~4h Emergency를 처리.

결과:

- Sustainability LOW가 자연스럽게 남음.
- 배가 충분히 살아 있다는 전제라면 Normal 1 `남을 근거가 없어 전원 출항`의 기본 첫 플레이 경로로 기능 가능.
- 정확한 Normal 1 확정은 ShipCondition 계산식 이후 다시 검증해야 함.

## 4. True 목표

핵심 패턴:

- W1부터 여섯 인물의 Seed를 적극적으로 회수.
- 샘의 부담을 반복적으로 들어주고 보리치의 과거를 확인.
- 페이 음악 꿈, 지나의 제이 과거, 벤자민 묘지, 디야/아이들의 삶을 모두 챙김.
- D8~9에 거주지 보존 2회를 수행하여 디야 어린이 축제 Route를 열었다고 가정.
- D10 제이가 미라를 직접 구조.
- D10까지 누적 수리 +15를 만들어 벤자민의 수송선 전환 Beat를 열었다고 가정.
- W3 지나 → UNDECIDED, 디야 → UNDECIDED, 페이 공연, 벤자민 Final을 모두 회수.

시간상 결과:

- 총 사용 222h / 자유 72h.
- 가장 빡빡한 날은 D8로 정확히 14h 사용.
- 즉 **True는 시간상 가능하지만, 계획적으로 플레이해야 하는 상위 루트**가 된다.

주의점:

- 누적 수리 25회는 전체 21일 기준이며, 특히 벤자민의 약 40/55 전환을 조기에 열기 위해 W1~2에 반복 수리가 많이 필요하다.
- 이 수리량이 실제 플레이에서 `서사를 이해한 보상`보다 `수리 노가다`처럼 느껴질 위험이 있다.
- 벤자민 전환 Gate를 총 내구도 하나로만 묶을지 재검토 필요.

## 5. Hidden 목표

핵심 패턴:

- W1에서 연구실을 최우선 복구하여 2/5 → 5/5.
- W2에 오아시스 3회 + 거주지 3회 보존.
- 어린이 축제 후 디야 대면에서 제이가 디야에게 공감하는 Hidden 방향을 선택.
- 미라는 제이가 직접 구조해 지나를 살림.
- 벤자민/페이 중 최소 한 명은 STAY를 유지하는 방향.
- D18 보리치가 연구실에서 Sustainability를 재점검.

시간상 결과:

- 총 사용 194.5h / 자유 99.5h.
- 현재 Proxy 기준으로는 True보다 넉넉하게 달성 가능.
- 따라서 실제 Sustainability HIGH 임계치를 너무 낮게 정하면 Hidden이 생각보다 쉬워질 수 있다.

좋은 점:

- Hidden은 `STAY 대사 선택`으로 여는 것이 아니라 **연구실 + 오아시스 + 거주지 + 보리치 생존**이라는 실제 플레이 투자로 열린다.
- 배도 계속 수리하므로 `떠날 수 없어서 남는다`와 구별할 여지가 충분하다.

## 6. 현재 발견된 핵심 문제

### A. Will 존재감 부족

현재 명시된 Will 사용만 계산하면 Overnight +2 때문에 세 Route 모두 거의 항상 다음날 5로 복구한다.

따라서 Will이 핵심 자원으로 남으려면 이후 최소 하나가 필요하다.

- Soft Requirement 보완에 더 자주 사용.
- Character의 어려운 선택에 사용.
- W3 Emergency를 시간 대신 Will로 압축.
- NPC 이동/설득 등 전략적 사용처 확대.

정확한 소모량은 밸런스 단계에서 결정한다.

### B. XP / Stat은 아직 검증 불가

성장 Activity를 일부 넣었지만 행동별 XP가 정의되지 않았다.

다음 단계에서:

- 행동별 XP 보상.
- 주요 Personal / Social / Technical Check 레벨.
- True/Hidden 플레이가 필요한 핵심 Check를 실제로 달성할 수 있는지.

를 정의한 뒤 다시 시뮬레이션해야 한다.

### C. Affinity도 아직 검증 불가

Character Quest를 많이 했다고 Affinity가 정확히 몇이 되는지 알 수 없다.

W3 NPC 이동 TEMP 조건 `Affinity >= 3` 같은 규칙을 사용하려면:

- 시작 Affinity.
- 대화/Quest별 Affinity 증가량.
- 최대 5까지의 성장 속도.

를 정해야 한다.

### D. ShipCondition 계산식 필요

현재 세 Route 모두 수리는 충분히 가능하지만 `보통/좋음`을 무엇으로 판단할지 없다.

특히 다음을 구분해야 한다.

- 전체 외벽(Global Hull)의 비중.
- 7개 시설의 합계/핵심 시설 최소값.
- 냉동창고/온실 영구 기능정지.
- W3 종료 시 폐쇄 구역.

### E. Hidden HIGH 실제 임계치 필요

현재 `오아시스 3 + 거주지 3`은 단지 시뮬레이션용 Proxy다.

실제 Preservation Max/증가량과 재점검 판정식을 정한 뒤 Hidden 난이도를 다시 측정한다.

## 7. 다음 설계 순서

Route Simulation 결과를 기준으로 다음 수치 설계 순서를 권장한다.

1. **수리 효율 / Facility Gate / ShipCondition** — True의 반복 수리 문제와 엔딩 출항 가능성을 먼저 해결.
2. **Preservation 증가량 / Sustainability** — Hidden 난이도 확정.
3. **Will 사용처와 소모량** — 현재 지나치게 여유로운 자원 압박 보완.
4. **XP / Stat Check / 보상량** — 성장 플레이의 실제 시간가치 설정.
5. **Affinity 증가량 / NPC 이동·Character Gate** — 관계 자원의 기능 확정.
6. 위 수치를 넣고 Normal / True / Hidden Route를 다시 시뮬레이션.

---

## 현재 판정

- **시간 구조:** 세 Route 모두 성립.
- **W3 Emergency 여유:** 평균 3~4h 수준의 큰 대응 1회/일을 넣어도 성립.
- **True:** 가능하나 수리 반복량 재검토 필요.
- **Hidden:** 현재 Proxy로는 충분히 가능. 실제 HIGH 임계치가 난이도를 결정.
- **Will:** 현재는 약함.
- **XP/Affinity/ShipCondition:** 아직 미정이라 다음 수치 설계 필요.
