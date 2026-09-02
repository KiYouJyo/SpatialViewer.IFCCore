# SpatialViewer.IFCCore

[中文](README.md) | [日本語](README.ja.md)

SpatialViewer.IFCCore is the BIM/IFC viewing kernel for SpatialViewer. It owns IFC parsing, BIM hierarchy and properties, real geometry tessellation, renderer-neutral interaction semantics, caching/performance measurement, and the integration boundary for models originating from Revit. It intentionally contains no WinUI 3 product UI.

## Scope

- Primary inputs: IFC STEP (`.ifc`) and IFCZIP (`.ifczip`); IFCXML is planned.
- Schemas: IFC2x3, IFC4 and IFC4.3.
- Semantic adapter: `Xbim.Essentials 6.1.605`.
- Geometry adapter: `Xbim.Geometry 6.3.891-netcore` with its OpenCascade geometry runtime.
- All xBIM/OpenCascade types remain isolated inside `SpatialViewer.Formats.Ifc.Xbim` and do not leak into Core or UI contracts.
- `.rvt` is not reverse-engineered or parsed by the portable core. Revit-origin models enter through IFC, a Revit API exporter/sidecar, Autodesk Platform Services, or an optional licensed SDK adapter.

## 0.5.0 capabilities

- Retains the complete 0.2–0.4 semantic, real OpenCascade geometry and BIM interaction contracts.
- `XbimIfcModelReader` continues to load on a background task with cancellation and staged progress; 0.5 adds cache-check/read/write stages.
- `CachedIfcModelReader` adds reusable caching around any `IIfcModelReader` without changing the xBIM adapter.
- Bounded in-memory LRU entries reuse the exact cached `SceneDocument` and shared `MeshData` references.
- Optional versioned `.svbim` disk cache stores the renderer-neutral `SceneDocument`, never xBIM/OpenCascade private objects.
- Cache identity binds source SHA-256, file length, open-option signature and cache-format version. Source or geometry/property/opening/rebase option changes invalidate reuse.
- `.svbim` stores a unique mesh table, preserving shared geometry, material slots, transforms, bounds, world origin, BIM properties and diagnostics across reader/process instances.
- Corrupt/unreadable/unwritable cache state never prevents a cold IFC load; disk writes use a temporary file followed by atomic replacement.
- `RenderSceneIndex` pre-indexes a loaded scene once so hide/isolate, opacity and section-box rebuilds no longer recurse the `SceneDocument`.
- `IfcLoadBenchmark` measures elapsed time plus sampled managed-heap and process-working-set start/peak/end values and reports Miss / MemoryHit / DiskHit disposition.
- `RenderPerformanceMetrics` measures indexed scene-rebuild elapsed time/allocations and estimates GPU geometry upload by unique mesh reference rather than multiplying geometry by instance count.
- CI caches NuGet/OpenCascade packages using a dependency-graph key to reduce repeated native-geometry restore cost.

## Cache example

```csharp
IIfcModelReader reader = new CachedIfcModelReader(
    new XbimIfcModelReader(),
    new IfcModelCacheOptions
    {
        MemoryEntryLimit = 4,
        DiskCacheDirectory = cacheDirectory,
    });

var result = await reader.OpenAsync(
    path,
    new IfcOpenOptions { IncludeGeometry = true },
    cancellationToken);
```

`.svbim` is an internal, versioned SpatialViewer performance cache. It is not an interchange replacement for IFC and should not be treated as a long-term archive format.

See [DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md), [COMPATIBILITY.md](docs/COMPATIBILITY.md) and [ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Build

```powershell
dotnet restore SpatialViewer.IFCCore.sln
dotnet build SpatialViewer.IFCCore.sln -c Release
dotnet test SpatialViewer.IFCCore.sln -c Release
```

MIT licensed. Third-party notices are tracked separately in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Version 0.5.0 adds no runtime third-party dependency.
