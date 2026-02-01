import 'dart:async';
import '../data/models/notification_model.dart';
import '../data/services/notifications_service.dart';
import '../data/services/firebase_messaging_service.dart';
import 'base_controller.dart';

// this class handles all the notifications stuff
class NoticeManager extends BaseController {
  final NotificationsService _service = NotificationsService();
  final FirebaseMessagingService _fcmService = FirebaseMessagingService();
  StreamSubscription? _fcmSubscription;

  List<NotificationModel> myNotices = [];
  int newNoticesCount = 0;

  NoticeManager() {
    _fcmSubscription = _fcmService.onMessage.listen((_) {
      loadNotices();
    });
  }

  @override
  void dispose() {
    _fcmSubscription?.cancel();
    super.dispose();
  }

  // get all notifcations from server
  Future<void> loadNotices() async {
    await executeVoidWithErrorHandling(() async {
      myNotices = await _service.getMyNotifications();
      newNoticesCount = await _service.getUnreadCount();
    });
  }

  Future<void> seenNotice(int noticeId) async {
    final index = myNotices.indexWhere((n) => n.notificationId == noticeId);
    if (index != -1 && !myNotices[index].isRead) {
      final success = await _service.markAsRead(noticeId);
      if (success) {
        myNotices[index].isRead = true;
        newNoticesCount = (newNoticesCount > 0) ? newNoticesCount - 1 : 0;
        notifyListeners();
      }
    }
  }

  Future<void> seenAllNotices() async {
    final success = await _service.markAllAsRead();
    if (success) {
      for (var n in myNotices) {
        n.isRead = true;
      }
      newNoticesCount = 0;
      notifyListeners();
    }
  }
}
