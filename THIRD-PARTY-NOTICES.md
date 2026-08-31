# Third-Party Notices

SpatialViewer.IFCCore itself is MIT licensed. Third-party dependencies remain under their own licenses.

## xBIM Essentials

- **Package:** `Xbim.Essentials`
- **Pinned version:** `6.1.605`
- **Publisher:** xBIM Team
- **License:** CDDL-1.0
- **Use in SpatialViewer.IFCCore:** IFC STEP / IFCZIP schema detection, model access and semantic BIM parsing inside `SpatialViewer.Formats.Ifc.Xbim`.

## xBIM Geometry

- **Package:** `Xbim.Geometry`
- **Pinned version:** `6.3.891-netcore`
- **Publisher:** xBIM Team
- **Project license:** CDDL-1.0
- **Use in SpatialViewer.IFCCore:** IFC geometry context generation, boolean processing, mapped/repeated geometry and binary triangle-mesh extraction inside `SpatialViewer.Formats.Ifc.Xbim`.
- **Release note:** this is the xBIM .NET/Core geometry line used for the 0.3.0 implementation; package upgrades must pass the geometry golden suite before adoption.

## OpenCascade and transitive geometry components

`Xbim.Geometry` uses native OpenCascade technology for geometric/topological operations and may redistribute additional transitive components under their own licenses. Those components are not relicensed by SpatialViewer.IFCCore. Distribution of binaries must retain the license/notices supplied by the xBIM geometry package and its transitive dependencies.

No Autodesk Revit binaries, Autodesk SDK assemblies, ODA binaries, commercial direct-RVT SDK binaries, or other proprietary Revit runtime components are committed to this repository.

The xBIM/OpenCascade dependency boundary is intentionally isolated from `SpatialViewer.Core` and renderer contracts so a future geometry implementation can be substituted without changing the portable scene model.
