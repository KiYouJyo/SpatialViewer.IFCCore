# SpatialViewer.IFCCore

[中文](README.md) | [English](README.en.md)

SpatialViewer.IFCCore は SpatialViewer の BIM / IFC 読み込みコアです。IFC の解析、BIM 階層・属性、ジオメトリ変換、レンダリング用シーン生成、および Revit 由来モデルの接続境界を担当し、WinUI 3 の製品 UI は含みません。

## 対象

- 主入力：IFC STEP (`.ifc`) と IFCZIP (`.ifczip`)。IFCXML は後続対応です。
- 0.2.x の対応 schema：IFC2x3、IFC4、IFC4.3。
- IFC アダプタ：`Xbim.Essentials`。`SpatialViewer.Formats.Ifc.Xbim` に隔離しています。
- `.rvt` はポータブルなコアで直接解析しません。Revit API エクスポータ、Autodesk Platform Services、または任意の商用 SDK アダプタを経由します。
- 出力は UI / レンダラ非依存の `SceneDocument` と BIM セマンティクスです。

## 0.2.0 の機能

- IFC STEP / IFCZIP の実読み込みと schema 自動判定。
- Project → Site → Building → Storey → Element 階層の生成。
- GlobalId、エンティティ識別、Occurrence/Type、空間所属情報の抽出。
- Property Set、Quantity、Classification、基本 Material の抽出。
- キャンセル、段階別進捗、構造化診断、読み込み時間の提供。
- ジオメトリの三角形化は 0.3.x に明確に分離しています。

詳細は [開発計画](docs/DEVELOPMENT_PLAN.md) と [アーキテクチャ](docs/ARCHITECTURE.md) を参照してください。

MIT License。第三者コンポーネントはそれぞれのライセンスに従います。
