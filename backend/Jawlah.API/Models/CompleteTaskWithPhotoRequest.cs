namespace Jawlah.API.Models;

/// <summary>
/// Request model for completing a task with a photo upload
/// </summary>
public class CompleteTaskWithPhotoRequest
{
    public string CompletionNotes { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public IFormFile? Photo { get; set; }
}
