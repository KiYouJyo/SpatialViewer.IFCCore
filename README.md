# SpatialViewer.IFCCore

[English](README.en.md) | [日本語](README.ja.md)

SpatialViewer 的 BIM / IFC 读图内核。仓库负责 IFC 模型解析、BIM 语义树、属性与材料提取、几何转换、渲染数据准备，以及来自 Revit 的模型接入边界；不包含 WinUI 3 产品界面。

## 定位

- 主输入：IFC STEP (`.ifc`) 与 IFCZIP (`.ifczip`)；IFCXML 后续兼容。
- 当前支持的 schema：IFC2x3、IFC4、IFC4.3。
- IFC 适配器：`Xbim.Essentials`，隔离在 `SpatialViewer.Formats.Ifc.Xbim`。
- `.rvt`：不在核心层直接解析。Revit 来源文件通过 Revit API 导出、Autodesk Platform Services 或独立商业 SDK 适配接入。
- 输出：与 UI、具体渲染后端解耦的 `SceneDocument` / BIM 语义与后续网格数据。

## 0.2.0 当前能力

- 实际打开 IFC STEP / IFCZIP，而不是占位接口。
- 自动识别 IFC2x3 / IFC4 / IFC4.3。
- 构建 Project → Site → Building → Storey → Element 空间树。
- 提取 GlobalId、实体类型/标签、名称、空间归属、Occurrence/Type 信息。
- 提取 Property Set、Quantity、Classification 与基础 Material。
- 提供取消、分阶段进度、结构化诊断和加载耗时。
- 几何三角化明确留在 0.3.x，不在 0.2.x 混入 Geometry 依赖。

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
