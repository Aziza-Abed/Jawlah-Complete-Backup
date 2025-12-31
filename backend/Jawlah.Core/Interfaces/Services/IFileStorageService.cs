using Microsoft.AspNetCore.Http;

namespace Jawlah.Core.Interfaces.Services;

// file upload and storage service
public interface IFileStorageService
{
    // upload image to storage
    Task<string> UploadImageAsync(IFormFile file, string folder, int? userId = null, string? entityType = null, int? entityId = null);

    // delete single image
    Task DeleteImageAsync(string fileUrl);

    // delete multiple images with error handling
    Task DeleteImagesAsync(IEnumerable<string> fileUrls);

    // validate image file
    bool ValidateImage(IFormFile file);
}
