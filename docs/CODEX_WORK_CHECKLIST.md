# SandPlanet Codex Work Checklist

이 문서는 `docs/CODEX_HANDOFF_2026-08-25.md`의 실행용 요약이다. 세부 판단은 handoff 문서를 우선한다.

## Session start

- [ ] branch = `prototype-foundation`
- [ ] `git status` 확인
- [ ] local modified `SandPlanet_Master.xlsx` / Generated CSV / scene / code 보존
- [ ] `AGENTS.md` 읽기
- [ ] `docs/CODEX_HANDOFF_2026-08-25.md` 읽기
- [ ] `Assets/SandPlanet/Data/Design/README.md` 읽기
- [ ] `Assets/SandPlanet/Data/Design/ContentDesign.version.yml` 읽기
- [ ] `SandPlanet_ContentDesign.xlsx` 존재 확인
- [ ] 최신 Master가 v3.7 W1 Repair Visibility 상태인지 확인

## Before any balance / ending / route calculation

- [ ] ContentDesign 확인
- [ ] Master 실제 이식 여부 확인
- [ ] Generated CSV 확인
- [ ] Runtime/Validator 지원 여부 확인
- [ ] 충돌하면 계산을 멈추고 먼저 동기화

## Canon guard

- [ ] Launch → crash ≈ 3 months
- [ ] Jay post-crash gap ≈ 10 years
- [ ] Jina ≠ old friend; reliable older practical worker → peer
- [ ] Diya = cynical 20-something from ruined Earth + values younger children
- [ ] Benjamin = no wife/child; one dead fiancee
- [ ] Mira = footprints erased by sandstorm → missing → Jay searches/finds
- [ ] Oasis = groundwater pump, not lake/pond
- [ ] D6 conclusion is limited to current groundwater + intake method, not whole planet impossibility

## W1 ship repair regression

Expected baseline:

- [ ] D3~D7, one Standard Repair per day
- [ ] D3 console shows both `수송선 시설을 한 곳 복구한다` and `연구실의 죽은 화면을 살펴본다`
- [ ] general repair: 2h
- [ ] general 5-Max facility: +1 Integrity
- [ ] Hull: +4, max20
- [ ] Research Restoration D3~D5 consumes same daily repair slot
- [ ] second repair in same day is blocked
- [ ] next day repair is available again
- [ ] no `STA_W1_SHIP_OVERVIEW_SEEN` prerequisite on general repair flow

## Immediate work order

### P0
- [ ] Verify whether local/remote Master + Generated CSV already contain v3.7 hotfix
- [ ] If local changes exist, do not reset; preserve and report

### P1
- [ ] Export Master → CSV
- [ ] Validate Generated CSV
- [ ] Unity D3 repair regression
- [ ] Commit Master + Generated CSV only when validated

### P2
- [ ] Migrate ContentDesign W2 D8~D14 7 Standard Repair slots into Master
- [ ] Do not redesign repair system
- [ ] Check evacuation / preserve / character time-budget competition
- [ ] Check FacilityTransition → D15 start values
- [ ] Export / Validate / Unity test D8~D14

### P3
- [ ] W2/W3 Canon text scan
- [ ] Compare Master against `28_W2_NarrativeAuthoring`, `29_W3_NarrativeAuthoring`, `30_EndingNarrative`
- [ ] Fix DATA first; avoid hardcoded runtime branches

### P4
- [ ] D1→D21 integrated playtest
- [ ] Quest progression
- [ ] time budget
- [ ] repair budget
- [ ] character route mastery
- [ ] facility integrity
- [ ] W2 preserve/evacuation
- [ ] W3 stance / endings
- [ ] narrative UX / log / hover / choices

## Data workflow

- [ ] edit Master, not Generated CSV
- [ ] `Tools > SandPlanet > Data v1.6 > Export Master Excel to CSV`
- [ ] `Validate Generated CSV`
- [ ] Prototype04 scene test
- [ ] `git status`
- [ ] commit only intended files

## Never do silently

- [ ] Do not run `Generate Prototype 0.4` during normal data/UI work
- [ ] Do not reset/checkout modified binary xlsx without user confirmation
- [ ] Do not revive old canon from older docs
- [ ] Do not invent a new ship repair system
- [ ] Do not hardcode character/quest-specific behavior into core services when DATA can express it
- [ ] Do not put historical 1.5h values directly into integer-hour Runtime

## Completion report

Always report:

- changed files / authority
- whether ContentDesign intent/numbers changed
- whether new state/owner was introduced
- validation/test performed
- remaining TEMP/TBD
- whether user must run Unity locally
