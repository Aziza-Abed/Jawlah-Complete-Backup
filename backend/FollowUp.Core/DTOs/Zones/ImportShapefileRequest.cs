using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Zones;

// request to import zones from a shapefile (admin-only endpoint)
public class ImportShapefileRequest
{
    // path to the shapefile on the server (must be within Storage/GIS directory)
    [Required(ErrorMessage = "File path is required")]
    [RegularExpression(@"^[a-zA-Z0-9_\-\\/\.]+$", ErrorMessage = "File path contains invalid characters")]
    public string FilePath { get; set; } = string.Empty;

    // municipality ID to assign imported zones to
    [Range(1, int.MaxValue, ErrorMessage = "Municipality ID must be a positive number")]
    public int MunicipalityId { get; set; }
}
