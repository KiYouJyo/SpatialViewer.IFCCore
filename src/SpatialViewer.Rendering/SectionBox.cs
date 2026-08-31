using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering;

public sealed record SectionBox(BoundingBox3 Bounds, bool Enabled = true);
