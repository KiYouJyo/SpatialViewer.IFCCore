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
SpatialViewer.Core                 immutable-ish scene graph, BIM metadata, mesh primitives
        |
        v
SpatialViewer.Rendering            view state, picking, batches, clipping, camera
        |
        +----------------------+
        v                      v
SpatialViewer.Rendering.Windows   caller / SpatialViewer UI
```

## Dependency rules

- `SpatialViewer.Core` contains no xBIM, OpenCascade, Autodesk, WinUI or Windows-only types.
- `SpatialViewer.Formats.Ifc.Xbim` owns all xBIM schema, model-store and geometry-engine integration.
- IFC schema-specific objects are normalized before entering Core.
- Geometry positions are normalized to metres; source world bounds are retained while render-space coordinates may be rebased around a local scene origin.
- Shared source geometry is represented once as `MeshData`; placement remains on per-instance `SceneNode` transforms.
- Mirrored instances keep an explicit winding-flip flag instead of mutating shared index buffers.
- The Rendering layer consumes Core but never mutates IFC semantics to represent transient view state.
- The Windows rendering project may consume native GPU APIs, but the render-scene and camera contracts stay platform-neutral.

## Geometry flow

```text
IfcStore
  -> Xbim3DModelContext / OpenCascade
  -> XbimShapeGeometry binary triangulation
  -> MeshData (metres, normals, indices, style slot)
  -> SceneNode instance transform + bounds + FlipWinding
  -> RenderScene
```

## 0.4 render flow

```text
SceneDocument
  -> semantic owner/storey context
  -> stable ObjectId + deterministic PickId
  -> visibility + isolate/hide filters
  -> opacity/material fallback resolution
  -> section-box bounds culling
  -> RenderMesh
  -> RenderBatch (shared mesh/material/render state + instances)
  -> Windows/GPU backend
```

`PickMap` maps a hit-test integer back to object identity, category, storey and a property snapshot. `OutlineTargets` allow a backend to implement object-ID/depth based outlines without requiring IFC geometry to be regenerated. A Section Box is preserved on the scene so intersecting geometry can be clipped precisely by the backend; only objects whose bounds are completely outside the box are omitted during RenderScene construction.

`RenderCamera` is independent of IFC and WinUI. It supplies perspective/orthographic projections plus orbit, pan and zoom transformations so the same scene can be navigated without reparsing source data.

## Data model goals

The Core scene preserves source BIM facts. The Rendering scene adds transient view semantics: selection identity, visibility, appearance overrides, clipping, outline targets, instancing batches and camera state. This keeps data extraction and interactive viewing independently replaceable.
