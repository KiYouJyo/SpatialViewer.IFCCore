# Contributing

1. Branch from `main` with a short-lived topic branch.
2. Preserve the Core / format-adapter / rendering boundaries. xBIM, Autodesk and WinUI types must not leak into `SpatialViewer.Core`.
3. IFC entity, geometry and compatibility fixes require tests or documented fixtures.
4. Run Release build and tests before opening a PR.
5. PRs must describe compatibility, performance and third-party licensing impact.
