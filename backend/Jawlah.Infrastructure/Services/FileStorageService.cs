using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Jawlah.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private const string SecureStorageFolder = "Storage";

    public FileStorageService(
        IWebHostEnvironment environment,
        ILogger<FileStorageService> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _environment = environment;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async System.Threading.Tasks.Task<string> UploadImageAsync(IFormFile file, string folder, int? userId = null, string? entityType = null, int? entityId = null)
    {
        try
        {
            // check if the file is a valid image
            if (!ValidateImage(file))
            {
                throw new InvalidOperationException("Invalid image file");
            }

            // sanitize the folder name
            var safeFolder = SanitizeFolder(folder);

            // create the directory if it doesn't exist
            var storageBasePath = Path.Combine(_environment.ContentRootPath, SecureStorageFolder, "uploads", safeFolder);
            Directory.CreateDirectory(storageBasePath);

            // give the file a unique name and save it
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(storageBasePath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // return the full URL of the uploaded image
            var baseUrl = GetBaseUrl();
            var fileUrl = $"{baseUrl}/api/files/{safeFolder}/{uniqueFileName}";

            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to folder {Folder}", folder);
            throw;
        }
    }

    public async System.Threading.Tasks.Task DeleteImagesAsync(IEnumerable<string> fileUrls)
    {
        foreach (var fileUrl in fileUrls)
        {
            try
            {
                await DeleteImageAsync(fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image during batch cleanup: {Url}", fileUrl);
            }
        }
    }

    public System.Threading.Tasks.Task DeleteImageAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return System.Threading.Tasks.Task.CompletedTask;

            // security check
            if (fileUrl.Contains(".."))
                throw new InvalidOperationException("Invalid file path");

            // handle both absolute URLs and relative paths
            string relativePath;
            if (fileUrl.StartsWith("http://") || fileUrl.StartsWith("https://"))
            {
                // extract path from absolute URL
                // example: "http://192.168.1.4:5000/api/files/tasks/xyz.jpg" -> "tasks/xyz.jpg"
                var uri = new Uri(fileUrl);
                var pathParts = uri.AbsolutePath.TrimStart('/').Split('/');

                // check if it's new format (/api/files/folder/filename) or old format (/uploads/folder/filename)
                if (pathParts.Length >= 3 && pathParts[0] == "api" && pathParts[1] == "files")
                {
                    // new format: /api/files/tasks/xyz.jpg -> tasks/xyz.jpg
                    relativePath = string.Join("/", pathParts.Skip(2));
                }
                else if (pathParts.Length >= 2 && pathParts[0] == "uploads")
                {
                    // old format: /uploads/tasks/xyz.jpg -> tasks/xyz.jpg
                    relativePath = string.Join("/", pathParts.Skip(1));
                }
                else
                {
                    relativePath = uri.AbsolutePath.TrimStart('/');
                }
            }
            else
            {
                // already a relative path
                relativePath = fileUrl.TrimStart('/');
            }

            var filePath = Path.Combine(_environment.ContentRootPath, SecureStorageFolder, "uploads", relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Image deleted: {FileUrl} (path: {FilePath})", fileUrl, filePath);
            }
            else
            {
                _logger.LogWarning("Image not found for deletion: {FileUrl} (path: {FilePath})", fileUrl, filePath);
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {FileUrl}", fileUrl);
            throw;
        }
    }

    public bool ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File is null or empty");
            return false;
        }

        // check file size
        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("File size {Size} exceeds maximum {MaxSize}", file.Length, MaxFileSize);
            return false;
        }

        // check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("File extension {Extension} not allowed", extension);
            return false;
        }

        // check content type
        if (!file.ContentType.StartsWith("image/"))
        {
            _logger.LogWarning("File content type {ContentType} is not an image", file.ContentType);
            return false;
        }

        return true;
    }

    private string GetBaseUrl()
    {
        // try to get from current HTTP request first
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }

        // fallback to configuration
        var configuredBaseUrl = _configuration["AppSettings:BaseUrl"];
        if (!string.IsNullOrEmpty(configuredBaseUrl))
        {
            return configuredBaseUrl;
        }

        // final fallback
        _logger.LogWarning("Base URL not configured, using localhost default");
        return "http://localhost:5000";
    }

    private static string SanitizeFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new InvalidOperationException("Folder name is required");

        var trimmed = folder.Trim();
        if (trimmed.Contains(".."))
            throw new InvalidOperationException("Folder name is invalid");

        var isValid = Regex.IsMatch(trimmed, "^[a-zA-Z0-9_-]+$");
        if (!isValid)
            throw new InvalidOperationException("Folder name contains invalid characters");

        return trimmed;
    }
}
