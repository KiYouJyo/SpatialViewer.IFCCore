# Compatibility

| Input / capability | Status | Notes |
|---|---|---|
| IFC STEP `.ifc` | Supported in 0.2.x | Semantic loading through xBIM |
| IFCZIP `.ifczip` | Supported in 0.2.x | Semantic loading through xBIM |
| IFCXML `.ifcxml` | Planned | Lower priority |
| IFC2x3 | Supported in 0.2.x | Common Revit interoperability baseline |
| IFC4 | Supported in 0.2.x | Main modern building schema |
| IFC4.3 | Supported in 0.2.x | Infrastructure / newer exchange workflows |
| Project → Site → Building → Storey → Element | Supported in 0.2.x | Decomposition, nesting and spatial containment |
| Psets / quantities / classifications / basic materials | Supported in 0.2.x | Exposed as `SceneProperty` values |
| Triangulated geometry | Planned for 0.3.x | Geometry is intentionally deferred in 0.2.x |
| Revit `.rvt` | Adapter only | No portable direct parser in Core |

Compatibility is verified by fixtures, not by extension alone. Each distributable fixture should record authoring application, export version, schema, units, expected element count and known geometry edge cases. Proprietary Revit source files are never required by the portable test suite.
