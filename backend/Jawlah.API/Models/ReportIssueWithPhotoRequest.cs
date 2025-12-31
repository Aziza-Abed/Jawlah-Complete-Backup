namespace Jawlah.API.Models;

// request model for reporting an issue with a photo upload
// title and Severity are optional - defaults will be provided if not specified
public class ReportIssueWithPhotoRequest
{
    public string? Title { get; set; }  // Optional - will be generated from Type if not provided
    public string? Description { get; set; }  // Optional - notes are optional
    public string Type { get; set; } = string.Empty;
    public string? Severity { get; set; }  // Optional - defaults to "Medium"
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public IFormFile? Photo { get; set; }  // Legacy support - for backward compatibility
    public IFormFile? Photo1 { get; set; }  // Mandatory
    public IFormFile? Photo2 { get; set; }  // Optional
    public IFormFile? Photo3 { get; set; }  // Optional
}
