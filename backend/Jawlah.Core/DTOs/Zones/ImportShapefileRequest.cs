using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Zones;

public class ImportShapefileRequest
{
    [Required(ErrorMessage = "File path is required")]
    public string FilePath { get; set; } = string.Empty;
}
