using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Zones;

public class ImportShapefileRequest
{
    [Required(ErrorMessage = "File path is required")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Municipality ID to associate the imported zones with.
    /// If not provided, defaults to 1 (Al-Bireh).
    /// </summary>
    public int MunicipalityId { get; set; } = 1;
}
