# SpatialViewer.IFCCore

[中文](README.md) | [English](README.en.md)

SpatialViewer.IFCCore は SpatialViewer の BIM / IFC 読み込みコアです。IFC 解析、BIM 階層・属性、実ジオメトリの三角形化、renderer-neutral な操作・描画セマンティクス、および Revit 由来モデルの接続境界を担当し、WinUI 3 の製品 UI は含みません。

## 対象

- 主入力：IFC STEP (`.ifc`) と IFCZIP (`.ifczip`)。IFCXML は後続対応です。
- Schema：IFC2x3、IFC4、IFC4.3。
- セマンティクス：`Xbim.Essentials 6.1.605`。
- ジオメトリ：`Xbim.Geometry 6.3.891-netcore` とその OpenCascade ランタイム。
- xBIM / OpenCascade 型は `SpatialViewer.Formats.Ifc.Xbim` 内に隔離し、Core や UI 契約には公開しません。
- `.rvt` を portable Core で逆解析・直接解析しません。IFC、Revit API exporter/sidecar、Autodesk Platform Services、または任意の商用 SDK adapter を経由します。

## 0.4.0 の機能

- 0.2/0.3 の IFC セマンティクス、Property、実三角形ジオメトリ機能を維持。
- 読み込み済み `SceneDocument` から IFC の再解析なしで `RenderScene` を生成。
- Stable ObjectId と deterministic `uint PickId`。Hide/Isolate などの表示変更でも未変更オブジェクトの PickId は維持。
- PickMap から名前、カテゴリ、階、`SceneProperty` snapshot を直接取得可能。
- Object / Category / Storey の Hide と Object Isolate。
- Global / Category / Object の opacity override と renderer-neutral fallback material key。
- Section Box contract：完全に box 外の object は粗い bounds culling、交差 object は backend/GPU の精密 clipping 用に保持。
- Object-ID/depth outline と選択ハイライト用の per-object outline target。
- Shared Mesh + Material + Opacity + Winding を単位とした instanced `RenderBatch`。
- Perspective / Orthographic、Orbit、Pan、Zoom、View/Projection matrix を持つ platform-neutral `RenderCamera`。

詳細は [開発計画](docs/DEVELOPMENT_PLAN.md)、[互換性](docs/COMPATIBILITY.md)、[アーキテクチャ](docs/ARCHITECTURE.md) を参照してください。

MIT License。第三者コンポーネントは [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) に記載した各ライセンスに従います。
