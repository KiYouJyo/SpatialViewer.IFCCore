# Compatibility

| Input / capability | Status | Notes |
|---|---|---|
| IFC STEP `.ifc` | Supported | Semantic and optional geometry loading through xBIM |
| IFCZIP `.ifczip` | Supported | Semantic and optional geometry loading through xBIM |
| IFCXML `.ifcxml` | Planned | Lower priority |
| IFC2x3 | Supported for parsing | Common Revit interoperability baseline; cross-schema geometry corpus is still expanding |
| IFC4 | Supported and geometry-tested | Primary generated golden geometry fixtures |
| IFC4.3 | Supported for parsing | Infrastructure / newer exchange workflows; geometry corpus is still expanding |
| Project → Site → Building → Storey → Element | Supported | Decomposition, nesting and spatial containment |
| Psets / quantities / classifications / basic materials | Supported | Exposed as `SceneProperty` values |
| Triangulated solid geometry | Supported | xBIM/OpenCascade → renderer-neutral triangle meshes |
| Repeated / mapped geometry | Supported | Shared mesh data with per-instance transforms |
| Mirrored / negative transforms | Supported | `FlipWinding` propagated to renderer contracts |
| Openings / voids | Supported | Host boolean result by default; opening geometry is opt-in |
| Metre normalization / large-coordinate rebasing | Supported | Local render origin plus preserved world bounds |
| Stable object / Pick IDs | Supported in 0.4.0 | Deterministic identity for hit testing and property selection |
| Object/category/storey hide | Supported in 0.4.0 | RenderScene view-state filtering; no IFC reparse |
| Object isolate | Supported in 0.4.0 | RenderScene view-state filtering |
| Transparency overrides | Supported in 0.4.0 | Global/category/object opacity resolution |
| Material fallback | Supported in 0.4.0 | Renderer-neutral fallback material key by semantic category |
| Section Box | Supported in 0.4.0 | Bounds culling + backend/GPU precise clipping contract |
| Outline targets | Supported in 0.4.0 | Object-ID/depth outline and selection-highlight contract |
| Instanced render batches | Supported in 0.4.0 | Shared Mesh/Material/Opacity/Winding state with per-object instances |
| Perspective / orthographic camera | Supported in 0.4.0 | Orbit, pan, zoom, view/projection matrices |
| Revit `.rvt` | Adapter only | No portable direct parser in Core |

Compatibility is verified by behavior-focused fixtures, not by extension alone. Geometry tests exercise real xBIM/OpenCascade representation mechanisms; rendering tests independently verify stable picking, property lookup, visibility/isolation, transparency, fallback materials, section-box behavior, outline targets, batching and camera operations.

Representative Revit exports remain part of the growing cross-exporter fidelity corpus. Proprietary Revit source files are never required by the portable test suite.
