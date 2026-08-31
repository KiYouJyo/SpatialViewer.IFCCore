# Development Plan

## Goal

Build a stable BIM viewing kernel that can display Revit-origin building models through open IFC first, while keeping a clean path for optional direct-RVT adapters later.

## Phase 0 — Repository foundation (0.1.x)

- Mirror CadCore repository conventions: .NET 10, `src/tests/docs/.github`, CI, policies and multilingual README.
- Establish Core / IFC contract / xBIM adapter / rendering boundaries.
- Define fixture metadata and compatibility matrix.
- Acceptance: solution restores, builds Debug + Release, tests run on CI, no proprietary dependency in Core.

## Phase 1 — IFC document loading (0.2.x)

- Integrate current xBIM packages in `SpatialViewer.Formats.Ifc.Xbim` only.
- Load STEP and IFCZIP; detect IFC2x3 / IFC4 / IFC4.3.
- Extract Project → Site → Building → Storey → Element hierarchy.
- Preserve GlobalId, entity label, class, name, type and containment.
- Extract property sets, quantities, classifications and basic materials.
- Add cancellation, progress and structured diagnostics.
- Acceptance: representative Revit-exported fixtures open without UI code and semantic counts match expected metadata.

## Phase 2 — Geometry pipeline (0.3.x)

- Generate triangulated geometry and per-instance transforms.
- Normalize units to metres and handle mapped items / repeated families efficiently.
- Preserve normals, triangle indices, material slots and world/local bounds.
- Introduce local-origin rebasing for large coordinates.
- Handle openings, voids, negative transforms and mirrored instances.
- Acceptance: walls, slabs, roofs, doors, windows, stairs, railings, MEP proxies and repeated families render with correct placement and scale.

## Phase 3 — BIM rendering semantics (0.4.x)

- Convert Core scene to renderer-friendly batches.
- Stable object IDs for hit testing and property selection.
- Category / storey visibility filters, isolate/hide, transparency and section clipping contracts.
- Edge/outline data and material fallbacks.
- Acceptance: scene output supports orbit/pan/zoom, selection, hide/isolate and section-box workflows without reparsing IFC.

## Phase 4 — Performance and cache (0.5.x)

- Background loading pipeline with cancellation and progress stages.
- Deduplicate repeated meshes and introduce geometry/material caches.
- Define optional on-disk SpatialViewer BIM cache with source fingerprint and schema/version stamp.
- Benchmark cold open, warm open, peak memory and GPU upload separately.
- Acceptance targets for reference models: deterministic output, bounded peak memory, cancellable load and materially faster warm-open path.

## Phase 5 — Revit-source adapters (0.6.x+)

- Build a separate Revit API exporter/sidecar specification.
- Evaluate neutral cache export versus IFC handoff for fidelity/performance.
- Keep Autodesk Platform Services as optional online adapter.
- Evaluate commercial direct-RVT SDK only if product requirements justify licensing.
- Acceptance: no Revit-version-specific dependency is introduced into the portable Core.

## Phase 6 — Fidelity hardening (0.7.x+)

- Large coordinate models, linked models and federated datasets.
- Complex swept solids, advanced BReps and tessellated geometry.
- Material/texturing edge cases and Revit exporter variations.
- Corrupt/malicious IFC resilience and resource limits.
- Build a golden fixture suite with image/geometry/semantic regression metrics.

## Release gate

A release is not considered complete only because a sample model opens. Each milestone must pass: schema compatibility, semantic correctness, geometry correctness, performance regression, malformed-input safety and license review.
