namespace FollowUp.Core.DTOs.Zones;

// request to import zones from a shapefile
public class ImportShapefileRequest
{
    // path to the shapefile on the server
    public string FilePath { get; set; } = string.Empty;

    // municipality ID to assign imported zones to
    public int MunicipalityId { get; set; }
}
