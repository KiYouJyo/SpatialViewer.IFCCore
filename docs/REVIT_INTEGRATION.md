# Revit Integration Boundary

`.rvt` is a proprietary Autodesk format. SpatialViewer.IFCCore therefore treats Revit as a **source ecosystem**, not as the portable file parser built into Core.

## Supported integration strategies

### A. Revit API exporter / sidecar — preferred local path

When Autodesk Revit is installed, a small exporter can open the RVT through the official Revit API and emit IFC or a SpatialViewer-neutral cache. This gives the best fidelity without contaminating the viewer kernel with Revit API types.

### B. Autodesk Platform Services — optional cloud path

Model Derivative or related Autodesk services can translate supported design files in the cloud. This is useful for server workflows but conflicts with a fully offline viewer and therefore must remain optional.

### C. Licensed direct-RVT SDK — optional commercial adapter

A separately licensed SDK may be used later for offline direct RVT reading. It must live in an isolated adapter package and may not be required by the MIT core build.

## Explicit non-goals

- Reverse-engineering the RVT binary format in this repository.
- Shipping Autodesk or commercial SDK binaries in source control.
- Making the Core API depend on a specific Revit release.
