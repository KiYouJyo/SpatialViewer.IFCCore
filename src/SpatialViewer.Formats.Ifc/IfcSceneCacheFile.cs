using System.Numerics;
using System.Runtime.CompilerServices;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Formats.Ifc;

internal sealed record IfcCachedEntry(
    SceneDocument Document,
    IfcSchemaVersion Schema,
    IReadOnlyList<IfcLoadDiagnostic> Diagnostics);

internal static class IfcSceneCacheFile
{
    private const string Magic = "SpatialViewer.IFCCore.SVBIM";
    private const int FormatVersion = 1;
    private const int MaximumCollectionCount = 50_000_000;

    public static Task WriteAsync(
        string path,
        string cacheKey,
        IfcCachedEntry entry,
        CancellationToken cancellationToken) =>
        Task.Run(() => Write(path, cacheKey, entry, cancellationToken), cancellationToken);

    public static Task<IfcCachedEntry?> TryReadAsync(
        string path,
        string expectedCacheKey,
        string currentSourcePath,
        CancellationToken cancellationToken) =>
        Task.Run(
            () => TryRead(path, expectedCacheKey, currentSourcePath, cancellationToken),
            cancellationToken);

    private static void Write(
        string path,
        string cacheKey,
        IfcCachedEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(entry);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 1024 * 1024,
                       options: FileOptions.SequentialScan))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(FormatVersion);
                writer.Write(cacheKey);
                writer.Write((int)entry.Schema);
                WriteDiagnostics(writer, entry.Diagnostics, cancellationToken);
                WriteDocument(writer, entry.Document, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static IfcCachedEntry? TryRead(
        string path,
        string expectedCacheKey,
        string currentSourcePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            options: FileOptions.SequentialScan);
        using var reader = new BinaryReader(stream);

        if (!string.Equals(reader.ReadString(), Magic, StringComparison.Ordinal))
        {
            return null;
        }

        if (reader.ReadInt32() != FormatVersion)
        {
            return null;
        }

        if (!string.Equals(reader.ReadString(), expectedCacheKey, StringComparison.Ordinal))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var schema = (IfcSchemaVersion)reader.ReadInt32();
        var diagnostics = ReadDiagnostics(reader, cancellationToken);
        var document = ReadDocument(reader, currentSourcePath, cancellationToken);
        return new IfcCachedEntry(document, schema, diagnostics);
    }

    private static void WriteDiagnostics(
        BinaryWriter writer,
        IReadOnlyList<IfcLoadDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        writer.Write(diagnostics.Count);
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write((int)diagnostic.Severity);
            writer.Write(diagnostic.Code);
            writer.Write(diagnostic.Message);
        }
    }

    private static List<IfcLoadDiagnostic> ReadDiagnostics(
        BinaryReader reader,
        CancellationToken cancellationToken)
    {
        var count = ReadCount(reader, "diagnostic");
        var diagnostics = new List<IfcLoadDiagnostic>(count);
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(new IfcLoadDiagnostic(
                (IfcDiagnosticSeverity)reader.ReadInt32(),
                reader.ReadString(),
                reader.ReadString()));
        }

        return diagnostics;
    }

    private static void WriteDocument(
        BinaryWriter writer,
        SceneDocument document,
        CancellationToken cancellationToken)
    {
        WriteNullableString(writer, document.SourcePath);
        WriteNullableBounds(writer, document.Bounds);
        WriteNullableBounds(writer, document.WorldBounds);
        WriteVector(writer, document.WorldOrigin);

        var metadata = document.Metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList();
        writer.Write(metadata.Count);
        foreach (var pair in metadata)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(pair.Key);
            writer.Write(pair.Value);
        }

        var meshes = new List<MeshData>();
        var meshIndices = new Dictionary<MeshData, int>(MeshReferenceComparer.Instance);
        CollectMeshes(document.Root, meshes, meshIndices, cancellationToken);
        writer.Write(meshes.Count);
        foreach (var mesh in meshes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteMesh(writer, mesh, cancellationToken);
        }

        WriteNode(writer, document.Root, meshIndices, cancellationToken);
    }

    private static SceneDocument ReadDocument(
        BinaryReader reader,
        string currentSourcePath,
        CancellationToken cancellationToken)
    {
        _ = ReadNullableString(reader);
        var bounds = ReadNullableBounds(reader);
        var worldBounds = ReadNullableBounds(reader);
        var worldOrigin = ReadVector(reader);

        var metadataCount = ReadCount(reader, "metadata");
        var metadata = new Dictionary<string, string>(metadataCount, StringComparer.Ordinal);
        for (var index = 0; index < metadataCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            metadata.Add(reader.ReadString(), reader.ReadString());
        }

        var meshCount = ReadCount(reader, "mesh");
        var meshes = new List<MeshData>(meshCount);
        for (var index = 0; index < meshCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            meshes.Add(ReadMesh(reader, cancellationToken));
        }

        var root = ReadNode(reader, meshes, cancellationToken);
        var fullSourcePath = Path.GetFullPath(currentSourcePath);
        var document = new SceneDocument(root)
        {
            SourcePath = fullSourcePath,
            Bounds = bounds,
            WorldBounds = worldBounds,
            WorldOrigin = worldOrigin,
        };

        foreach (var pair in metadata)
        {
            document.Metadata[pair.Key] = pair.Value;
        }

        document.Metadata["SourcePath"] = fullSourcePath;
        return document;
    }

    private static void CollectMeshes(
        SceneNode node,
        List<MeshData> meshes,
        Dictionary<MeshData, int> meshIndices,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var mesh in node.Meshes)
        {
            if (!meshIndices.ContainsKey(mesh))
            {
                meshIndices.Add(mesh, meshes.Count);
                meshes.Add(mesh);
            }
        }

        foreach (var child in node.Children)
        {
            CollectMeshes(child, meshes, meshIndices, cancellationToken);
        }
    }

    private static void WriteMesh(
        BinaryWriter writer,
        MeshData mesh,
        CancellationToken cancellationToken)
    {
        writer.Write(mesh.Positions.Count);
        foreach (var position in mesh.Positions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteVector(writer, position);
        }

        writer.Write(mesh.Indices.Count);
        foreach (var index in mesh.Indices)
        {
            writer.Write(index);
        }

        writer.Write(mesh.Normals is not null);
        if (mesh.Normals is not null)
        {
            writer.Write(mesh.Normals.Count);
            foreach (var normal in mesh.Normals)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteVector(writer, normal);
            }
        }

        WriteNullableString(writer, mesh.MaterialId);
    }

    private static MeshData ReadMesh(BinaryReader reader, CancellationToken cancellationToken)
    {
        var positionCount = ReadCount(reader, "position");
        if (positionCount == 0)
        {
            throw new InvalidDataException("A cached mesh cannot contain zero positions.");
        }

        var positions = new Vector3[positionCount];
        for (var index = 0; index < positionCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            positions[index] = ReadVector(reader);
        }

        var indexCount = ReadCount(reader, "triangle index");
        var indices = new int[indexCount];
        for (var index = 0; index < indexCount; index++)
        {
            indices[index] = reader.ReadInt32();
        }

        Vector3[]? normals = null;
        if (reader.ReadBoolean())
        {
            var normalCount = ReadCount(reader, "normal");
            normals = new Vector3[normalCount];
            for (var index = 0; index < normalCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                normals[index] = ReadVector(reader);
            }
        }

        return new MeshData(positions, indices)
        {
            Normals = normals,
            MaterialId = ReadNullableString(reader),
        };
    }

    private static void WriteNode(
        BinaryWriter writer,
        SceneNode node,
        Dictionary<MeshData, int> meshIndices,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        writer.Write(node.Id);
        WriteNullableString(writer, node.SourceId);
        WriteNullableString(writer, node.Name);
        WriteNullableString(writer, node.Category);
        WriteMatrix(writer, node.Transform);
        writer.Write(node.FlipWinding);
        WriteNullableBounds(writer, node.Bounds);

        var properties = node.Properties.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList();
        writer.Write(properties.Count);
        foreach (var pair in properties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(pair.Key);
            writer.Write(pair.Value.Name);
            WriteNullableString(writer, pair.Value.Value);
            WriteNullableString(writer, pair.Value.Unit);
            WriteNullableString(writer, pair.Value.Group);
        }

        writer.Write(node.Meshes.Count);
        foreach (var mesh in node.Meshes)
        {
            writer.Write(meshIndices[mesh]);
        }

        writer.Write(node.Children.Count);
        foreach (var child in node.Children)
        {
            WriteNode(writer, child, meshIndices, cancellationToken);
        }
    }

    private static SceneNode ReadNode(
        BinaryReader reader,
        List<MeshData> meshes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var node = new SceneNode(reader.ReadString())
        {
            SourceId = ReadNullableString(reader),
            Name = ReadNullableString(reader),
            Category = ReadNullableString(reader),
            Transform = ReadMatrix(reader),
            FlipWinding = reader.ReadBoolean(),
            Bounds = ReadNullableBounds(reader),
        };

        var propertyCount = ReadCount(reader, "node property");
        for (var index = 0; index < propertyCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = reader.ReadString();
            node.Properties.Add(key, new SceneProperty(
                reader.ReadString(),
                ReadNullableString(reader),
                ReadNullableString(reader),
                ReadNullableString(reader)));
        }

        var nodeMeshCount = ReadCount(reader, "node mesh reference");
        for (var index = 0; index < nodeMeshCount; index++)
        {
            var meshIndex = reader.ReadInt32();
            if ((uint)meshIndex >= (uint)meshes.Count)
            {
                throw new InvalidDataException("A cached node references an invalid mesh index.");
            }

            node.Meshes.Add(meshes[meshIndex]);
        }

        var childCount = ReadCount(reader, "child node");
        for (var index = 0; index < childCount; index++)
        {
            node.Children.Add(ReadNode(reader, meshes, cancellationToken));
        }

        return node;
    }

    private static int ReadCount(BinaryReader reader, string description)
    {
        var value = reader.ReadInt32();
        if (value < 0 || value > MaximumCollectionCount)
        {
            throw new InvalidDataException($"The cached {description} count is invalid: {value}.");
        }

        return value;
    }

    private static void WriteNullableString(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            writer.Write(value);
        }
    }

    private static string? ReadNullableString(BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadString() : null;

    private static void WriteVector(BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    private static Vector3 ReadVector(BinaryReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    private static void WriteNullableBounds(BinaryWriter writer, BoundingBox3? bounds)
    {
        writer.Write(bounds is not null);
        if (bounds is not null)
        {
            WriteVector(writer, bounds.Value.Min);
            WriteVector(writer, bounds.Value.Max);
        }
    }

    private static BoundingBox3? ReadNullableBounds(BinaryReader reader) =>
        reader.ReadBoolean() ? new BoundingBox3(ReadVector(reader), ReadVector(reader)) : null;

    private static void WriteMatrix(BinaryWriter writer, Matrix4x4 matrix)
    {
        writer.Write(matrix.M11); writer.Write(matrix.M12); writer.Write(matrix.M13); writer.Write(matrix.M14);
        writer.Write(matrix.M21); writer.Write(matrix.M22); writer.Write(matrix.M23); writer.Write(matrix.M24);
        writer.Write(matrix.M31); writer.Write(matrix.M32); writer.Write(matrix.M33); writer.Write(matrix.M34);
        writer.Write(matrix.M41); writer.Write(matrix.M42); writer.Write(matrix.M43); writer.Write(matrix.M44);
    }

    private static Matrix4x4 ReadMatrix(BinaryReader reader) =>
        new(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class MeshReferenceComparer : IEqualityComparer<MeshData>
    {
        public static MeshReferenceComparer Instance { get; } = new();

        public bool Equals(MeshData? x, MeshData? y) => ReferenceEquals(x, y);

        public int GetHashCode(MeshData obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
