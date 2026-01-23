using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FollowUp.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB max
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

    public async Task<string> UploadImageAsync(IFormFile file, string folder, int? userId = null, string? entityType = null, int? entityId = null)
    {
        try
        {
            // make sure its a valid image
            if (!ValidateImage(file))
            {
                throw new InvalidOperationException("Invalid image file");
            }

            // clean the folder name
            var safeFolder = SanitizeFolder(folder);

            // make folder if not exist
            var storageBasePath = Path.Combine(_environment.ContentRootPath, SecureStorageFolder, "uploads", safeFolder);
            Directory.CreateDirectory(storageBasePath);

            // keep original extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(storageBasePath, uniqueFileName);

            // save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File uploaded: {FileName}", uniqueFileName);

            // return the url
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

    public async Task DeleteImagesAsync(IEnumerable<string> fileUrls)
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

    public Task<bool> DeleteImageAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return Task.FromResult(false);

            // block path traversal hacking attempts
            if (fileUrl.Contains("..") || fileUrl.Contains("~") ||
                fileUrl.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new InvalidOperationException("Invalid file path");

            // handle full urls or just paths
            string relativePath;
            if (fileUrl.StartsWith("http://") || fileUrl.StartsWith("https://"))
            {
                // get path from full url
                var uri = new Uri(fileUrl);
                var pathParts = uri.AbsolutePath.TrimStart('/').Split('/');

                // check if new format or old format
                if (pathParts.Length >= 3 && pathParts[0] == "api" && pathParts[1] == "files")
                {
                    // new format
                    relativePath = string.Join("/", pathParts.Skip(2));
                }
                else if (pathParts.Length >= 2 && pathParts[0] == "uploads")
                {
                    // old format
                    relativePath = string.Join("/", pathParts.Skip(1));
                }
                else
                {
                    relativePath = uri.AbsolutePath.TrimStart('/');
                }
            }
            else
            {
                // its alredy relative
                relativePath = fileUrl.TrimStart('/');
            }

            var storageRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, SecureStorageFolder, "uploads"));
            var filePath = Path.GetFullPath(Path.Combine(storageRoot, relativePath));

            // SECURITY: Verify final path is within storage directory
            if (!filePath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Blocked path traversal attempt in delete: {FileUrl}", fileUrl);
                return Task.FromResult(false);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Image deleted: {FileUrl}", fileUrl);
                return Task.FromResult(true);
            }
            else
            {
                _logger.LogWarning("Image not found for deletion: {FileUrl}", fileUrl);
                return Task.FromResult(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {FileUrl}", fileUrl);
            return Task.FromResult(false);
        }
    }

    public bool ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File is null or empty");
            return false;
        }

        // file too big
        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("File size {Size} exceeds maximum {MaxSize}", file.Length, MaxFileSize);
            return false;
        }

        // wrong extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("File extension {Extension} not allowed", extension);
            return false;
        }

        // not an image content type
        if (!file.ContentType.StartsWith("image/"))
        {
            _logger.LogWarning("File content type {ContentType} is not an image", file.ContentType);
            return false;
        }

        // SECURITY: Validate actual file content via magic bytes
        if (!ValidateImageMagicBytes(file))
        {
            _logger.LogWarning("File failed magic bytes validation - potential malicious file");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates file content by checking magic bytes to ensure it's a real image
    /// </summary>
    private bool ValidateImageMagicBytes(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var header = new byte[8];
            var bytesRead = stream.Read(header, 0, 8);

            if (bytesRead < 4)
                return false;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                return true;

            // GIF: 47 49 46 38
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                return true;

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating file magic bytes");
            return false;
        }
    }

    private string GetBaseUrl()
    {
        // try from http request first
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }

        // try from config
        var configuredBaseUrl = _configuration["AppSettings:BaseUrl"];
        if (!string.IsNullOrEmpty(configuredBaseUrl))
        {
            return configuredBaseUrl;
        }

        // just use localhost
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
