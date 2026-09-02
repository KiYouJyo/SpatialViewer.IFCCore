# SpatialViewer.IFCCore

[English](README.en.md) | [日本語](README.ja.md)

SpatialViewer 的 BIM / IFC 读图内核。仓库负责 IFC 模型解析、BIM 语义树、属性与材料提取、真实几何三角化、渲染场景语义、缓存与性能测量，以及来自 Revit 的模型接入边界；不包含 WinUI 3 产品界面。

## 定位

- 主输入：IFC STEP (`.ifc`) 与 IFCZIP (`.ifczip`)；IFCXML 后续兼容。
- Schema：IFC2x3、IFC4、IFC4.3。
- IFC 语义适配：`Xbim.Essentials 6.1.605`。
- IFC 几何适配：`Xbim.Geometry 6.3.891-netcore` + 其 OpenCascade 几何运行时。
- xBIM/OpenCascade 类型全部隔离在 `SpatialViewer.Formats.Ifc.Xbim`，不会泄漏到 Core / UI 契约。
- `.rvt` 不在 portable Core 中逆向或直接解析；Revit 来源模型通过 IFC、Revit API exporter/sidecar、Autodesk Platform Services 或独立商业 SDK adapter 接入。

## 0.5.0 当前能力

- 完整保留 0.2–0.4 的 IFC/BIM 语义、真实 OpenCascade 几何以及 Picking、Hide/Isolate、Section Box、Batch、Camera 等看图交互契约。
- `XbimIfcModelReader` 本身继续提供后台 `Task` 加载、CancellationToken、分阶段 Progress；0.5 新增缓存检查/读取/写入阶段。
- `CachedIfcModelReader` 可在任意 `IIfcModelReader` 外层提供跨加载缓存，不修改 xBIM adapter。
- 内存缓存使用有界 LRU entry 数；命中时直接复用同一 `SceneDocument` 与共享 `MeshData`。
- 可选磁盘 `.svbim` 缓存保存 renderer-neutral `SceneDocument`，不会序列化 xBIM/OpenCascade 私有对象。
- 缓存键绑定源文件 SHA-256、长度、加载选项签名与缓存格式版本；源文件或几何/属性/Opening/Rebase 选项变化都会失效。
- `.svbim` 通过唯一 Mesh 表保存重复几何，跨 reader/process 恢复后仍保持实例间 Mesh 共享关系、材质槽、Transform、Bounds、WorldOrigin、属性与诊断。
- 缓存损坏/不可读/不可写不会阻止 IFC 冷加载；磁盘写入使用临时文件 + 原子替换。
- `RenderSceneIndex` 将已加载 BIM 场景预索引一次；后续 Hide/Isolate、透明度、Section Box 等视图状态变化无需再次递归 `SceneDocument`。
- `IfcLoadBenchmark` 分别记录加载耗时、managed heap 与 process working-set 的起始/峰值/结束采样，并区分 Miss / MemoryHit / DiskHit。
- `RenderPerformanceMetrics` 可测量 indexed RenderScene rebuild 的耗时与托管分配，并按唯一 Mesh 估算 GPU geometry upload，而不是按实例重复计算顶点。
- CI 为 NuGet/OpenCascade 依赖启用基于项目依赖图的缓存，减少重复 Restore 成本。

## 缓存使用示例

```csharp
IIfcModelReader reader = new CachedIfcModelReader(
    new XbimIfcModelReader(),
    new IfcModelCacheOptions
    {
        MemoryEntryLimit = 4,
        DiskCacheDirectory = cacheDirectory,
    });

var result = await reader.OpenAsync(
    path,
    new IfcOpenOptions { IncludeGeometry = true },
    cancellationToken);
```

`.svbim` 是 SpatialViewer 内部、带版本号的性能缓存，不是 IFC 的替代交换格式，也不应作为长期归档格式。

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

MIT。第三方组件遵循各自许可证，见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。0.5.0 未新增运行时第三方依赖。
