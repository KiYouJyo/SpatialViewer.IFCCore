# Third-Party Notices

SpatialViewer.IFCCore itself is MIT licensed. Third-party dependencies remain under their own licenses.

## xBIM Essentials

- **Package:** `Xbim.Essentials`
- **Pinned version:** `6.1.605`
- **Publisher:** xBIM Team
- **License:** CDDL-1.0
- **Use in SpatialViewer.IFCCore:** IFC STEP / IFCZIP schema detection and semantic BIM parsing inside `SpatialViewer.Formats.Ifc.Xbim`.

The xBIM dependency is isolated from `SpatialViewer.Core` and the renderer contracts. Geometry-specific xBIM packages are intentionally not introduced in 0.2.x; they require a separate review for the 0.3.x geometry pipeline.

No Autodesk Revit binaries, SDK assemblies, ODA binaries, or other proprietary runtime components are committed to this repository.
