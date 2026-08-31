# SpatialViewer.IFCCore

[English](README.en.md) | [日本語](README.ja.md)

SpatialViewer 的 BIM / IFC 读图内核。仓库负责 IFC 模型解析、BIM 语义树、属性与材料提取、真实几何三角化、渲染数据准备，以及来自 Revit 的模型接入边界；不包含 WinUI 3 产品界面。

## 定位

- 主输入：IFC STEP (`.ifc`) 与 IFCZIP (`.ifczip`)；IFCXML 后续兼容。
- Schema：IFC2x3、IFC4、IFC4.3。
- IFC 语义适配：`Xbim.Essentials 6.1.605`。
- IFC 几何适配：`Xbim.Geometry 6.3.891-netcore` + 其 OpenCascade 几何运行时。
- 所有 xBIM/OpenCascade 类型均隔离在 `SpatialViewer.Formats.Ifc.Xbim`，不会泄漏到 Core / UI 契约。
- `.rvt`：不在核心层逆向或直接解析。Revit 来源文件通过 IFC、Revit API 导出/sidecar、Autodesk Platform Services 或独立商业 SDK 适配接入。

## 0.3.0 当前能力

- 实际打开 IFC STEP / IFCZIP，并识别 IFC2x3 / IFC4 / IFC4.3。
- 构建 Project → Site → Building → Storey → Element 空间树。
- 提取 GlobalId、实体类型/标签、名称、空间归属、Occurrence/Type、Pset、Quantity、Classification 与基础 Material。
- `IncludeGeometry=true` 时通过真实 xBIM/OpenCascade 管线生成三角网格，而不是占位接口。
- 保留 Positions、Normals、Triangle Indices、Style/Material slot。
- 几何统一为米制；保留世界包围盒，并对超大坐标自动进行局部原点 rebasing。
- 重复/映射几何复用同一 `MeshData`，实例仅保存 Transform。
- 支持镜像/负变换的 `FlipWinding` 语义。
- Opening/Void 默认参与宿主构件布尔切洞；可通过选项保留 Opening 自身几何。
- 提供取消、分阶段进度、结构化诊断和加载耗时。

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
