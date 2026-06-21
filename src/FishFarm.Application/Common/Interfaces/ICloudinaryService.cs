using Microsoft.AspNetCore.Http;

namespace FishFarm.Application.Common.Interfaces;

/// <summary>
/// Abstraction over Cloudinary so the Application layer remains
/// infrastructure-agnostic and can be tested with fakes.
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Uploads an image stream to Cloudinary under the given folder.
    /// </summary>
    /// <param name="file">The multipart file from the HTTP request.</param>
    /// <param name="folder">Cloudinary folder path (e.g. "fishfarms", "workers").</param>
    /// <returns>Tuple of (SecureUrl, PublicId) for storage in the database.</returns>
    Task<(string SecureUrl, string PublicId)> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from Cloudinary by its public_id.
    /// Safe to call with null (no-op).
    /// </summary>
    Task DeleteImageAsync(string? publicId, CancellationToken cancellationToken = default);
}
