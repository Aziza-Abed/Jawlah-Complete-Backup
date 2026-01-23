using FollowUp.Core.Enums;

namespace FollowUp.Core.Entities;

/// <summary>
/// Represents a GIS file uploaded by admin for geographic data.
/// Files are stored in Storage/GIS folder and tracked here.
/// </summary>
public class GisFile
{
    public int GisFileId { get; set; }

    /// <summary>
    /// Type of GIS data: Quarters, Borders, Blocks
    /// </summary>
    public GisFileType FileType { get; set; }

    /// <summary>
    /// Original filename when uploaded
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored filename in Storage/GIS folder
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Whether this file is currently active (only one per type should be active)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of features/zones imported from this file
    /// </summary>
    public int FeaturesCount { get; set; }

    /// <summary>
    /// User who uploaded this file
    /// </summary>
    public int UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this file was last used to import zones
    /// </summary>
    public DateTime? LastImportedAt { get; set; }

    /// <summary>
    /// Notes about this file (optional)
    /// </summary>
    public string? Notes { get; set; }

    // Navigation property
    public User? UploadedBy { get; set; }
}
