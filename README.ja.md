# SpatialViewer.IFCCore

[中文](README.md) | [English](README.en.md)

SpatialViewer.IFCCore は SpatialViewer の BIM / IFC 読み込みコアです。IFC 解析、BIM 階層・属性、実ジオメトリの三角形化、renderer-neutral な操作・描画セマンティクス、キャッシュ／性能計測、および Revit 由来モデルの接続境界を担当し、WinUI 3 の製品 UI は含みません。

## 対象

- 主入力：IFC STEP (`.ifc`) と IFCZIP (`.ifczip`)。IFCXML は後続対応です。
- Schema：IFC2x3、IFC4、IFC4.3。
- セマンティクス：`Xbim.Essentials 6.1.605`。
- ジオメトリ：`Xbim.Geometry 6.3.891-netcore` とその OpenCascade ランタイム。
- xBIM / OpenCascade 型は `SpatialViewer.Formats.Ifc.Xbim` 内に隔離し、Core や UI 契約には公開しません。
- `.rvt` を portable Core で逆解析・直接解析しません。IFC、Revit API exporter/sidecar、Autodesk Platform Services、または任意の商用 SDK adapter を経由します。

## 0.5.0 の機能

- 0.2–0.4 の IFC/BIM セマンティクス、実 OpenCascade ジオメトリ、Picking / Hide / Isolate / Section Box / Batch / Camera 契約をすべて維持。
- `XbimIfcModelReader` は background task、CancellationToken、段階別 Progress を継続し、0.5 では cache check/read/write stage を追加。
- `CachedIfcModelReader` は任意の `IIfcModelReader` を包み、xBIM adapter を変更せずに cross-load cache を提供。
- bounded LRU memory cache は同一 `SceneDocument` と shared `MeshData` reference をそのまま再利用。
- optional `.svbim` disk cache は renderer-neutral `SceneDocument` を保存し、xBIM/OpenCascade の private object は保存しない。
- cache identity は source SHA-256、file length、open-option signature、cache format version を使用。source または geometry/property/opening/rebase option の変更で失効。
- `.svbim` は unique mesh table を使い、reader/process をまたいでも shared geometry、material slot、transform、bounds、world origin、BIM property、diagnostic を復元。
- cache が破損・読み取り不可・書き込み不可でも cold IFC load は継続。disk write は temporary file から atomic replace。
- `RenderSceneIndex` は scene を一度だけ pre-index し、以後の Hide/Isolate、opacity、Section Box rebuild で `SceneDocument` を再帰走査しない。
- `IfcLoadBenchmark` は elapsed time と managed heap / process working set の start / peak / end sample、および Miss / MemoryHit / DiskHit を計測。
- `RenderPerformanceMetrics` は indexed scene rebuild の elapsed/allocation と、instance 数ではなく unique mesh 単位の GPU geometry upload estimate を提供。
- CI は dependency graph key に基づく NuGet/OpenCascade package cache を使用し、native geometry restore の反復コストを削減。

## キャッシュ例

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

`.svbim` は SpatialViewer 内部の versioned performance cache です。IFC の代替交換形式や長期保存形式ではありません。

詳細は [開発計画](docs/DEVELOPMENT_PLAN.md)、[互換性](docs/COMPATIBILITY.md)、[アーキテクチャ](docs/ARCHITECTURE.md) を参照してください。

MIT License。第三者コンポーネントは [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) に記載した各ライセンスに従います。0.5.0 では runtime third-party dependency を追加していません。
