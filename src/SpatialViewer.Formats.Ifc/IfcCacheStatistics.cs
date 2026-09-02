namespace SpatialViewer.Formats.Ifc;

public sealed record IfcCacheStatistics(
    long MemoryHits,
    long DiskHits,
    long Misses,
    long DiskWrites);
