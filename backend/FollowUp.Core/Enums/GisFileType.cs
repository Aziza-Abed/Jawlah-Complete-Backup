namespace FollowUp.Core.Enums;

/// <summary>
/// Types of GIS files that can be uploaded
/// </summary>
public enum GisFileType
{
    /// <summary>
    /// Quarters/Neighborhoods boundaries
    /// </summary>
    Quarters = 0,

    /// <summary>
    /// Municipality borders/boundaries
    /// </summary>
    Borders = 1,

    /// <summary>
    /// Blocks within quarters
    /// </summary>
    Blocks = 2
}
