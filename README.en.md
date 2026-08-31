# SpatialViewer.IFCCore

[中文](README.md) | [日本語](README.ja.md)

SpatialViewer.IFCCore is the BIM/IFC viewing kernel for SpatialViewer. It owns IFC parsing, BIM hierarchy and properties, geometry conversion, render-scene preparation, and the integration boundary for models originating from Revit. It intentionally contains no WinUI 3 product UI.

## Scope

- Primary input: IFC STEP (`.ifc`), with IFCZIP / IFCXML planned.
- Target schemas: IFC2x3, IFC4 and IFC4.3.
- Preferred .NET adapter: xBIM, isolated behind `SpatialViewer.Formats.Ifc.Xbim`.
- `.rvt` is not parsed by the portable core. Revit-origin models are integrated through a Revit API exporter, Autodesk Platform Services, or an optional licensed SDK adapter.
- Output is a renderer-agnostic `SceneDocument` plus BIM semantics and mesh data.

See [DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md) and [ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Build

```powershell
dotnet restore SpatialViewer.IFCCore.sln
dotnet build SpatialViewer.IFCCore.sln -c Release
dotnet test SpatialViewer.IFCCore.sln -c Release
```

MIT licensed. Third-party notices are tracked separately.
