import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

class PriorityBadge extends StatelessWidget {
  final String priority;

  const PriorityBadge({super.key, required this.priority});

  @override
  Widget build(BuildContext context) {
    final priorityData = _getPriorityData(priority);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: priorityData['color'],
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.flag,
            size: 14,
            color: Colors.white,
          ),
          const SizedBox(width: 4),
          Text(
            priorityData['text'],
            style: const TextStyle(
              color: Colors.white,
              fontSize: 13,
              fontWeight: FontWeight.w600,
              fontFamily: 'Cairo',
            ),
          ),
        ],
      ),
    );
  }

  Map<String, dynamic> _getPriorityData(String priority) {
    final priorityLower = priority.toLowerCase();

    // handle both arabic and english priority values
    if (priorityLower == 'low' || priorityLower == 'منخفض') {
      return {'text': 'منخفض', 'color': AppColors.priorityLow};
    } else if (priorityLower == 'medium' || priorityLower == 'متوسط') {
      return {'text': 'متوسط', 'color': AppColors.priorityMedium};
    } else if (priorityLower == 'high' || priorityLower == 'عالي') {
      return {'text': 'عالي', 'color': AppColors.priorityHigh};
    } else {
      return {'text': priority, 'color': AppColors.textSecondary};
    }
  }
}
