# SpatialViewer.IFCCore

[English](README.en.md) | [日本語](README.ja.md)

SpatialViewer 的 BIM / IFC 读图内核。仓库负责 IFC 模型解析、BIM 语义树、属性与材料提取、真实几何三角化、渲染场景语义，以及来自 Revit 的模型接入边界；不包含 WinUI 3 产品界面。

## 定位

- 主输入：IFC STEP (`.ifc`) 与 IFCZIP (`.ifczip`)；IFCXML 后续兼容。
- Schema：IFC2x3、IFC4、IFC4.3。
- IFC 语义适配：`Xbim.Essentials 6.1.605`。
- IFC 几何适配：`Xbim.Geometry 6.3.891-netcore` + 其 OpenCascade 几何运行时。
- xBIM/OpenCascade 类型全部隔离在 `SpatialViewer.Formats.Ifc.Xbim`，不会泄漏到 Core / UI 契约。
- `.rvt` 不在 portable Core 中逆向或直接解析；Revit 来源模型通过 IFC、Revit API exporter/sidecar、Autodesk Platform Services 或独立商业 SDK adapter 接入。

## 0.4.0 当前能力

- 0.2/0.3 的 IFC STEP / IFCZIP、BIM 语义、Pset/Quantity/Classification/Material 与真实三角几何能力全部保留。
- `RenderScene` 从已加载的 `SceneDocument` 生成，不需要重新解析 IFC。
- 基于 SourceId/SceneNodeId 生成稳定 ObjectId 与确定性 `uint PickId`；Hide/Isolate 等视图变化不会改变未变对象的 PickId。
- PickMap 直接提供名称、类别、楼层与 `SceneProperty` 快照，用于命中后的属性检查。
- 支持按对象、类别、楼层 Hide，以及对象 Isolate。
- 支持全局/类别/对象透明度覆盖，并为缺失材质生成 renderer-neutral fallback material key。
- 支持 Section Box：完全位于盒外的对象在场景构建时粗裁剪，相交对象保留给 GPU/backend 做精确 clipping。
- 生成每对象 Outline Target，供 object-ID/depth outline 或选中高亮使用。
- 按共享 Mesh + Material + Opacity + Winding 生成实例化 `RenderBatch`，重复 BIM 几何无需重复上传顶点。
- 提供平台无关 `RenderCamera`，支持 Perspective/Orthographic、Orbit、Pan、Zoom 与 View/Projection matrix。

## 仓库结构

```text
src/
  SpatialViewer.Core/
  SpatialViewer.Formats.Ifc/
  SpatialViewer.Formats.Ifc.Xbim/
  SpatialViewer.Rendering/
  SpatialViewer.Rendering.Windows/
tests/
  SpatialViewer.Core.Tests/
  SpatialViewer.Ifc.Tests/
  SpatialViewer.Rendering.Tests/
  fixtures/
docs/
  ARCHITECTURE.md
  COMPATIBILITY.md
  DEVELOPMENT_PLAN.md
  REVIT_INTEGRATION.md
```

## 构建

```powershell
dotnet restore SpatialViewer.IFCCore.sln
dotnet build SpatialViewer.IFCCore.sln -c Release
dotnet test SpatialViewer.IFCCore.sln -c Release
```

## License

MIT。第三方组件遵循各自许可证，见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
