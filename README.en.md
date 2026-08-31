# SpatialViewer.IFCCore

[中文](README.md) | [日本語](README.ja.md)

SpatialViewer.IFCCore is the BIM/IFC viewing kernel for SpatialViewer. It owns IFC parsing, BIM hierarchy and properties, geometry conversion, render-scene preparation, and the integration boundary for models originating from Revit. It intentionally contains no WinUI 3 product UI.

## Scope

- Primary inputs: IFC STEP (`.ifc`) and IFCZIP (`.ifczip`); IFCXML is planned.
- Supported schemas in 0.2.x: IFC2x3, IFC4 and IFC4.3.
- IFC adapter: `Xbim.Essentials`, isolated behind `SpatialViewer.Formats.Ifc.Xbim`.
- `.rvt` is not parsed by the portable core. Revit-origin models are integrated through a Revit API exporter, Autodesk Platform Services, or an optional licensed SDK adapter.
- Output is a renderer-agnostic `SceneDocument` with BIM semantics and later geometry data.

## 0.2.0 capabilities

- Real IFC STEP / IFCZIP opening and schema detection.
- Project → Site → Building → Storey → Element hierarchy.
- GlobalId, entity identity, occurrence/type and containment metadata.
- Property sets, quantities, classifications and basic materials.
- Cancellation, staged progress, structured diagnostics and elapsed timing.
- Geometry tessellation is intentionally deferred to 0.3.x.

See [DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md) and [ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Build

```powershell
dotnet restore SpatialViewer.IFCCore.sln
dotnet build SpatialViewer.IFCCore.sln -c Release
dotnet test SpatialViewer.IFCCore.sln -c Release
```

MIT licensed. Third-party notices are tracked separately.
