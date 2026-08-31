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
| Triangulated solid geometry | Supported in 0.3.0 | xBIM/OpenCascade → renderer-neutral triangle meshes |
| Normals / triangle indices | Supported in 0.3.0 | Preserved in `MeshData` |
| Repeated / mapped geometry | Supported in 0.3.0 | Shared mesh data with per-instance transforms |
| Mirrored / negative transforms | Supported in 0.3.0 | `FlipWinding` is propagated to the renderer contract |
| Surface-style slot | Supported in 0.3.0 | xBIM style label is preserved as a renderer-neutral material ID |
| Openings / voids | Supported in 0.3.0 | Host boolean result by default; opening geometry is opt-in |
| Metre normalization | Supported in 0.3.0 | Source length units are normalized before renderer upload |
| Large-coordinate rebasing | Supported in 0.3.0 | Local scene origin plus preserved original world bounds |
| Revit `.rvt` | Adapter only | No portable direct parser in Core |

Compatibility is verified by behavior-focused fixtures, not by extension alone. The portable 0.3 geometry tests exercise the representation mechanisms used across BIM categories; representative Revit exports for walls, slabs, roofs, doors, windows, stairs, railings and MEP content remain part of the growing cross-exporter fidelity corpus.

Each distributable real-world fixture should record authoring application, export version, schema, units, expected element count, expected bounds and known geometry edge cases. Proprietary Revit source files are never required by the portable test suite.
