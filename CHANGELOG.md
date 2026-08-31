# Changelog

All notable changes to SpatialViewer.IFCCore are documented here.

## [Unreleased]

### Planned
- Performance/cache work for 0.5.x, including warm-view rebuild benchmarks and geometry/material caching beyond one load operation.
- Expand the redistributable golden corpus with representative Revit-origin IFC exports and malformed-input cases.
- Continue cross-exporter fidelity hardening for complex BReps, textures, linked/federated models and discipline-specific content.

## [0.4.0] - 2026-08-31

### Added
- Renderer-neutral BIM interaction semantics built from an already loaded `SceneDocument`.
- Stable ObjectId and deterministic `uint PickId` generation for hit testing.
- PickMap entries containing source identity, name, category, storey and `SceneProperty` snapshots.
- Object/category/storey hide filters and object isolate state.
- Global/category/object opacity overrides and semantic-category material fallback keys.
- Section Box contract with bounds-based coarse culling and retained backend/GPU clipping state.
- Per-object outline targets for object-ID/depth outlines and selection highlighting.
- Instanced `RenderBatch` output grouped by shared mesh/material/opacity/winding state.
- Platform-neutral perspective/orthographic `RenderCamera` with orbit, pan, zoom and view/projection matrices.
- Rendering regression tests covering picking stability, property lookup, filters, transparency, fallbacks, section boxes, outlines, batching and camera behavior.

### Changed
- Repository version advanced to 0.4.0.
- Render view state is downstream of Core scene semantics and no longer requires callers to mutate BIM data for interactive visibility/appearance changes.

## [0.3.0] - 2026-08-31

### Added
- Integrated `Xbim.Geometry` 6.3.891-netcore behind `SpatialViewer.Formats.Ifc.Xbim` for real xBIM/OpenCascade geometry generation.
- IFC solid-to-triangle conversion with positions, normals, triangle indices and renderer-neutral `MeshData`.
- Per-instance transforms, local shape displacement handling and shared-mesh reuse for repeated geometry.
- Length normalization to metres plus local-scene rebasing for large world coordinates while preserving original world bounds.
- Geometry bounds at mesh, instance and document levels.
- Surface-style labels preserved as renderer-neutral material slots.
- Mirrored / negative transforms preserved through `FlipWinding` so render backends can correct face winding.
- Opening/void boolean results are used for host geometry by default, with optional opening-element geometry through `PreserveOpeningElements`.
- Structured geometry progress stages and non-fatal per-shape diagnostics.
- Generated IFC4 golden fixtures covering tessellation, repeated geometry, mapped mirror transforms, surface styles, openings and large coordinates.

### Changed
- `IncludeGeometry=true` now performs real geometry extraction instead of returning the 0.2.x deferred-geometry diagnostic.
- Core and render-scene contracts now carry bounds and winding information without exposing xBIM types.
- Repository version advanced to 0.3.0.

## [0.2.0] - 2026-08-31

### Added
- Integrated `Xbim.Essentials` 6.1.605 behind `SpatialViewer.Formats.Ifc.Xbim`.
- Real IFC STEP and IFCZIP loading with IFC2x3 / IFC4 / IFC4.3 schema detection.
- Project → Site → Building → Storey → Element semantic hierarchy construction.
- GlobalId, entity class/label, names, containment, occurrence/type metadata, property sets, quantities, classifications and basic material extraction.
- Cancellation checks, staged progress reporting, structured diagnostics and elapsed load timing.
- Self-contained IFC fixtures covering schema detection, IFCZIP and semantic metadata extraction.

### Changed
- Default IFC loading remains semantic-only; geometry requests were explicitly deferred until 0.3.x.
- Repository version advanced to 0.2.0.

## [0.1.0] - 2026-08-31

### Added
- Repository foundation aligned with SpatialViewer.CadCore.
- .NET 10 solution layout, CI, tests and repository policy files.
- Renderer-agnostic scene contracts and IFC adapter boundaries.
- IFC/Revit development roadmap and compatibility policy.
