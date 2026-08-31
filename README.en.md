# SpatialViewer.IFCCore

[中文](README.md) | [日本語](README.ja.md)

SpatialViewer.IFCCore is the BIM/IFC viewing kernel for SpatialViewer. It owns IFC parsing, BIM hierarchy and properties, real geometry tessellation, render-scene preparation, and the integration boundary for models originating from Revit. It intentionally contains no WinUI 3 product UI.

## Scope

- Primary inputs: IFC STEP (`.ifc`) and IFCZIP (`.ifczip`); IFCXML is planned.
- Schemas: IFC2x3, IFC4 and IFC4.3.
- Semantic adapter: `Xbim.Essentials 6.1.605`.
- Geometry adapter: `Xbim.Geometry 6.3.891-netcore` with its OpenCascade geometry runtime.
- All xBIM/OpenCascade types remain isolated inside `SpatialViewer.Formats.Ifc.Xbim` and do not leak into Core or UI contracts.
- `.rvt` is not reverse-engineered or parsed by the portable core. Revit-origin models enter through IFC, a Revit API exporter/sidecar, Autodesk Platform Services, or an optional licensed SDK adapter.

## 0.3.0 capabilities

- Real IFC STEP / IFCZIP opening and IFC2x3 / IFC4 / IFC4.3 schema detection.
- Project → Site → Building → Storey → Element hierarchy and common BIM metadata.
- Real xBIM/OpenCascade triangle geometry when `IncludeGeometry=true`.
- Positions, normals, triangle indices and renderer-neutral style/material slots.
- Metre normalization, original world bounds and automatic local-origin rebasing for large coordinates.
- Shared `MeshData` for repeated/mapped geometry with independent instance transforms.
- Mirrored/negative transform semantics through `FlipWinding`.
- Opening/void boolean results for host geometry, with optional opening-element geometry.
- Cancellation, staged progress, structured diagnostics and elapsed timing.

See [DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md), [COMPATIBILITY.md](docs/COMPATIBILITY.md) and [ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Build

```powershell
dotnet restore SpatialViewer.IFCCore.sln
dotnet build SpatialViewer.IFCCore.sln -c Release
dotnet test SpatialViewer.IFCCore.sln -c Release
```

MIT licensed. Third-party notices are tracked separately in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
