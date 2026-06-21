using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FishFarm.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FishFarm.Infrastructure.Services;

public sealed class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(Cloudinary cloudinary, ILogger<CloudinaryService> logger)
    {
        _cloudinary = cloudinary;
        _logger     = logger;
    }

    public async Task<(string SecureUrl, string PublicId)> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File           = new FileDescription(file.FileName, stream),
            Folder         = folder,
            UseFilename    = false,
            UniqueFilename = true,
            Overwrite      = false,
            Transformation = new Transformation()
                .Quality("auto:good")
                .FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            _logger.LogError("Cloudinary upload failed: {Message}", result.Error.Message);
            throw new InvalidOperationException($"Image upload failed: {result.Error.Message}");
        }

        return (result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteImageAsync(string? publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;

        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Error is not null)
        {
            // Log but don't throw — deletion failure should not block the main operation
            _logger.LogWarning(
                "Cloudinary delete failed for publicId '{PublicId}': {Message}",
                publicId, result.Error.Message);
        }
    }
}
