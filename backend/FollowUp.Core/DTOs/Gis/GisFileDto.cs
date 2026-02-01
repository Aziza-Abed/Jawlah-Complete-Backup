using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Gis;

public class GisFileDto
{
    public int GisFileId { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted => FormatFileSize(FileSize);
    public bool IsActive { get; set; }
    public int FeaturesCount { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? LastImportedAt { get; set; }
    public string? Notes { get; set; }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}

public class GisFileUploadRequest
{
    public GisFileType FileType { get; set; }
    public string? Notes { get; set; }
}

public class GisFilesStatusDto
{
    public GisFileDto? Quarters { get; set; }
    public GisFileDto? Borders { get; set; }
    public GisFileDto? Blocks { get; set; }
    public bool HasAllRequiredFiles => Quarters != null && Borders != null;
}
