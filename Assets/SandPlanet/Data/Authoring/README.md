# SandPlanet Authoring Master

Prototype 0.4 expects the writer-facing master workbook at:

`Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx`

Current approved workbook: **v1.3 StateIDs**

SHA-256:
`7ad51336d636295f0cc8420fc8f3c88c9b48fc578a863311dcb0222c23b4853b`

Saving that XLSX while the Unity project is open triggers the 0.4 Excel → CSV exporter automatically on reimport. Manual export/validation is available under `Tools > SandPlanet > Data 0.4`.

The generated CSV files are committed under `Assets/SandPlanet/Data/Generated/CSV` so the current prototype content remains inspectable even before the Master workbook is present locally.
