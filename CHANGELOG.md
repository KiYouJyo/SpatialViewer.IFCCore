# Changelog

All notable changes to SpatialViewer.IFCCore are documented here.

## [Unreleased]

### Planned
- Add geometry tessellation, transforms, unit normalization and render-scene conversion for 0.3.x.
- Expand the golden fixture corpus with redistributable Revit-origin IFC exports.

## [0.2.0] - 2026-08-31

### Added
- Integrated `Xbim.Essentials` 6.1.605 behind `SpatialViewer.Formats.Ifc.Xbim`.
- Real IFC STEP and IFCZIP loading with IFC2x3 / IFC4 / IFC4.3 schema detection.
- Project → Site → Building → Storey → Element semantic hierarchy construction.
- GlobalId, entity class/label, names, containment, occurrence/type metadata, property sets, quantities, classifications and basic material extraction.
- Cancellation checks, staged progress reporting, structured diagnostics and elapsed load timing.
- Self-contained IFC fixtures covering schema detection, IFCZIP and semantic metadata extraction.

### Changed
- Default IFC loading remains semantic-only; geometry requests emit an explicit deferred diagnostic until 0.3.x.
- Repository version advanced to 0.2.0.

## [0.1.0] - 2026-08-31

### Added
- Repository foundation aligned with SpatialViewer.CadCore.
- .NET 10 solution layout, CI, tests and repository policy files.
- Renderer-agnostic scene contracts and IFC adapter boundaries.
- IFC/Revit development roadmap and compatibility policy.
