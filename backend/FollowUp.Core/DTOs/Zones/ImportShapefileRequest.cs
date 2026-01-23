namespace FollowUp.Core.DTOs.Zones;

/// <summary>
/// Request to import zones from a shapefile
/// </summary>
public class ImportShapefileRequest
{
    /// <summary>
    /// Path to the shapefile on the server
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Municipality ID to assign imported zones to
    /// </summary>
    public int MunicipalityId { get; set; }
}
