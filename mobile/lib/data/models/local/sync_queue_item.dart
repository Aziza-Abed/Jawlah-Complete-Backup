import 'package:hive/hive.dart';

part 'sync_queue_item.g.dart';

// Enum for sync status
enum SyncStatus {
  pending,
  syncing,
  failed,
  synced,
}

@HiveType(typeId: 10)
class SyncQueueItem extends HiveObject {
  @HiveField(0)
  String itemId; // Unique ID for the queue item

  @HiveField(1)
  String itemType; // 'task', 'issue', 'attendance'

  @HiveField(2)
  int? serverId; // Server ID if exists

  @HiveField(3)
  String status; // pending, syncing, failed, synced

  @HiveField(4)
  String? errorMessage; // Last error if failed

  @HiveField(5)
  int retryCount; // Number of retry attempts

  @HiveField(6)
  DateTime createdAt;

  @HiveField(7)
  DateTime? lastAttemptAt;

  @HiveField(8)
  String actionType; // 'create', 'update', 'complete'

  SyncQueueItem({
    required this.itemId,
    required this.itemType,
    this.serverId,
    this.status = 'pending',
    this.errorMessage,
    this.retryCount = 0,
    required this.createdAt,
    this.lastAttemptAt,
    required this.actionType,
  });

  // Check if item can be retried (max 5 retries)
  bool get canRetry => retryCount < 5;

  // Time until next retry using exponential backoff
  Duration get nextRetryDelay {
    // 30s, 1m, 2m, 4m, 8m
    final seconds = 30 * (1 << retryCount);
    return Duration(seconds: seconds.clamp(30, 480));
  }

  // Check if ready to retry
  bool get isReadyToRetry {
    if (status != 'failed' || !canRetry) return false;
    if (lastAttemptAt == null) return true;
    return DateTime.now().difference(lastAttemptAt!) > nextRetryDelay;
  }
}
