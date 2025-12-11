namespace Jawlah.Core.Interfaces.Services;

public interface ISyncService
{
    Task<object> ProcessBatchSyncAsync(int userId, string deviceId, List<object> changes);
    Task<object> GetServerChangesAsync(int userId, DateTime lastSyncTime);
    Task<bool> ResolveConflictAsync(string entityType, int entityId, object clientData, object serverData);
}
