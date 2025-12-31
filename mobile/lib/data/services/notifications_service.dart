import '../../core/config/api_config.dart';
import '../../core/errors/app_exception.dart';
import '../models/notification_model.dart';
import 'api_service.dart';

class NotificationsService {
  final ApiService _apiService = ApiService();

  // get all user notifications from the server
  Future<List<NotificationModel>> getMyNotifications() async {
    try {
      // 1. call the api
      final response = await _apiService.get(ApiConfig.notifications);
      if (response.data['success'] == true) {
        // 2. convert the list from json to models
        final List<dynamic> data = response.data['data'] as List<dynamic>;
        return data.map((json) => NotificationModel.fromJson(json)).toList();
      }
      return [];
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل تحميل الإشعارات');
    }
  }

  // get unread notification count
  Future<int> getUnreadCount() async {
    try {
      final response = await _apiService.get(ApiConfig.unreadCount);
      if (response.data['success'] == true) {
        return response.data['data'] as int;
      }
      return 0;
    } catch (e) {
      if (e is AppException) rethrow;
      return 0;
    }
  }

  // mark notification as read
  Future<bool> markAsRead(int notificationId) async {
    try {
      final response = await _apiService.put(
        ApiConfig.getMarkAsReadUrl(notificationId),
      );
      return response.data['success'] == true;
    } catch (e) {
      if (e is AppException) rethrow;
      return false;
    }
  }

  // mark all notifications as read
  Future<bool> markAllAsRead() async {
    try {
      final response = await _apiService.put(ApiConfig.markAllAsRead);
      return response.data['success'] == true;
    } catch (e) {
      if (e is AppException) rethrow;
      return false;
    }
  }

  // delete notification
  Future<bool> deleteNotification(int notificationId) async {
    try {
      final response = await _apiService.delete(
        ApiConfig.getDeleteNotificationUrl(notificationId),
      );
      return response.data['success'] == true;
    } catch (e) {
      if (e is AppException) rethrow;
      return false;
    }
  }
}
