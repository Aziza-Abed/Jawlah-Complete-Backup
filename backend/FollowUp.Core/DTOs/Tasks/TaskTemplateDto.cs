namespace FollowUp.Core.DTOs.Tasks;

public class TaskTemplateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MunicipalityId { get; set; }
    public int? ZoneId { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty; // HH:mm
    public bool IsActive { get; set; }
    public string ZoneName { get; set; } = string.Empty; // For convenience
}
