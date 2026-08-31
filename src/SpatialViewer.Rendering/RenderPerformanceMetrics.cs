using System.Diagnostics;
using System.Runtime.CompilerServices;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering;

public sealed record RenderSceneBuildMetrics(
    int Iterations,
    TimeSpan TotalElapsed,
    TimeSpan AverageElapsed,
    long AllocatedBytes);

public sealed record RenderUploadEstimate(
    int UniqueMeshCount,
    int InstanceCount,
    int TriangleCount,
    int MaterialCount,
    long VertexBytes,
    long IndexBytes)
{
    public long TotalGeometryBytes => checked(VertexBytes + IndexBytes);
}

public static class RenderPerformanceMetrics
{
    public static RenderSceneBuildMetrics MeasureRebuild(
        RenderSceneIndex index,
        RenderSceneOptions? options = null,
        int iterations = 10)
    {
        ArgumentNullException.ThrowIfNull(index);
        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        _ = index.Build(options);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            _ = index.Build(options);
        }

        stopwatch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return new RenderSceneBuildMetrics(
            iterations,
            stopwatch.Elapsed,
            TimeSpan.FromTicks(stopwatch.Elapsed.Ticks / iterations),
            allocated);
    }

    public static RenderUploadEstimate EstimateGpuUpload(RenderScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var meshes = new HashSet<MeshData>(MeshReferenceComparer.Instance);
        long vertexBytes = 0;
        long indexBytes = 0;
        var triangleCount = 0;

        foreach (var renderMesh in scene.Meshes)
        {
            if (!meshes.Add(renderMesh.Mesh))
            {
                continue;
            }

            var mesh = renderMesh.Mesh;
            vertexBytes = checked(vertexBytes + ((long)mesh.Positions.Count * 3L * sizeof(float)));
            if (mesh.Normals is not null)
            {
                vertexBytes = checked(vertexBytes + ((long)mesh.Normals.Count * 3L * sizeof(float)));
            }

            indexBytes = checked(indexBytes + ((long)mesh.Indices.Count * sizeof(int)));
            triangleCount = checked(triangleCount + mesh.TriangleCount);
        }

        var materialCount = scene.Meshes
            .Select(mesh => mesh.MaterialId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        return new RenderUploadEstimate(
            meshes.Count,
            scene.Meshes.Count,
            triangleCount,
            materialCount,
            vertexBytes,
            indexBytes);
    }

    private sealed class MeshReferenceComparer : IEqualityComparer<MeshData>
    {
        public static MeshReferenceComparer Instance { get; } = new();

        public bool Equals(MeshData? x, MeshData? y) => ReferenceEquals(x, y);

        public int GetHashCode(MeshData obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
