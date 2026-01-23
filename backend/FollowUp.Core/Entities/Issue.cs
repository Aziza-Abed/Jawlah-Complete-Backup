using FollowUp.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.Entities;

public class Issue
{
    public int IssueId { get; set; }

    // Municipality that this issue belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    public int ReportedByUserId { get; set; }
    public int? ZoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueType Type { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueStatus Status { get; set; }

    // concurrency token for optimistic locking
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationDescription { get; set; }

    // legacy photo storage (semicolon-separated urls) - kept for backward compatibility
    // use Photos collection for new uploads
    public string? PhotoUrl { get; set; }

    public DateTime ReportedAt { get; set; }

    // modern photo storage - use this for new uploads
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime? SyncTime { get; set; }
    public bool IsSynced { get; set; }
    public int SyncVersion { get; set; }
    public User ReportedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
    public Zone? Zone { get; set; }

    // helper method to get all photos (both legacy PhotoUrl and Photos collection)
    public IEnumerable<string> GetAllPhotoUrls()
    {
        var urls = new List<string>();

        // add photos from Photos collection first (preferred)
        urls.AddRange(Photos.OrderBy(p => p.OrderIndex).Select(p => p.PhotoUrl));

        // fallback to legacy PhotoUrl if Photos collection is empty
        if (urls.Count == 0 && !string.IsNullOrEmpty(PhotoUrl))
        {
            urls.AddRange(PhotoUrl.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        return urls;
    }

    // helper method to migrate legacy PhotoUrl to Photos collection
    public void MigratePhotosToCollection(int uploadedByUserId)
    {
        if (string.IsNullOrEmpty(PhotoUrl) || Photos.Any())
            return;

        var urls = PhotoUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < urls.Length; i++)
        {
            Photos.Add(new Photo
            {
                PhotoUrl = urls[i],
                EntityType = "Issue",
                EntityId = IssueId,
                OrderIndex = i,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = uploadedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
