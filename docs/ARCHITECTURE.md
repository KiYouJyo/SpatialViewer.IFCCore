# Architecture

SpatialViewer.IFCCore follows the same separation principle as SpatialViewer.CadCore while using BIM-specific semantics.

```text
IFC / Revit-origin source
        |
        v
SpatialViewer.Formats.Ifc          contracts, cache wrapper, performance measurement
        |
        +--> memory LRU / versioned .svbim SceneDocument cache
        |
        v  (cache miss)
SpatialViewer.Formats.Ifc.Xbim     xBIM semantic + geometry implementation boundary
        |
        v
SpatialViewer.Core                 immutable-ish scene graph, BIM metadata, mesh primitives
        |
        v
SpatialViewer.Rendering            indexed view state, picking, batches, clipping, camera
        |
        +----------------------+
        v                      v
SpatialViewer.Rendering.Windows   caller / SpatialViewer UI
```

## Dependency rules

- `SpatialViewer.Core` contains no xBIM, OpenCascade, Autodesk, WinUI or Windows-only types.
- `SpatialViewer.Formats.Ifc.Xbim` owns all xBIM schema, model-store and geometry-engine integration.
- IFC schema-specific objects are normalized before entering Core.
- `CachedIfcModelReader` sits outside a concrete reader and stores only renderer-neutral `SceneDocument` state; the cache format never serializes xBIM/OpenCascade objects.
- Geometry positions are normalized to metres; source world bounds are retained while render-space coordinates may be rebased around a local scene origin.
- Shared source geometry is represented once as `MeshData`; placement remains on per-instance `SceneNode` transforms.
- Mirrored instances keep an explicit winding-flip flag instead of mutating shared index buffers.
- The Rendering layer consumes Core but never mutates IFC semantics to represent transient view state.
- The Windows rendering project may consume native GPU APIs, but the render-scene, performance and camera contracts stay platform-neutral.

## Geometry flow

```text
IfcStore
  -> Xbim3DModelContext / OpenCascade
  -> XbimShapeGeometry binary triangulation
  -> MeshData (metres, normals, indices, style slot)
  -> SceneNode instance transform + bounds + FlipWinding
  -> SceneDocument
```

## 0.5 load/cache flow

```text
OpenAsync(path, options)
  -> SHA-256 source fingerprint + option signature
  -> memory LRU lookup
       -> hit: reuse exact SceneDocument / MeshData references
  -> .svbim disk lookup
       -> hit: restore renderer-neutral SceneDocument
  -> cache miss
       -> wrapped IIfcModelReader (xBIM/OpenCascade in the default adapter)
       -> optional atomic .svbim write
       -> memory LRU insert
```

The cache identity includes source SHA-256, source length, geometry/property/opening/rebase option state and the cache-format version. Source changes or incompatible open options therefore cannot silently reuse a previous scene. Disk-cache read/write failures are non-fatal: the source reader remains the authority and a normal cold load continues.

`.svbim` uses a unique mesh table plus per-node mesh references, so repeated BIM instances do not duplicate vertex buffers in the cache. The file also carries transforms, local/world bounds, world origin, material/style slots, BIM properties, document metadata and load diagnostics. It is an internal versioned performance artifact, not an IFC interchange or archival format.

## Render flow

```text
SceneDocument
  -> RenderSceneIndex.Create (one semantic/tree traversal)
  -> stable RenderMesh templates + RenderObjectInfo index
  -> repeated Build(options)
       -> visibility + isolate/hide filters
       -> opacity/material fallback resolution
       -> section-box bounds culling
       -> RenderMesh / RenderBatch / PickMap / OutlineTargets
  -> Windows/GPU backend
```

`RenderSceneIndex` is a snapshot of source BIM/render facts. Transient view-state rebuilds no longer recurse `SceneDocument`; callers recreate the index only when the underlying scene facts change.

`PickMap` maps a hit-test integer back to object identity, category, storey and a property snapshot. `OutlineTargets` allow a backend to implement object-ID/depth based outlines without requiring IFC geometry to be regenerated. A Section Box is preserved on the scene so intersecting geometry can be clipped precisely by the backend; only objects whose bounds are completely outside the box are omitted during scene construction.

`RenderCamera` is independent of IFC and WinUI. It supplies perspective/orthographic projections plus orbit, pan and zoom transformations so the same scene can be navigated without reparsing source data.

## Performance measurement

- `IfcLoadBenchmark` reports elapsed time and sampled managed-heap / process-working-set start, peak and end values, while classifying the load as uncached, cache miss, memory hit or disk hit.
- `RenderPerformanceMetrics.MeasureRebuild` measures indexed view-state rebuild elapsed time and thread-local managed allocations.
- `RenderPerformanceMetrics.EstimateGpuUpload` counts positions/normals/indices by unique `MeshData` reference so instanced geometry is not multiplied by instance count.
- These are measurement contracts, not a universal SLA. Reference-model thresholds should be added only from a redistributable real-model performance corpus.

## Data model goals

The Core scene preserves source BIM facts. The cache persists that renderer-neutral scene without importing provider-specific state. The Rendering scene adds transient view semantics: selection identity, visibility, appearance overrides, clipping, outline targets, instancing batches and camera state. This keeps extraction, caching and interactive viewing independently replaceable.
