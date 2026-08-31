using System.Security.Cryptography;

namespace SpatialViewer.Formats.Ifc;

public sealed record IfcSourceFingerprint(
    long Length,
    DateTime LastWriteTimeUtc,
    string Sha256)
{
    public static async ValueTask<IfcSourceFingerprint> CreateAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var before = new FileInfo(path);
        if (!before.Exists)
        {
            throw new FileNotFoundException("The IFC source file was not found.", path);
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var algorithm = SHA256.Create();
        var hash = await algorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

        var after = new FileInfo(path);
        if (before.Length != after.Length || before.LastWriteTimeUtc != after.LastWriteTimeUtc)
        {
            throw new IOException("The IFC source file changed while its cache fingerprint was being computed.");
        }

        return new IfcSourceFingerprint(
            after.Length,
            after.LastWriteTimeUtc,
            Convert.ToHexString(hash));
    }
}
