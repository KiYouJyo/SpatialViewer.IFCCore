# Changelog

All notable changes to SpatialViewer.IFCCore are documented here.

## [Unreleased]

### Planned
- Performance/cache work for 0.5.x, including warm-view rebuild benchmarks and geometry/material caching beyond one load operation.
- Expand the redistributable golden corpus with representative Revit-origin IFC exports and malformed-input cases.

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
- Per-instance transforms, local shape displacement, shared-mesh reuse, metre normalization, local-origin rebasing and local/world bounds.
- Surface-style slots, mirrored winding semantics, opening/void boolean handling, geometry progress and generated geometry fixtures.

## [0.2.0] - 2026-08-31

### Added
- Integrated `Xbim.Essentials` 6.1.605 behind `SpatialViewer.Formats.Ifc.Xbim`.
- IFC STEP / IFCZIP loading, IFC2x3 / IFC4 / IFC4.3 schema detection, BIM hierarchy and common semantic metadata extraction.
- Property sets, quantities, classifications, materials, cancellation, progress, diagnostics and semantic fixtures.

## [0.1.0] - 2026-08-31

### Added
- Repository foundation aligned with SpatialViewer.CadCore.
- .NET 10 solution layout, CI, tests and repository policy files.
- Renderer-agnostic scene contracts and IFC adapter boundaries.
