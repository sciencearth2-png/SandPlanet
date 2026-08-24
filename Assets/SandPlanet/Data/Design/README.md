# SandPlanet Design Data

이 폴더는 SandPlanet의 **설계·밸런스·엔딩 역산 기준**을 보관합니다.

## Read first

Codex/agent가 이어서 작업할 때는 다음을 먼저 읽습니다.

1. `docs/CODEX_HANDOFF_2026-08-25.md`
2. `docs/CODEX_WORK_CHECKLIST.md`
3. 이 README
4. `ContentDesign.version.yml`
5. `SandPlanet_ContentDesign.xlsx`

## Source of Truth

- `SandPlanet_ContentDesign.xlsx`: 엔딩 역산, 수송선 시설 Integrity, Repair Budget, Action Budget, Route Simulation의 설계 원본.
- `../Authoring/SandPlanet_Master.xlsx`: Unity CSV Export에 사용하는 런타임 Authoring 원본.
- `../Generated/CSV/`: Master Export 결과. 직접 authoring하지 않음.
- C# Runtime/Validator: 실제 구현 규칙.

수치·루트·엔딩 계산을 하기 전에는 반드시 아래 순서로 대조합니다.

1. **ContentDesign** — 의도된 시스템/밸런스/역산 구조 확인
2. **Master** — 현재 실제 데이터 이식 상태 확인
3. **Generated CSV** — Export 결과 확인
4. **Runtime / Validator** — 코드가 실제로 지원하는 규칙 확인

세 자료가 충돌하면 계산을 진행하기 전에 먼저 동기화합니다. 과거 채팅/로컬의 버전 파일을 암묵적으로 섞어 쓰지 않습니다.

## Current Canon / Baseline

현재 설계 버전은 **ContentDesign v3.1 Canon + Ship Baseline Sync**입니다.

수송선 기존 역산 설계는 유지합니다.
- W1 Standard Repair: **5회**, D3~D7 하루 1회
- W2 Standard Repair: **7회**, D8~D14 하루 1회
- 총 Standard Repair: **12회**
- 5-Max Facility Standard Repair: +1 Integrity
- Hull Standard Repair: +4 Hull

최신 Canon:
- 지구 출항 → 불시착: 약 3개월, 이후 Jay만 약 10년 공백
- Jina: Jay의 친구가 아니라 항해 중 의지하던 연상의 실무자/큰언니 같은 인물
- Diya: 망가진 지구 출생의 염세적인 20대. 기성세대에 불만이 크며 아이들이 잘 지내는 것을 중요하게 여김
- Benjamin: 배우자/자녀 없음. 불시착 당시 약혼자 한 사람 사망
- Mira: 모래폭풍으로 발자국 소실 → 실종 → Jay가 수색해 발견. 모래지옥/희생 Branch 사용 안 함
- Oasis: 호수/연못이 아니라 지하수를 퍼 올리는 **급수 펌프 자체**. 내부 `Oasis` 변수명은 계산 호환을 위해 유지

## Current W1 repair migration status

- ContentDesign의 W1 D3~D7 Standard Repair 5-slot을 Master로 이식함.
- Research Restoration D3~D5는 같은 날짜의 Standard Repair 슬롯을 공유함.
- v3.6 Unity 테스트에서 일반 repair에 잘못된 `STA_W1_SHIP_OVERVIEW_SEEN` 선행조건이 있어 Research만 보이는 문제가 확인됨.
- v3.7에서 이 gate를 제거했고 사용자가 Unity에서 노출 문제를 확인함.
- **작업 시작 시 원격 Master/Generated CSV가 v3.7과 동일하게 commit된 상태인지 반드시 확인.** 로컬 modified 파일이 있으면 덮어쓰지 않음.

## Next migration

ContentDesign에는 W2 D8~D14 Standard Repair 7-slot이 이미 설계되어 있으나 Master Runtime migration은 아직 다음 작업입니다.

새 시스템을 기획하지 말고 기존 `06_ActionBudget`, `13_FacilityTransition`, `19_W2ReverseDesign`, `21_IntegratedRouteSim`을 기준으로 이식합니다.

## Binary workbook registration

Git 기준 경로는 `Assets/SandPlanet/Data/Design/SandPlanet_ContentDesign.xlsx`로 고정합니다. 버전 번호는 파일명 대신 workbook 내부 `31_CanonSync_v3.1` 및 `ContentDesign.version.yml`에서 관리합니다.
