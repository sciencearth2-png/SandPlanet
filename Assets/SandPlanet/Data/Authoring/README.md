# SandPlanet Authoring Master

Prototype 0.4 now reads the **v1.5 integrated-flow workbook** from:

`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Current development workbook structure: **v1.5 Integrated Flows**

Key authoring change:
- `06_상호작용 플로우` combines the old interaction entry / time-cost choice / post-choice presentation sheets.
- `08_이벤트 플로우` treats Events as world-driven narrative flows, including choices and system results.
- `09_이벤트 트리거` remains responsible only for when an Event starts.

Saving/replacing the XLSX while Unity is open triggers Excel → CSV export automatically on reimport.
Manual export/validation is available under:

`Tools > SandPlanet > Data v1.5`

The familiar `Tools > SandPlanet > Data 0.4` menu is kept as a compatibility alias.

The exporter writes the v1.5 flow data into the existing `Interactions.csv` and `Events.csv` asset paths so an already-generated Prototype 0.4 scene can keep its serialized TextAsset references. Legacy `Choices.csv` and `ChoiceBeats.csv` remain as harmless placeholders and are no longer read by runtime content logic.
