# Architecture

SpatialViewer.IFCCore follows the same separation principle as SpatialViewer.CadCore while using BIM-specific semantics.

```text
IFC / Revit-origin source
        |
        v
SpatialViewer.Formats.Ifc          contracts, schema/open options
        |
        v
SpatialViewer.Formats.Ifc.Xbim     xBIM implementation boundary
        |
        v
SpatialViewer.Core                 scene graph, BIM metadata, mesh primitives
        |
        +----------------------+
        v                      v
SpatialViewer.Rendering       caller / SpatialViewer UI
        |
        v
SpatialViewer.Rendering.Windows
```

## Dependency rules

- `SpatialViewer.Core` contains no xBIM, Autodesk, WinUI or Windows-only types.
- IFC schema-specific objects are normalized before entering Core.
- Geometry coordinates are normalized to metres and local scene origin before renderer upload.
- Source entity identity (GlobalId + source label) is preserved for selection and property inspection.
- The Windows rendering project may consume native GPU APIs, but the render-scene contract stays platform-neutral.

## Data model goals

The Core scene must preserve: hierarchy, source identity, category/type, property sets, materials, transforms, bounds, meshes, visibility state and stable selection IDs.
