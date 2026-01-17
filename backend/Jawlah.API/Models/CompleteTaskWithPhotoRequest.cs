namespace Jawlah.API.Models;

// request model for completing a task with a photo upload
public class CompleteTaskWithPhotoRequest
{
    public string CompletionNotes { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public IFormFile? Photo { get; set; }
}
