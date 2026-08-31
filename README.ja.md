# SpatialViewer.IFCCore

[中文](README.md) | [English](README.en.md)

SpatialViewer.IFCCore は SpatialViewer の BIM / IFC 読み込みコアです。IFC 解析、BIM 階層・属性、実ジオメトリの三角形化、レンダリング用シーン生成、および Revit 由来モデルの接続境界を担当し、WinUI 3 の製品 UI は含みません。

## 対象

- 主入力：IFC STEP (`.ifc`) と IFCZIP (`.ifczip`)。IFCXML は後続対応です。
- Schema：IFC2x3、IFC4、IFC4.3。
- セマンティクス：`Xbim.Essentials 6.1.605`。
- ジオメトリ：`Xbim.Geometry 6.3.891-netcore` とその OpenCascade ランタイム。
- xBIM / OpenCascade 型は `SpatialViewer.Formats.Ifc.Xbim` 内に隔離し、Core や UI 契約には公開しません。
- `.rvt` をポータブル Core で逆解析・直接解析しません。IFC、Revit API exporter/sidecar、Autodesk Platform Services、または任意の商用 SDK adapter を経由します。

## 0.3.0 の機能

- IFC STEP / IFCZIP の実読み込みと IFC2x3 / IFC4 / IFC4.3 の判定。
- Project → Site → Building → Storey → Element 階層と BIM メタデータの抽出。
- `IncludeGeometry=true` で xBIM/OpenCascade による実際の三角形メッシュを生成。
- Position、Normal、Triangle Index、renderer-neutral な Style/Material slot を保持。
- メートル単位への正規化、元の world bounds 保持、大座標に対する local-origin rebasing。
- repeated / mapped geometry の `MeshData` 共有と instance transform の分離。
- mirrored / negative transform を `FlipWinding` として保持。
- Opening/Void を host geometry の boolean に反映し、必要時のみ opening geometry を保持可能。
- キャンセル、段階別進捗、構造化診断、読み込み時間を提供。

詳細は [開発計画](docs/DEVELOPMENT_PLAN.md)、[互換性](docs/COMPATIBILITY.md)、[アーキテクチャ](docs/ARCHITECTURE.md) を参照してください。

MIT License。第三者コンポーネントは [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) に記載した各ライセンスに従います。
