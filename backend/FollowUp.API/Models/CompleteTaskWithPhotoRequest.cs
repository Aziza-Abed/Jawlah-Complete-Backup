using System.ComponentModel.DataAnnotations;

namespace FollowUp.API.Models;

// request model for completing a task with a photo upload
public class CompleteTaskWithPhotoRequest
{
    [StringLength(2000)]
    public string CompletionNotes { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public IFormFile? Photo { get; set; }
    // device time when the worker actually completed the task (before GPS/upload delay)
    public DateTime? EventTime { get; set; }
    // true when the worker's device could not obtain a GPS fix — flagged for supervisor awareness
    public bool GpsUnavailable { get; set; }
}
