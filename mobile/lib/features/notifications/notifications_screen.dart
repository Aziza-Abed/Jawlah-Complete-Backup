import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';
import '../../data/models/notification_model.dart';
import '../../providers/notice_manager.dart';
import '../../presentation/widgets/base_screen.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  @override
  void initState() {
    super.initState();
    // Refresh notifications when entering the screen
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NoticeManager>().loadNotices();
    });
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'الإشعارات',
      showBackButton: true,
      actions: [
        IconButton(
          icon: const Icon(Icons.done_all, color: Colors.white),
          tooltip: 'تحديد الكل كمقروء',
          onPressed: () => _showMarkAllReadDialog(context),
        ),
      ],
      body: Consumer<NoticeManager>(
        builder: (context, provider, child) {
          if (provider.isLoading && provider.myNotices.isEmpty) {
            return const Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            );
          }

          final notifications = provider.myNotices;
          if (notifications.isEmpty) {
            return _buildEmptyState();
          }

          return RefreshIndicator(
            onRefresh: provider.loadNotices,
            color: AppColors.primary,
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: notifications.length,
              itemBuilder: (context, index) {
                return _buildNotificationCard(
                    context, provider, notifications[index]);
              },
            ),
          );
        },
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(32),
            decoration: BoxDecoration(
              color: AppColors.primary.withOpacity(0.05),
              shape: BoxShape.circle,
            ),
            child: Icon(
              Icons.notifications_none_rounded,
              size: 80,
              color: AppColors.primary.withOpacity(0.3),
            ),
          ),
          const SizedBox(height: 24),
          const Text(
            'لا توجد تنبيهات جديدة',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 8),
          const Text(
            'عندما تتلقى إشعاراً جديداً، سيظهر هنا',
            style: TextStyle(
              fontSize: 14,
              color: AppColors.textSecondary,
              fontFamily: 'Cairo',
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNotificationCard(
    BuildContext context,
    NoticeManager provider,
    NotificationModel notification,
  ) {
    final bool isRead = notification.isRead;

    return InkWell(
      onTap: () {
        if (!isRead) {
          provider.seenNotice(notification.notificationId);
        }
        // Handle navigation based on type if needed
      },
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isRead
              ? AppColors.cardBackground
              : AppColors.primary.withOpacity(0.04),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isRead
                ? Colors.transparent
                : AppColors.primary.withOpacity(0.1),
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.03),
              blurRadius: 10,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 52,
              height: 52,
              decoration: BoxDecoration(
                color:
                    _getNotificationColor(notification.type).withOpacity(0.1),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(
                _getNotificationIcon(notification.type),
                color: _getNotificationColor(notification.type),
                size: 26,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          notification.title,
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight:
                                isRead ? FontWeight.w600 : FontWeight.bold,
                            color: AppColors.textPrimary,
                            fontFamily: 'Cairo',
                          ),
                        ),
                      ),
                      if (!isRead)
                        Container(
                          width: 8,
                          height: 8,
                          decoration: const BoxDecoration(
                            color: AppColors.primary,
                            shape: BoxShape.circle,
                          ),
                        ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    notification.message,
                    style: TextStyle(
                      fontSize: 14,
                      color: isRead
                          ? AppColors.textSecondary
                          : AppColors.textPrimary.withOpacity(0.8),
                      height: 1.5,
                      fontFamily: 'Cairo',
                    ),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Icon(
                        Icons.access_time_rounded,
                        size: 14,
                        color: AppColors.textSecondary.withOpacity(0.5),
                      ),
                      const SizedBox(width: 6),
                      Text(
                        _formatDate(notification.createdAt),
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary.withOpacity(0.7),
                          fontFamily: 'Cairo',
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showMarkAllReadDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('تمييز الكل كمقروء',
            style: TextStyle(fontFamily: 'Cairo')),
        content: const Text('هل أنت متأكد من تمييز جميع الإشعارات كمقروءة؟',
            style: TextStyle(fontFamily: 'Cairo')),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
          ),
          TextButton(
            onPressed: () {
              context.read<NoticeManager>().seenAllNotices();
              Navigator.pop(context);
            },
            child: const Text('تأكيد',
                style:
                    TextStyle(color: AppColors.primary, fontFamily: 'Cairo')),
          ),
        ],
      ),
    );
  }

  IconData _getNotificationIcon(String type) {
    final value = type.toLowerCase();
    if (value.contains('assign')) return Icons.assignment_turned_in_rounded;
    if (value.contains('update')) return Icons.edit_notifications_rounded;
    if (value.contains('complete')) return Icons.check_circle_rounded;
    if (value.contains('reject')) return Icons.error_rounded;
    return Icons.notifications_rounded;
  }

  Color _getNotificationColor(String type) {
    final value = type.toLowerCase();
    if (value.contains('assign')) return const Color(0xFF7895B2);
    if (value.contains('update')) return const Color(0xFFC97A63);
    if (value.contains('complete')) return const Color(0xFFA3B18A);
    if (value.contains('reject')) return AppColors.error;
    return AppColors.primary;
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final diff = now.difference(date);
    if (diff.inMinutes < 60) return '${diff.inMinutes} دقيقة';
    if (diff.inHours < 24) return '${diff.inHours} ساعة';
    return '${date.year}-${date.month}-${date.day}';
  }
}
