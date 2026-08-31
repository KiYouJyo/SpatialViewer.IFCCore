using System.Globalization;
using System.Numerics;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace SpatialViewer.Formats.Ifc.Xbim;

internal static class XbimGeometryExtractor
{
    public static void AttachGeometry(
        IfcStore model,
        SceneDocument document,
        IfcOpenOptions options,
        List<IfcLoadDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var scale = model.ModelFactors.LengthToMetresConversionFactor;
        if (!double.IsFinite(scale) || scale <= 0d)
        {
            scale = 1d;
            diagnostics.Add(new IfcLoadDiagnostic(
                IfcDiagnosticSeverity.Warning,
                "IFC_GEOMETRY_UNIT_FALLBACK",
                "The IFC model did not expose a valid length-to-metres factor; geometry uses a factor of 1."));
        }

        Report(options, IfcLoadStage.GeneratingGeometry, 0, "Generating xBIM geometry context");
        var context = new Xbim3DModelContext(model);
        var generated = context.CreateContext(
            (percent, state) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (percent is >= 0 and <= 100)
                {
                    Report(options, IfcLoadStage.GeneratingGeometry, percent, state?.ToString() ?? "Generating geometry");
                }
            },
            adjustWcs: false);

        if (!generated)
        {
            diagnostics.Add(new IfcLoadDiagnostic(
                IfcDiagnosticSeverity.Warning,
                "IFC_GEOMETRY_CONTEXT_EMPTY",
                "xBIM did not generate a geometry context for this model."));
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var reader = model.GeometryStore.BeginRead();
        var finalInstances = reader.ShapeInstances
            .Where(instance => instance.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded)
            .ToList();
        var instances = finalInstances.Count > 0 ? finalInstances : reader.ShapeInstances.ToList();
        if (instances.Count == 0)
        {
            diagnostics.Add(new IfcLoadDiagnostic(
                IfcDiagnosticSeverity.Warning,
                "IFC_GEOMETRY_EMPTY",
                "The geometry store contains no shape instances."));
            return;
        }

        var nodeByLabel = new Dictionary<int, SceneNode>();
        IndexSemanticNodes(document.Root, nodeByLabel);
        var meshCache = new Dictionary<(int Geometry, int Style), MeshData>();
        var pending = new List<PendingGeometry>();
        BoundingBox3? worldBounds = null;
        var triangleCount = 0;

        Report(options, IfcLoadStage.ExtractingGeometry, 0, "Extracting triangle meshes");
        for (var index = 0; index < instances.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var instance = instances[index];
            if (!nodeByLabel.TryGetValue(instance.IfcProductLabel, out var productNode))
            {
                continue;
            }

            try
            {
                var shapeGeometry = reader.ShapeGeometryOfInstance(instance);
                var cacheKey = (instance.ShapeGeometryLabel, instance.StyleLabel);
                if (!meshCache.TryGetValue(cacheKey, out var mesh))
                {
                    mesh = ReadMesh(shapeGeometry, scale, instance.StyleLabel);
                    meshCache.Add(cacheKey, mesh);
                    triangleCount += mesh.TriangleCount;
                }

                var transformation = instance.Transformation;
                if (shapeGeometry.LocalShapeDisplacement is { } displacement)
                {
                    transformation = XbimMatrix3D.CreateTranslation(displacement) * transformation;
                }

                var worldTransform = ToMatrix(transformation, scale, Vector3.Zero);
                var instanceWorldBounds = TransformBounds(mesh.Bounds, worldTransform);
                worldBounds = worldBounds is { } existing
                    ? existing.Union(instanceWorldBounds)
                    : instanceWorldBounds;
                pending.Add(new PendingGeometry(productNode, instance, mesh, worldTransform, instanceWorldBounds));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                diagnostics.Add(new IfcLoadDiagnostic(
                    IfcDiagnosticSeverity.Warning,
                    "IFC_GEOMETRY_SHAPE_FAILED",
                    $"Shape instance {instance.InstanceLabel.ToString(CultureInfo.InvariantCulture)} could not be extracted: {exception.Message}"));
            }

            Report(
                options,
                IfcLoadStage.ExtractingGeometry,
                GetPercent(index + 1, instances.Count),
                $"Extracting geometry {index + 1}/{instances.Count}");
        }

        if (pending.Count == 0 || worldBounds is null)
        {
            diagnostics.Add(new IfcLoadDiagnostic(
                IfcDiagnosticSeverity.Warning,
                "IFC_GEOMETRY_EMPTY",
                "No usable triangle meshes were extracted from the geometry store."));
            return;
        }

        var origin = SelectWorldOrigin(worldBounds.Value, options);
        document.WorldBounds = worldBounds;
        document.WorldOrigin = origin;
        document.Bounds = worldBounds.Value.Translate(-origin);

        foreach (var item in pending)
        {
            var rebasedTransform = ApplyWorldOrigin(item.WorldTransform, origin);
            var geometryNode = new SceneNode(
                $"ifc-geometry:{item.Instance.IfcProductLabel.ToString(CultureInfo.InvariantCulture)}:{item.Instance.InstanceLabel.ToString(CultureInfo.InvariantCulture)}")
            {
                SourceId = item.ProductNode.SourceId,
                Name = item.ProductNode.Name,
                Category = "IFC.Geometry",
                Transform = rebasedTransform,
                FlipWinding = rebasedTransform.GetDeterminant() < 0f,
                Bounds = item.WorldBounds.Translate(-origin),
            };
            geometryNode.Meshes.Add(item.Mesh);
            geometryNode.Properties["IFC.Geometry.ShapeGeometryLabel"] = new SceneProperty(
                "ShapeGeometryLabel",
                item.Instance.ShapeGeometryLabel.ToString(CultureInfo.InvariantCulture),
                null,
                "IFC.Geometry");
            geometryNode.Properties["IFC.Geometry.StyleLabel"] = new SceneProperty(
                "StyleLabel",
                item.Instance.StyleLabel.ToString(CultureInfo.InvariantCulture),
                null,
                "IFC.Geometry");
            item.ProductNode.Children.Add(geometryNode);
            item.ProductNode.Bounds = item.ProductNode.Bounds is { } existing
                ? existing.Union(geometryNode.Bounds.Value)
                : geometryNode.Bounds;
        }

        document.Metadata["Geometry.Unit"] = "metre";
        document.Metadata["Geometry.InstanceCount"] = pending.Count.ToString(CultureInfo.InvariantCulture);
        document.Metadata["Geometry.UniqueMeshCount"] = meshCache.Count.ToString(CultureInfo.InvariantCulture);
        document.Metadata["Geometry.TriangleCount"] = triangleCount.ToString(CultureInfo.InvariantCulture);
        document.Metadata["Geometry.WorldOrigin"] = FormatVector(origin);
        document.Root.Bounds = document.Bounds;
    }

    private static MeshData ReadMesh(XbimShapeGeometry shapeGeometry, double scale, int styleLabel)
    {
        var data = ((IXbimShapeGeometryData)shapeGeometry).ShapeData;
        if (data is null || data.Length == 0)
        {
            throw new InvalidDataException("xBIM returned empty shape data.");
        }

        using var stream = new MemoryStream(data, writable: false);
        using var binaryReader = new BinaryReader(stream);
        var triangulation = binaryReader.ReadShapeTriangulation();
        triangulation.ToPointsWithNormalsAndIndices(out var packedPositions, out var indices);
        if (packedPositions.Count == 0 || indices.Count == 0)
        {
            throw new InvalidDataException("xBIM returned an empty triangulation.");
        }

        var positions = new List<Vector3>(packedPositions.Count);
        var normals = new List<Vector3>(packedPositions.Count);
        foreach (var packed in packedPositions)
        {
            positions.Add(new Vector3(
                (float)(packed[0] * scale),
                (float)(packed[1] * scale),
                (float)(packed[2] * scale)));
            var normal = new Vector3(packed[3], packed[4], packed[5]);
            normals.Add(normal.LengthSquared() > 0f ? Vector3.Normalize(normal) : normal);
        }

        return new MeshData(positions, indices)
        {
            Normals = normals,
            MaterialId = styleLabel == 0 ? null : $"xbim-style:{styleLabel.ToString(CultureInfo.InvariantCulture)}",
        };
    }

    private static Matrix4x4 ToMatrix(XbimMatrix3D matrix, double scale, Vector3 origin) =>
        new(
            (float)matrix.M11, (float)matrix.M12, (float)matrix.M13, (float)matrix.M14,
            (float)matrix.M21, (float)matrix.M22, (float)matrix.M23, (float)matrix.M24,
            (float)matrix.M31, (float)matrix.M32, (float)matrix.M33, (float)matrix.M34,
            (float)(matrix.OffsetX * scale) - origin.X,
            (float)(matrix.OffsetY * scale) - origin.Y,
            (float)(matrix.OffsetZ * scale) - origin.Z,
            (float)matrix.M44);

    private static Matrix4x4 ApplyWorldOrigin(Matrix4x4 matrix, Vector3 origin)
    {
        matrix.M41 -= origin.X;
        matrix.M42 -= origin.Y;
        matrix.M43 -= origin.Z;
        return matrix;
    }

    private static BoundingBox3 TransformBounds(BoundingBox3 bounds, Matrix4x4 transform)
    {
        var min = bounds.Min;
        var max = bounds.Max;
        var corners = new Vector3[]
        {
            new(min.X, min.Y, min.Z),
            new(max.X, min.Y, min.Z),
            new(min.X, max.Y, min.Z),
            new(max.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z),
            new(max.X, min.Y, max.Z),
            new(min.X, max.Y, max.Z),
            new(max.X, max.Y, max.Z),
        };

        for (var index = 0; index < corners.Length; index++)
        {
            corners[index] = Vector3.Transform(corners[index], transform);
        }

        return BoundingBox3.FromPoints(corners);
    }

    private static Vector3 SelectWorldOrigin(BoundingBox3 bounds, IfcOpenOptions options)
    {
        if (!options.RebaseLargeCoordinates)
        {
            return Vector3.Zero;
        }

        var center = bounds.Center;
        var maximumAbsoluteCoordinate = Math.Max(Math.Abs(center.X), Math.Max(Math.Abs(center.Y), Math.Abs(center.Z)));
        return maximumAbsoluteCoordinate >= options.LargeCoordinateThresholdMetres ? center : Vector3.Zero;
    }

    private static void IndexSemanticNodes(SceneNode node, Dictionary<int, SceneNode> destination)
    {
        if (node.Id.StartsWith("ifc:", StringComparison.Ordinal) &&
            int.TryParse(node.Id.AsSpan(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var label))
        {
            destination[label] = node;
        }

        foreach (var child in node.Children)
        {
            IndexSemanticNodes(child, destination);
        }
    }

    private static string FormatVector(Vector3 value) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{value.X:R},{value.Y:R},{value.Z:R}");

    private static int GetPercent(int completed, int total) =>
        total <= 0 ? 100 : Math.Clamp((int)Math.Round(completed * 100d / total), 0, 100);

    private static void Report(IfcOpenOptions options, IfcLoadStage stage, int percent, string message) =>
        options.Progress?.Report(new IfcLoadProgress(stage, percent, message));

    private sealed record PendingGeometry(
        SceneNode ProductNode,
        XbimShapeInstance Instance,
        MeshData Mesh,
        Matrix4x4 WorldTransform,
        BoundingBox3 WorldBounds);
}
