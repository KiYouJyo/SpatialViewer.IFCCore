# SpatialViewer.IFCCore

[中文](README.md) | [English](README.en.md)

SpatialViewer.IFCCore は SpatialViewer の BIM / IFC 読み込みコアです。IFC の解析、BIM 階層・属性、ジオメトリ変換、レンダリング用シーン生成、および Revit 由来モデルの接続境界を担当し、WinUI 3 の製品 UI は含みません。

## 対象

- 主入力：IFC STEP (`.ifc`)。IFCZIP / IFCXML は後続対応。
- 対象 schema：IFC2x3、IFC4、IFC4.3。
- .NET 側の第一候補：xBIM。`SpatialViewer.Formats.Ifc.Xbim` に隔離します。
- `.rvt` はポータブルなコアで直接解析しません。Revit API エクスポータ、Autodesk Platform Services、または任意の商用 SDK アダプタを経由します。
- 出力は UI / レンダラ非依存の `SceneDocument`、BIM セマンティクス、メッシュデータです。

詳細は [開発計画](docs/DEVELOPMENT_PLAN.md) と [アーキテクチャ](docs/ARCHITECTURE.md) を参照してください。

MIT License。
