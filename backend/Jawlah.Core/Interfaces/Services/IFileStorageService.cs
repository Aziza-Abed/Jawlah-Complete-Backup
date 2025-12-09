using Microsoft.AspNetCore.Http;

namespace Jawlah.Core.Interfaces.Services;

/// <summary>
/// Service for handling file uploads and storage
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads an image file to local storage
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="folder">Folder name (tasks, issues, profiles)</param>
    /// <returns>The URL path to access the uploaded file</returns>
    Task<string> UploadImageAsync(IFormFile file, string folder);

    /// <summary>
    /// Deletes an image file from local storage
    /// </summary>
    /// <param name="fileUrl">The URL of the file to delete</param>
    Task DeleteImageAsync(string fileUrl);

    /// <summary>
    /// Validates if the file is a valid image
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateImage(IFormFile file);
}
