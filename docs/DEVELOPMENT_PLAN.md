# Development Plan

## Goal

Build a stable BIM viewing kernel that can display Revit-origin building models through open IFC first, while keeping a clean path for optional direct-RVT adapters later.

## Phase 0 — Repository foundation (0.1.x) — Complete

- Mirror CadCore repository conventions: .NET 10, `src/tests/docs/.github`, CI, policies and multilingual README.
- Establish Core / IFC contract / xBIM adapter / rendering boundaries.
- Define fixture metadata and compatibility matrix.
- Acceptance: solution restores, builds Debug + Release, tests run on CI, no proprietary dependency in Core.

## Phase 1 — IFC document loading (0.2.x) — Complete

- `Xbim.Essentials` is pinned inside `SpatialViewer.Formats.Ifc.Xbim` only.
- Load STEP and IFCZIP; detect IFC2x3 / IFC4 / IFC4.3.
- Extract Project → Site → Building → Storey → Element hierarchy from decomposition, nesting and spatial containment relations.
- Preserve GlobalId, entity label, class, name, type and containment metadata.
- Extract property sets, quantities, classifications and basic materials.
- Provide cancellation checks, staged progress and structured diagnostics.
- Automated fixtures validate schemas, IFCZIP, semantic hierarchy and common metadata without product UI code.

## Phase 2 — Geometry pipeline (0.3.x) — Complete

- `Xbim.Geometry` is isolated inside `SpatialViewer.Formats.Ifc.Xbim`; Core remains free of xBIM/OpenCascade types.
- Generate real triangulated geometry with positions, normals and triangle indices.
- Normalize source length units to metres.
- Preserve per-instance transforms and local shape displacement.
- Deduplicate repeated/mapped shapes into shared `MeshData` with separate instance nodes.
- Preserve surface-style slots for later material resolution.
- Compute local mesh bounds, transformed world bounds and document bounds.
- Rebase large world coordinates to a local scene origin while preserving original world bounds.
- Preserve mirrored/negative transforms with an explicit winding-flip flag.
- Use xBIM's opening/void boolean results for host elements; optionally expose opening-element geometry.
- Propagate geometry flags and bounds through the renderer-neutral scene contract.
- Generated IFC4 fixtures verify solid tessellation, repeated geometry, mapped mirrors, styles, openings and large coordinates through the real xBIM/OpenCascade runtime.

### 0.3 acceptance boundary

The geometry pipeline is representation-driven rather than category-specific: walls, slabs, roofs, doors, windows, stairs, railings and MEP products that expose supported IFC body representations use the same extraction path. The portable release gate therefore verifies the underlying representation mechanisms. A growing set of representative Revit-origin exports remains a fidelity-hardening corpus for exporter/category-specific regressions.

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

Interactive view state is downstream of IFC parsing. A loaded `SceneDocument` can be reused for camera navigation, selection, hide/isolate, transparency and section-box changes. 0.4 verifies the contracts and deterministic scene transformations; concrete Direct3D/WinUI drawing remains the responsibility of `SpatialViewer.Rendering.Windows` and the product UI.

## Phase 4 — Performance and cache (0.5.x) — Implemented

- Keep source parsing/geometry work background/cancellable and extend staged progress with cache check/read/write phases.
- Add `CachedIfcModelReader` around the reader contract instead of embedding cache policy into xBIM-specific code.
- Use a bounded in-memory LRU entry cache for exact `SceneDocument` and shared `MeshData` reuse across opens.
- Add a versioned internal `.svbim` disk cache for renderer-neutral scene state with unique mesh-table deduplication.
- Bind cache identity to SHA-256 source fingerprint, file length, cache-format version and geometry/property/opening/rebase option signature.
- Recover from corrupt/unreadable/unwritable cache state by falling back to the source reader; write disk entries through temporary files and atomic replacement.
- Add `RenderSceneIndex` so repeated visibility/appearance/section rebuilds filter pre-indexed render candidates rather than retraversing the BIM tree.
- Measure cold/miss/memory-hit/disk-hit loading with elapsed time plus sampled managed-heap and process-working-set start/peak/end values.
- Measure indexed RenderScene rebuild elapsed time and allocations separately from source loading.
- Estimate GPU geometry upload by unique `MeshData` reference, keeping repeated instances separate from vertex/index upload size.
- Cache NuGet/OpenCascade CI dependencies by dependency-graph inputs to reduce repeated native-package restore cost.
- Validate disk warm-open behavior against the real xBIM/OpenCascade IFC4 geometry fixture: a new wrapped xBIM reader is not invoked on a valid `.svbim` hit.

### 0.5 acceptance boundary

0.5 establishes deterministic cache invalidation, warm-path bypass, bounded retained cache entries and reproducible performance measurement contracts. It does **not** claim a universal large-Revit-model latency or memory SLA yet: hard numeric thresholds require a redistributable real-world performance corpus across model sizes/exporters. `.svbim` is an internal versioned performance artifact rather than a public interchange or archival format.

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
- Category/discipline corpus: walls, slabs, roofs, doors, windows, stairs, railings, families and MEP content from redistributable real exports.
- Corrupt/malicious IFC resilience and resource limits.
- Build a golden fixture suite with image/geometry/semantic regression metrics.

## Release gate

A release is not considered complete only because a sample model opens. Each milestone must pass the checks relevant to its scope: schema compatibility, semantic correctness, geometry correctness, deterministic transforms/bounds, rendering-semantic determinism, cache invalidation/recovery, performance regression when applicable, malformed-input safety and license review.
