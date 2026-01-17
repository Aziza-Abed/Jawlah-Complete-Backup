import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

class StatusBadge extends StatelessWidget {
  final String status;

  const StatusBadge({super.key, required this.status});

  @override
  Widget build(BuildContext context) {
    final statusData = _getStatusData(status);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: statusData['color'],
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        statusData['text'],
        style: const TextStyle(
          color: Colors.white,
          fontSize: 13,
          fontWeight: FontWeight.w600,
          fontFamily: 'Cairo',
        ),
      ),
    );
  }

  Map<String, dynamic> _getStatusData(String status) {
    final statusLower = status.toLowerCase();

    // handle both arabic and english status values
    if (statusLower == 'pending' || statusLower == 'جديد') {
      return {'text': 'جديد', 'color': AppColors.statusNew};
    } else if (statusLower == 'inprogress' ||
        statusLower == 'in progress' ||
        statusLower == 'قيد التنفيذ') {
      return {'text': 'قيد التنفيذ', 'color': AppColors.statusInProgress};
    } else if (statusLower == 'completed' || statusLower == 'مكتمل') {
      return {'text': 'مكتمل', 'color': AppColors.success};
    } else if (statusLower == 'rejected' || statusLower == 'مرفوض') {
      return {'text': 'مرفوض', 'color': AppColors.error};
    } else {
      return {'text': status, 'color': AppColors.textSecondary};
    }
  }
}
