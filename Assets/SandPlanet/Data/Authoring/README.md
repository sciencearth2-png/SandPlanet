# SandPlanet Authoring Master

Prototype 0.4 now reads the **v1.6 integrated-flow workbook** from:

`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Current development workbook structure: **v1.6 Quest Roles**

## Core authoring model

- `06_상호작용 플로우` combines interaction entry, narrative nodes, player choices, costs/results, and explicit quest changes.
- `08_이벤트 플로우` treats Events as world-driven narrative flows, including choices, affinity/State results, and explicit quest changes.
- `09_이벤트 트리거` is responsible only for **when an Event starts**. A Trigger by itself never mutates Quest state.

Quest status changes have two legal authoring paths:

1. an Interaction node result (`QuestAction`), or
2. an Event node result (`QuestAction`).

Day/time/State conditions can expose content or trigger an Event, but they do not silently accept or progress a Quest.

### QuestRole

Interaction flows use `QuestRole`:

- `NONE` — ordinary interaction / `[일상]`
- `OFFER` — shown while the linked Quest is `LOCKED`; target UI shows `?`
- `PROGRESS` — shown while the linked Quest is `ACTIVE` and the current `QuestStepID` matches; target UI shows `!`

Quest type UI colors:

- MAIN — orange
- CHARACTER — green
- SIDE — light yellow

Entry buttons are derived automatically from the data, for example `[MAIN !]`, `[CHAR ?]`, `[SIDE !]`. Do not type those labels into Excel manually.

### Character Quest policy

`CHARACTER` is reserved for a quasi-main-story character arc. There is one long-running Character Quest per major character, with multiple steps across the 21-day structure. Smaller one-off character activities should use `SIDE` instead.

Current Character Quest IDs:

- `QST_CHAR_BENJAMIN`
- `QST_CHAR_SAM`
- `QST_CHAR_FAYE`
- `QST_CHAR_JINA`
- `QST_CHAR_DIYA`
- `QST_CHAR_BORICHI`

## Excel → CSV

Saving/replacing the XLSX while Unity is open triggers Excel → CSV export automatically on reimport.
Manual export/validation is available under:

`Tools > SandPlanet > Data v1.6`

The familiar `Tools > SandPlanet > Data 0.4` menu is kept as a compatibility alias.

The exporter writes v1.6 flow data into the existing `Interactions.csv` and `Events.csv` asset paths so an already-generated Prototype 0.4 scene can keep its serialized TextAsset references. Legacy `Choices.csv` and `ChoiceBeats.csv` remain as harmless placeholders and are no longer read by runtime content logic.
