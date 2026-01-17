namespace Jawlah.Core.DTOs.Issues;

/// <summary>
/// Request to update forwarding notes for an issue
/// </summary>
public class UpdateForwardingNotesRequest
{
    /// <summary>
    /// Notes about where the issue was forwarded (e.g., "تم إرساله لقسم الكهرباء")
    /// </summary>
    public string? Notes { get; set; }
}
