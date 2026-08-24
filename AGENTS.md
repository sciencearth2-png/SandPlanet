# SandPlanet Agent Instructions

## CRITICAL — current handoff first

2026-08-25 이후 작업은 **반드시** `docs/CODEX_HANDOFF_2026-08-25.md`를 먼저 읽는다.

이 파일은 과거 docs보다 최신이다. 과거 문서와 충돌하면 다음 우선순위를 따른다.

1. `docs/CODEX_HANDOFF_2026-08-25.md`
2. `Assets/SandPlanet/Data/Design/SandPlanet_ContentDesign.xlsx`의 최신 Canon/역산 설계 (`31_CanonSync_v3.1` 포함)
3. `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`
4. Generated CSV / Runtime / Validator의 실제 구현 계약
5. `Assets/SandPlanet/Data/Design/README.md`
6. 기존 `docs/*.md`

특히 과거 문서에 남은 아래 설정은 폐기된 역사적 초안이다.

- Jina = Jay의 동년배 친구/절친 → **폐기**
- Benjamin = 아내/아이 있음 → **폐기**
- Mira = 모래지옥/싱크홀에 빨려 들어감 → **폐기**
- Oasis = 호수/연못/물가 → **폐기**

작업 시작 시 `git status`를 먼저 본다. 로컬의 binary xlsx / Generated CSV 변경을 확인 없이 reset/checkout하지 않는다.

---

## Active target
- 현재 유일한 개발 대상은 **Prototype 0.4**다.
- Unity **6000.4.1f1 / URP**.
- Scene: `Assets/Scenes/SandPlanet_Prototype_04.unity`.
- 과거 Prototype 0.1~0.3 코드/씬/Generated 자산은 의도적으로 제거되었다. 복구하거나 다시 참조하지 않는다.
- Working branch: `prototype-foundation`.

## Read first
작업 전 아래 순서로 읽는다.
1. `docs/CODEX_HANDOFF_2026-08-25.md`
2. `Assets/SandPlanet/Data/Design/README.md`
3. `Assets/SandPlanet/Data/Design/ContentDesign.version.yml`
4. `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
5. `docs/NARRATIVE_REVERSE_DESIGN_v0_1.md`
6. `docs/NARRATIVE_ARCS_AND_INTEGRATION_v0_2.md`
7. `docs/PROTOTYPE_0_4_RUN.md`
8. `docs/DESIGN_STATE.md`
9. `docs/PROTOTYPE_SCOPE.md`
10. `docs/UNITY_ARCHITECTURE.md`

## Source of truth
### 설계/밸런스/엔딩 역산
- `Assets/SandPlanet/Data/Design/SandPlanet_ContentDesign.xlsx`
- 현재 기준: **ContentDesign v3.1 Canon + Ship Baseline Sync**.
- 수치/루트/엔딩 계산 전에는 반드시 이 파일을 먼저 확인한다.

### Runtime authoring
- `Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`
- Generated CSV: `Assets/SandPlanet/Data/Generated/CSV/`
- Generated CSV를 직접 Source처럼 수정하지 않는다. Master 수정 후 Export한다.

### 최신 인수인계
- `docs/CODEX_HANDOFF_2026-08-25.md`

### 과거 세부 문서
- Week 1 서사/시스템 과거 합의: `docs/W1_NARRATIVE_SYSTEM_v1_7.md`
- 엔딩 역산 기반 Week 3 시설/생존 과거 상세: `docs/NARRATIVE_REVERSE_DESIGN_v0_1.md`
- 캐릭터 아크 과거 통합 문서: `docs/NARRATIVE_ARCS_AND_INTEGRATION_v0_2.md`
- 현재 런타임/데이터 계약 참고: `docs/PROTOTYPE_0_4_RUN.md`
- 현재 기획 상태 참고: `docs/DESIGN_STATE.md`
- Prototype 범위 참고: `docs/PROTOTYPE_SCOPE.md`
- 코드 책임 경계: `docs/UNITY_ARCHITECTURE.md`

과거 서사 문서와 최신 handoff/ContentDesign이 충돌하면 최신 handoff/ContentDesign을 우선한다. 임의로 과거 설정으로 회귀하지 않는다.

문서와 실제 코드가 충돌하면 임의로 새 패치 코드를 추가하지 말고 먼저 충돌 지점을 설명한다.

## Required verification order
수치·루트·시스템 판단은 아래 순서를 고정한다.

1. **ContentDesign** — 의도된 시스템/밸런스/엔딩 역산 구조
2. **Master** — 실제 authoring 이식 상태
3. **Generated CSV** — Export 결과
4. **Runtime / Validator** — 실제 코드 지원 여부

ContentDesign에 있다고 Runtime에 있다고 가정하지 않는다. 반대로 Master에 없다고 새 시스템으로 재기획하기 전에 ContentDesign에서 기존 설계를 찾는다.

## Work classification before implementation
모든 요청은 구현 전에 아래 중 하나로 분류한다.
- **DATA/CONTENT**: 대사, 수치, 조건, Quest/Event/Interaction/State/Schedule 등 authoring 데이터로 해결 가능.
- **PRESENTATION**: 기존 UI/표현 authority 내부 변경.
- **GAMEPLAY RULE**: 기존 gameplay service의 규칙 변경.
- **NEW SYSTEM**: 기존 authority로 표현할 수 없는 신규 상태/규칙/흐름.

가능하면 가장 작은 기존 authority만 수정한다. DATA로 가능한 요청 때문에 새 MonoBehaviour나 하드코딩 분기를 만들지 않는다.

## Architecture ownership guardrails
### Gameplay/runtime
- `Prototype04GameState`: mutable runtime state의 유일한 저장소.
- `Prototype04ConditionEvaluator`: 조건 판정의 유일한 구현.
- `Prototype04QuestService`: Quest 상태/Step mutation 및 event progression authority.
- `Prototype04ScheduleService`: NPC 위치/schedule 판정 authority.
- `Prototype04InteractionService`: Interaction availability authority.
- `Prototype04EventService`: Event/Trigger/queue/STATE_CHANGE authority.
- `Prototype04FlowRuntime`: Interaction/Event flow, staged effects, commit authority.
- `SandPlanetPrototype04Controller`: 위 서비스를 Unity scene/presentation에 연결하는 coordinator. 새로운 gameplay 규칙의 만능 저장소로 사용하지 않는다.

### Presentation/navigation
- `Prototype04CompositionRoot`: runtime presentation dependency 조립.
- `Prototype04NavigationController`: UI mode와 Back/ESC의 유일한 authority.
- `Prototype04LocationPresenter`: Location viewport, target placement, clipping, pan/zoom의 유일한 authority.
- `Prototype04NarrativeLogPresenter`: 현재 Interaction/Narrative panel, transcript, portrait, choice presentation authority.
- `Prototype04HudPresenter`: HUD/context/progress presentation authority.
- Quest tracker와 People panel은 gameplay state를 읽어 표현만 하며 상태 mutation을 소유하지 않는다.

### One-owner rule
- 같은 RectTransform, visibility, selected target, UI mode, flow state, Quest state에 복수 final writer를 두지 않는다.
- 기존 authority가 있으면 그 클래스/API를 확장한다. 새 `Polish`, `Finalizer`, `Override`, `Guard`, execution-order patch를 추가하지 않는다.
- Reflection으로 다른 component private state를 읽거나 UI text를 파싱해 identity/state를 역추론하지 않는다.
- `Update/LateUpdate/Canvas.willRenderCanvases/DefaultExecutionOrder` 경쟁으로 최종 상태를 결정하지 않는다.

## New-system rule
신규 시스템이 정말 필요하면 구현 전에 최소한 다음을 명시한다.
1. 소유 state가 무엇인지
2. 그 state의 단일 owner가 누구인지
3. 입력과 출력/event가 무엇인지
4. 기존 service/presenter 중 어디와 연결되는지
5. workbook/CSV 계약 변경 여부
6. 회귀 테스트 항목

이 항목이 정의되지 않은 상태에서 새로운 manager/helper/patch component를 추가하지 않는다.

## Scope control
- 요청받지 않은 시스템/NPC/퀘스트/UI를 추가하지 않는다.
- 확정과 TEMP/TBD를 구분한다.
- 구현 때문에 기획을 바꿔야 하면 먼저 영향과 선택지를 제시한다.
- 특정 캐릭터/Quest 전용 로직을 core manager/service에 하드코딩하지 않는다. 가능한 경우 authoring data로 표현한다.

## Current canon summary
- 지구 출항 → 불시착: 약 **3개월**.
- Jay: 불시착 이후 약 **10년** 공백.
- Jina: 친구가 아니라 항해 중 의지하던 연상의 실무자/큰언니 같은 인물. W3에서는 함께 조건을 계산하는 동료.
- Diya: 망가진 지구 출생의 염세적인 20대. 기성세대에 불만이 크며 어린 아이들이 잘 지내는 것을 중요하게 여김.
- Benjamin: 배우자/자녀 없음. 약혼자 한 사람이 불시착 당시 사망.
- Mira: 모래폭풍이 발자국을 지워 실종 → Jay가 수색해 발견. 모래지옥/희생 branch 없음.
- Oasis: 호수가 아니라 지하수를 퍼 올리는 **급수 펌프 자체**. 취수정/배관/계측부 포함.
- D6 결론: `현재 지하수와 오아시스 펌프의 취수 방식만으로 장기 거주를 보장할 수 없다.`

세부는 `docs/CODEX_HANDOFF_2026-08-25.md` 우선.

## Narrative/data rules
- 동기 / 공포 / 정체성은 캐릭터 행동의 직접 원리다.
- Big5는 표현 참고값이며 기계적 결정식이 아니다.
- 플레이어가 직접 선택하는 행동은 Interaction Flow가 담당한다.
- 자동/강제 스토리는 Event Flow가 담당한다.
- Quest는 Main / Character / Side 진행 추적과 UI 역할을 한다.
- 현재 runtime은 explicit `QuestAction`과 기존 `ProgressEventID/OnProgressEvent`를 모두 지원한다. 둘 중 하나를 제거/통합하려면 authoring migration을 별도 승인받는다.
- Quest 상태를 임의의 Day/State 하드코딩으로 직접 변경하지 않는다.
- Character Stance는 단순 Affinity threshold로 자동 변경하지 않는다. 의미 있는 Quest/Event 결과가 변화를 만든다.
- Facility Integrity 자체가 캐릭터 감정을 직접 바꾸지 않는다. `시설 상태 → Character/Event 가능 → Event 결과 → Stance 변화` 순서를 우선한다.

### Narrative UX
- Narration과 Dialogue 분리. 행동 지문을 Dialogue에 넣지 않는다.
- Beat: 정보 → 플레이어 입력 → 반응 → 다음 정보.
- 단순 질문/관찰/생각은 0시간 가능.
- 실제 시간 소모 행동만 TimeCost를 준다.
- Hover는 즉시 기계적 변화만 공개한다.
- Narrative body + choices는 하나의 세로 ScrollRect 안에서 유지.
- Typewriter는 새 Beat만 적용하고 이전 transcript는 남긴다.

## Current core rules
- 21일 / 3주.
- Week 1: D1~2 재회·상황 파악 → D3~6 오아시스 펌프/지하수 조사·보고 → D7 공개 범위 선택.
- Week 2: 폭풍 발견/대비, 수송선 피난 준비, 정착지/오아시스 펌프/묘지 보존, Character Arc 시험.
- Week 3: 수송선 내부 4구역, 시설/사람 배치 기반 폭풍 생존, 미래 결정, D21 결말.
- 기본 행동 가능 시간 08:00~22:00.
- 하루 종료 후 22:00→08:00 Overnight Phase에서 세계시간은 계속 흐르며 야간조가 최소 운영/감시를 담당한다.
- 시간대: Morning 06:00~11:59 / Afternoon 12:00~16:59 / Evening 17:00~22:00.
- 최대 의지 기본 5, 수면 +2, 휴식 3시간 → +1.
- 개인 / 대인 / 기술: Lv + XP, 시작 Lv 합계 6, 6XP마다 즉시 Lv +1.
- 호감도 0~5, 설득/미래 Stance와 별개.
- Soft Requirement는 의지로 보완 가능, Hard Requirement는 불가.

## Ship repair baseline — preserve, do not redesign
ContentDesign의 기존 엔딩 역산 설계를 보존한다.

- W1 Standard Repair: **5회 / D3~D7 하루 1회**.
- W2 Standard Repair: **7회 / D8~D14 하루 1회**.
- 총 12회.
- Hull `5/20`, Bridge/Engine/Oxygen/Research/Living `2/5`, Freezer/Greenhouse `5/5`.
- 5-Max 일반 시설 repair `+1`.
- Hull repair `+4`.
- 일반 수리 2h, Tech Lv3 TEMP.
- Research Restoration은 D3~D5의 W1 Standard Repair 슬롯을 공유한다.

### W1 latest hotfix
사용자 Unity 테스트에서 v3.6 일반 repair가 D1 Ship Overview를 요구해 Research만 보이는 문제가 발견됨.
최신 v3.7에서는 `FLOW_W1_SHIP_REPAIR_D3~D7`의 잘못된 `STA_W1_SHIP_OVERVIEW_SEEN` gate를 제거했다.
D3 정비 콘솔에는 Research entry와 일반 Repair entry가 동시에 보여야 한다.

**Codex는 작업 시작 시 원격 Master/CSV가 이 v3.7과 동일한지 확인한다.** 로컬 modified 파일이 있으면 보존한다.

## Validator contract
`Assets/SandPlanet/Editor/SandPlanetSpreadsheetExporter04.cs`

- inactive row는 runtime validation에서 제외.
- `ChoiceID`가 있어도 1h 최소 비용을 강제하지 않는다.
- `ACTIVATE_QUEST`도 1h 강제 없음.
- 음수 TimeCost만 오류.
- active `SET_STEP`은 `NextStepID` 필수.
- NextStep은 존재하고 같은 Quest 소속이어야 함.

과거 Validator 규칙으로 되돌리지 않는다.

## Current UI contract
- Location viewport는 불투명하고 Quest/Log보다 위에 표시.
- 장소명은 좌측 상단.
- 캐릭터/사물은 viewport 안에 자연 배치하고 invisible clipping boundary를 사용.
- 기본 map browse 배율 1.5x, target focus 최신 승인값 2.35x.
- Interaction 선택 → dialogue 전환에서 장소 배치가 재정렬되거나 급변하면 안 된다.
- 우측 panel은 Interaction 선택과 Narrative를 담당한다.
- ESC는 가장 위 UI 계층부터 한 단계씩 닫는다.
- 캐릭터 초상은 Interaction 선택 및 해당 캐릭터 대화에서 표시한다.
- Interaction 버튼의 주 문구는 자연스러운 행동 표현이며 Quest 정보는 보조 정보다.

## Data workflow
일반 authoring 수정:
1. ContentDesign 확인
2. Master 수정
3. `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
4. `Validate Generated CSV`
5. 기존 Prototype04 scene에서 테스트
6. `git status`로 의도한 파일만 변경됐는지 확인

`Tools > SandPlanet > Generate Prototype 0.4`는 **일반 기획/데이터/UI 작업에서 실행하지 않는다.** Scene 구조 자체를 재생성해야 하는 특별한 경우에도 먼저 사용자에게 영향 범위를 설명하고 승인받는다.

## Immediate next work
세부는 handoff 문서의 우선순위를 따른다.

1. 로컬/원격 v3.7 Master + Generated CSV 기준점 확인 및 필요 시 commit 정리.
2. ContentDesign에 이미 존재하는 **W2 D8~D14 Standard Repair 7-slot**을 Master로 이식. 새로 기획하지 않는다.
3. W2/W3 최신 Canon/대사 정합성 QA.
4. D1→D21 통합 플레이테스트.

## Completion
작업 완료 시 다음을 짧게 보고한다.
- 변경한 데이터/authority
- ContentDesign의 설계/수치를 변경했는지 여부
- 새 owner 또는 새 state를 만들었는지 여부
- 테스트 방법과 결과
- 남은 TEMP/TBD
- 로컬 Unity 검증 필요 여부
