using FollowUp.Core.Enums;

namespace FollowUp.Core.Entities;

// GIS file uploaded by admin for geographic data, stored in Storage/GIS folder
public class GisFile
{
    public int GisFileId { get; set; }

    // type of GIS data: Quarters, Borders, Blocks
    public GisFileType FileType { get; set; }

    // original filename when uploaded
    public string OriginalFileName { get; set; } = string.Empty;

    // stored filename in Storage/GIS folder
    public string StoredFileName { get; set; } = string.Empty;

    // file size in bytes
    public long FileSize { get; set; }

    // whether this file is currently active (only one per type should be active)
    public bool IsActive { get; set; } = true;

    // number of features/zones imported from this file
    public int FeaturesCount { get; set; }

    // user who uploaded this file
    public int UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // when this file was last used to import zones
    public DateTime? LastImportedAt { get; set; }

    // notes about this file (optional)
    public string? Notes { get; set; }

    // Navigation property
    public User? UploadedBy { get; set; }
}
