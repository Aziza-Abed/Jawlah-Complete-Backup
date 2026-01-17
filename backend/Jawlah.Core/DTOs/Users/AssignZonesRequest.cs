namespace Jawlah.Core.DTOs.Users;

public class AssignZonesRequest
{
    public IEnumerable<int> ZoneIds { get; set; } = new List<int>();
}
