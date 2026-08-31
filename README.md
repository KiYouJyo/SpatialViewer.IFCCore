# SpatialViewer.IFCCore

[English](README.en.md) | [日本語](README.ja.md)

SpatialViewer 的 BIM / IFC 读图内核。仓库负责 IFC 模型解析、BIM 语义树、属性与材料提取、几何转换、渲染数据准备，以及来自 Revit 的模型接入边界；不包含 WinUI 3 产品界面。

## 定位

- 主输入：IFC STEP (`.ifc`)，后续兼容 IFCZIP / IFCXML。
- 目标 schema：IFC2x3、IFC4、IFC4.3。
- 首选 .NET 适配器：xBIM，隔离在 `SpatialViewer.Formats.Ifc.Xbim`。
- `.rvt`：不在核心层直接解析。Revit 来源文件通过 Revit API 导出、Autodesk Platform Services 或独立商业 SDK 适配接入。
- 输出：与 UI、具体渲染后端解耦的 `SceneDocument` / BIM 语义与网格数据。

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

## 开发状态

当前为 `0.1.0` 仓库基础阶段。具体里程碑见 [开发计划](docs/DEVELOPMENT_PLAN.md)。

## 构建

```powershell
dotnet restore SpatialViewer.IFCCore.sln
dotnet build SpatialViewer.IFCCore.sln -c Release
dotnet test SpatialViewer.IFCCore.sln -c Release
```

## License

MIT。第三方组件遵循各自许可证，见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
