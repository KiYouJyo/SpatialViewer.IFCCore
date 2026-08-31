# Architecture

SpatialViewer.IFCCore follows the same separation principle as SpatialViewer.CadCore while using BIM-specific semantics.

```text
IFC / Revit-origin source
        |
        v
SpatialViewer.Formats.Ifc          contracts, schema/open options
        |
        v
SpatialViewer.Formats.Ifc.Xbim     xBIM semantic + geometry implementation boundary
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

- `SpatialViewer.Core` contains no xBIM, OpenCascade, Autodesk, WinUI or Windows-only types.
- `SpatialViewer.Formats.Ifc.Xbim` owns all xBIM schema, model-store and geometry-engine integration.
- IFC schema-specific objects are normalized before entering Core.
- Geometry positions are normalized to metres; source world bounds are retained while render-space coordinates may be rebased around a local scene origin.
- Shared source geometry is represented once as `MeshData`; placement remains on per-instance `SceneNode` transforms.
- Mirrored instances keep an explicit winding-flip flag instead of mutating shared index buffers.
- xBIM surface-style labels are carried as renderer-neutral material IDs; material rendering policy remains outside the IFC adapter.
- Source entity identity (GlobalId + source label) is preserved for selection and property inspection.
- The Windows rendering project may consume native GPU APIs, but the render-scene contract stays platform-neutral.

## Geometry flow

```text
IfcStore
  -> Xbim3DModelContext / OpenCascade
  -> XbimShapeGeometry binary triangulation
  -> MeshData (metres, normals, indices, style slot)
  -> SceneNode instance transform + bounds + FlipWinding
  -> RenderScene / backend
```

Opening/void subtraction is resolved by the xBIM geometry context before host geometry enters Core. Opening feature geometry is hidden by default and can be retained explicitly for diagnostic/editing workflows.

## Data model goals

The Core scene must preserve hierarchy, source identity, category/type, property sets, materials, transforms, local/world bounds, shared meshes and stable selection identity while remaining independent of a concrete UI or rendering API.
