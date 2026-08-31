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

        var worldBounds = ToBounds(instances[0].BoundingBox, scale);
        for (var index = 1; index < instances.Count; index++)
        {
            worldBounds = worldBounds.Union(ToBounds(instances[index].BoundingBox, scale));
        }

        var origin = SelectWorldOrigin(worldBounds, options);
        document.WorldBounds = worldBounds;
        document.WorldOrigin = origin;
        document.Bounds = worldBounds.Translate(-origin);

        var nodeByLabel = new Dictionary<int, SceneNode>();
        IndexSemanticNodes(document.Root, nodeByLabel);
        var meshCache = new Dictionary<(int Geometry, int Style), MeshData>();
        var instanceCount = 0;
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

                var matrix = ToMatrix(transformation, scale, origin);
                var geometryNode = new SceneNode(
                    $"ifc-geometry:{instance.IfcProductLabel.ToString(CultureInfo.InvariantCulture)}:{instance.InstanceLabel.ToString(CultureInfo.InvariantCulture)}")
                {
                    SourceId = productNode.SourceId,
                    Name = productNode.Name,
                    Category = "IFC.Geometry",
                    Transform = matrix,
                    FlipWinding = matrix.GetDeterminant() < 0f,
                    Bounds = ToBounds(instance.BoundingBox, scale).Translate(-origin),
                };
                geometryNode.Meshes.Add(mesh);
                geometryNode.Properties["IFC.Geometry.ShapeGeometryLabel"] = new SceneProperty(
                    "ShapeGeometryLabel",
                    instance.ShapeGeometryLabel.ToString(CultureInfo.InvariantCulture),
                    null,
                    "IFC.Geometry");
                geometryNode.Properties["IFC.Geometry.StyleLabel"] = new SceneProperty(
                    "StyleLabel",
                    instance.StyleLabel.ToString(CultureInfo.InvariantCulture),
                    null,
                    "IFC.Geometry");
                productNode.Children.Add(geometryNode);
                productNode.Bounds = productNode.Bounds is { } existing
                    ? existing.Union(geometryNode.Bounds.Value)
                    : geometryNode.Bounds;
                instanceCount++;
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

        document.Metadata["Geometry.Unit"] = "metre";
        document.Metadata["Geometry.InstanceCount"] = instanceCount.ToString(CultureInfo.InvariantCulture);
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

    private static BoundingBox3 ToBounds(XbimRect3D bounds, double scale) =>
        new(
            new Vector3((float)(bounds.X * scale), (float)(bounds.Y * scale), (float)(bounds.Z * scale)),
            new Vector3(
                (float)((bounds.X + bounds.SizeX) * scale),
                (float)((bounds.Y + bounds.SizeY) * scale),
                (float)((bounds.Z + bounds.SizeZ) * scale)));

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
}
