# Compatibility

| Input | Initial target | Notes |
|---|---|---|
| IFC STEP `.ifc` | Required | First production path |
| IFCZIP `.ifczip` | Planned | Adapter-level feature |
| IFCXML `.ifcxml` | Planned | Lower priority |
| IFC2x3 | Required | Common Revit interoperability baseline |
| IFC4 | Required | Main modern building schema |
| IFC4.3 | Required | Infrastructure / newer exchange workflows |
| Revit `.rvt` | Adapter only | No portable direct parser in Core |

Compatibility is verified by fixtures, not by extension alone. Each fixture should record authoring application, export version, schema, units, expected element count and known geometry edge cases.
