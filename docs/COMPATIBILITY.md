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
| Stable object / Pick IDs | Supported since 0.4.0 | Deterministic identity for hit testing and property selection |
| Object/category/storey hide | Supported since 0.4.0 | RenderScene view-state filtering; no IFC reparse |
| Object isolate | Supported since 0.4.0 | RenderScene view-state filtering |
| Transparency overrides | Supported since 0.4.0 | Global/category/object opacity resolution |
| Material fallback | Supported since 0.4.0 | Renderer-neutral fallback material key by semantic category |
| Section Box | Supported since 0.4.0 | Bounds culling + backend/GPU precise clipping contract |
| Outline targets | Supported since 0.4.0 | Object-ID/depth outline and selection-highlight contract |
| Instanced render batches | Supported since 0.4.0 | Shared Mesh/Material/Opacity/Winding state with per-object instances |
| Perspective / orthographic camera | Supported since 0.4.0 | Orbit, pan, zoom, view/projection matrices |
| Background/cancellable source loading | Supported | xBIM source reader runs off the caller thread with staged cancellation/progress checks |
| In-memory BIM cache | Supported in 0.5.0 | Bounded LRU entry count; exact SceneDocument/MeshData reuse |
| `.svbim` disk cache | Supported in 0.5.0 | Internal versioned performance cache; not an interchange/archive format |
| Source fingerprint / option invalidation | Supported in 0.5.0 | SHA-256 + file length + cache-format/open-option signature |
| Corrupt cache fallback | Supported in 0.5.0 | Read/write failure does not block a cold IFC load |
| Cross-reader warm geometry reuse | Supported in 0.5.0 | Real xBIM/OpenCascade fixture verifies disk hit bypasses the wrapped geometry reader |
| Indexed RenderScene rebuild | Supported in 0.5.0 | `RenderSceneIndex` avoids repeated SceneDocument traversal for transient view changes |
| Load memory/working-set metrics | Supported in 0.5.0 | Sampled start/peak/end values; no universal numeric SLA yet |
| Render rebuild metrics | Supported in 0.5.0 | Elapsed time and thread-local managed allocation measurement |
| GPU upload estimate | Supported in 0.5.0 | Counts unique MeshData geometry, not repeated instances |
| Revit `.rvt` | Adapter only | No portable direct parser in Core |

Compatibility is verified by behavior-focused fixtures, not by extension alone. Geometry tests exercise real xBIM/OpenCascade representation mechanisms; rendering tests independently verify stable picking, property lookup, visibility/isolation, transparency, fallback materials, section-box behavior, outline targets, batching and camera operations.

The 0.5 cache gate additionally performs a real IFC4 geometry cold load through xBIM/OpenCascade, writes a SpatialViewer `.svbim` cache, then opens the same source through a new reader wrapper and verifies a disk hit without invoking the wrapped xBIM reader. Portable performance APIs measure behavior but deliberately do not claim a large-model SLA until a redistributable real-world corpus is available.

Representative Revit exports remain part of the growing cross-exporter fidelity/performance corpus. Proprietary Revit source files are never required by the portable test suite.
