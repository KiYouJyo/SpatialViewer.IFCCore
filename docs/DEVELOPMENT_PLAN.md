# Development Plan

## Goal

Build a stable BIM viewing kernel that can display Revit-origin building models through open IFC first, while keeping a clean path for optional direct-RVT adapters later.

## Phase 0 — Repository foundation (0.1.x) — Complete

- CadCore-style .NET 10 repository, tests, CI, policies and renderer-independent boundaries.

## Phase 1 — IFC document loading (0.2.x) — Complete

- IFC STEP / IFCZIP, IFC2x3 / IFC4 / IFC4.3, BIM hierarchy, identity, Psets, quantities, classifications, materials, cancellation/progress/diagnostics.

## Phase 2 — Geometry pipeline (0.3.x) — Complete

- Real xBIM/OpenCascade tessellation, metre normalization, shared meshes, transforms, bounds, large-coordinate rebasing, style slots, mirrored winding and opening/void handling.
- Generated IFC4 fixtures verify the underlying representation mechanisms through the native geometry runtime.

## Phase 3 — BIM rendering semantics (0.4.x) — Implemented

- Convert Core geometry into renderer-friendly `RenderMesh` and instanced `RenderBatch` output.
- Generate stable ObjectId and deterministic `uint PickId` before view-state filtering, so hide/isolate does not renumber unaffected objects.
- Map PickId directly to source identity, name, category, storey and `SceneProperty` snapshots for property selection.
- Apply object/category/storey hide and object isolate without modifying or reparsing `SceneDocument`.
- Apply global/category/object opacity overrides and renderer-neutral category material fallbacks.
- Carry Section Box state; cull fully outside bounds while retaining intersecting geometry for backend/GPU precise clipping.
- Expose per-object outline targets for object-ID/depth outlines and selection highlighting.
- Batch shared Mesh/Material/Opacity/Winding state while preserving per-instance transform, bounds and PickId.
- Provide platform-neutral perspective/orthographic camera contracts with orbit, pan, zoom and view/projection matrices.

### 0.4 acceptance boundary

Interactive view state is intentionally downstream of IFC parsing. A loaded `SceneDocument` can be reused for camera navigation, selection, hide/isolate, transparency and section-box changes. 0.4 verifies the contracts and deterministic scene transformations; concrete Direct3D/WinUI drawing remains the responsibility of `SpatialViewer.Rendering.Windows` and the product UI.

## Phase 4 — Performance and cache (0.5.x)

- Background loading pipeline with cancellation and progress stages.
- Deduplicate repeated meshes and introduce geometry/material caches beyond one load operation.
- Define optional on-disk SpatialViewer BIM cache with source fingerprint and schema/version stamp.
- Benchmark cold open, warm open, RenderScene rebuild, peak memory and GPU upload separately.
- Add dependency/CI caching where it materially reduces the native geometry restore path.
- Acceptance targets: deterministic output, bounded peak memory, cancellable load and materially faster warm-open/view-state rebuild paths.

## Phase 5 — Revit-source adapters (0.6.x+)

- Separate Revit API exporter/sidecar specification, optional APS adapter and commercial direct-RVT evaluation without contaminating portable Core.

## Phase 6 — Fidelity hardening (0.7.x+)

- Large coordinate models, linked/federated datasets, complex swept solids/BReps/tessellated geometry, textures and discipline-specific Revit exporter variations.
- Grow a redistributable golden corpus for architecture, structure and MEP content.

## Release gate

A release is not considered complete because one sample opens. Each milestone must pass restore, Debug build, Release build and automated tests relevant to its scope, plus architecture/license review when dependencies change.
