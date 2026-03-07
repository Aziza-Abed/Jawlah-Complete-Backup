using FollowUp.Core.Constants;
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

            // strip EXIF/metadata from JPEG images (GPS coordinates, device info, timestamps)
            StripImageMetadata(filePath, extension);

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

            // make sure path stays inside storage dir
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

        // check file content with magic bytes
        if (!ValidateImageMagicBytes(file))
        {
            _logger.LogWarning("File failed magic bytes validation - potential malicious file");
            return false;
        }

        return true;
    }

    // validates file content by checking magic bytes to ensure it's a real image
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

    // strips EXIF metadata (GPS, device info, timestamps) from JPEG images for privacy
    // removes APP1-APP13 segments (EXIF, XMP, ICC, IPTC) and COM markers
    // preserves APP0 (JFIF) and all image data segments (SOF, DQT, DHT, SOS)
    private void StripImageMetadata(string filePath, string extension)
    {
        if (extension != ".jpg" && extension != ".jpeg")
            return; // only JPEG carries significant EXIF from phone cameras

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
                return; // not a valid JPEG

            using var output = new MemoryStream(bytes.Length);
            output.WriteByte(0xFF); // SOI
            output.WriteByte(0xD8);

            int pos = 2;
            while (pos + 1 < bytes.Length)
            {
                if (bytes[pos] != 0xFF)
                {
                    pos++;
                    continue;
                }

                byte marker = bytes[pos + 1];

                // SOS (Start of Scan) — copy rest of file (actual image data)
                if (marker == 0xDA)
                {
                    output.Write(bytes, pos, bytes.Length - pos);
                    break;
                }

                // EOI (End of Image)
                if (marker == 0xD9)
                {
                    output.WriteByte(0xFF);
                    output.WriteByte(0xD9);
                    break;
                }

                // Standalone markers (RST0-RST7, TEM) — no length field
                if ((marker >= 0xD0 && marker <= 0xD7) || marker == 0x01)
                {
                    output.WriteByte(0xFF);
                    output.WriteByte(marker);
                    pos += 2;
                    continue;
                }

                // All other markers have a 2-byte length field
                if (pos + 3 >= bytes.Length)
                    break;

                int segLen = (bytes[pos + 2] << 8) | bytes[pos + 3];

                // Strip APP1-APP13 (EXIF, XMP, ICC profile, IPTC metadata)
                if (marker >= 0xE1 && marker <= 0xED)
                {
                    pos += 2 + segLen;
                    continue;
                }

                // Strip COM (comments that may contain metadata)
                if (marker == 0xFE)
                {
                    pos += 2 + segLen;
                    continue;
                }

                // Keep everything else (APP0/JFIF, SOF, DQT, DHT, DRI, etc.)
                output.Write(bytes, pos, 2 + segLen);
                pos += 2 + segLen;
            }

            File.WriteAllBytes(filePath, output.ToArray());
            _logger.LogInformation("EXIF metadata stripped from {FileName}", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to strip EXIF metadata from {FileName} — file preserved as-is", Path.GetFileName(filePath));
        }
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
