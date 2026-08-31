# 贡献指南

1. 从 `main` 创建短生命周期分支。
2. 保持 Core / Format Adapter / Rendering 的依赖边界，不得把 xBIM、Autodesk 或 WinUI 类型泄漏到 `SpatialViewer.Core`。
3. 新增 IFC 实体、几何或兼容性修复时必须补测试或 fixture 说明。
4. 提交前运行 `dotnet build SpatialViewer.IFCCore.sln -c Release` 与 `dotnet test SpatialViewer.IFCCore.sln -c Release`。
5. PR 说明需包含兼容性影响、性能影响和第三方许可变化。
