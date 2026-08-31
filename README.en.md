# SpatialViewer.IFCCore

[中文](README.md) | [日本語](README.ja.md)

SpatialViewer.IFCCore is the BIM/IFC viewing kernel for SpatialViewer. It owns IFC parsing, BIM hierarchy and properties, real geometry tessellation, renderer-neutral interaction semantics, and the integration boundary for models originating from Revit. It intentionally contains no WinUI 3 product UI.

## Scope

- Primary inputs: IFC STEP (`.ifc`) and IFCZIP (`.ifczip`); IFCXML is planned.
- Schemas: IFC2x3, IFC4 and IFC4.3.
- Semantic adapter: `Xbim.Essentials 6.1.605`.
- Geometry adapter: `Xbim.Geometry 6.3.891-netcore` with its OpenCascade geometry runtime.
- All xBIM/OpenCascade types remain isolated inside `SpatialViewer.Formats.Ifc.Xbim` and do not leak into Core or UI contracts.
- `.rvt` is not reverse-engineered or parsed by the portable core. Revit-origin models enter through IFC, a Revit API exporter/sidecar, Autodesk Platform Services, or an optional licensed SDK adapter.

## 0.4.0 capabilities

- Retains the 0.2/0.3 IFC semantic, property and real triangle-geometry pipeline.
- Builds `RenderScene` from an already loaded `SceneDocument` without reparsing IFC.
- Stable object identity plus deterministic `uint PickId`; visibility-state changes do not renumber unaffected objects.
- PickMap exposes object name, category, storey and `SceneProperty` snapshots for property selection.
- Object/category/storey hide and object isolate filters.
- Global, category and object opacity overrides plus renderer-neutral fallback material keys.
- Section-box contract with coarse bounds culling while intersecting meshes remain available for precise backend/GPU clipping.
- Per-object outline targets for object-ID/depth outlines and selection highlighting.
- Instanced `RenderBatch` generation by shared mesh/material/opacity/winding state.
- Platform-neutral `RenderCamera` with perspective/orthographic projection, orbit, pan, zoom and view/projection matrices.

See [DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md), [COMPATIBILITY.md](docs/COMPATIBILITY.md) and [ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Build

```powershell
dotnet restore SpatialViewer.IFCCore.sln
dotnet build SpatialViewer.IFCCore.sln -c Release
dotnet test SpatialViewer.IFCCore.sln -c Release
```

MIT licensed. Third-party notices are tracked separately in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
