import 'package:flutter/material.dart';
import '../../data/services/sync/sync_service.dart';
import '../theme/app_colors.dart';

// Shows a toast message with sync result feedback
void showSyncResultToast(BuildContext context, SyncResult result) {
  String message;
  Color bgColor;

  if (result.totalFailed > 0) {
    message = 'تم رفع ${result.totalSynced}، فشل ${result.totalFailed}';
    bgColor = AppColors.warning;
  } else if (result.success) {
    message = result.totalSynced > 0
        ? 'تمت المزامنة (${result.totalSynced})'
        : 'لا توجد عناصر للمزامنة';
    bgColor = AppColors.success;
  } else {
    message = 'فشلت المزامنة';
    bgColor = AppColors.error;
  }

  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(message, style: const TextStyle(fontFamily: 'Cairo')),
      backgroundColor: bgColor,
    ),
  );
}
