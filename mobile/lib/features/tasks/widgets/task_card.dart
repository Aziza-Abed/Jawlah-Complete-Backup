import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../data/models/task_model.dart';

class TaskCard extends StatelessWidget {
  final TaskModel task;

  const TaskCard({super.key, required this.task});

  // get colors for priority badge
  Map<String, Color> _getPriorityStyle() {
    switch (task.priority.toLowerCase()) {
      case 'urgent':
      case 'عاجلة':
        return {
          'bg': AppColors.error.withOpacity(0.15),
          'fg': AppColors.error,
        };
      case 'high':
      case 'عالية':
        return {
          'bg': AppColors.priorityHigh.withOpacity(0.12),
          'fg': AppColors.priorityHigh,
        };
      case 'medium':
      case 'متوسطة':
        return {
          'bg': AppColors.accentOrange.withOpacity(0.08),
          'fg': AppColors.accentOrange,
        };
      default: // low
        return {
          'bg': AppColors.primaryBlue.withOpacity(0.1),
          'fg': AppColors.primaryBlue,
        };
    }
  }

  // get color for status badge — each status is visually distinct
  Color _getStatusColor() {
    switch (task.status.toLowerCase()) {
      case 'inprogress':
        return AppColors.statusInProgress;
      case 'completed':
        return AppColors.statusCompleted;
      case 'rejected':
        return AppColors.statusRejected;
      case 'underreview':
        return AppColors.statusUnderReview;
      case 'cancelled':
        return AppColors.textSecondary;
      default: // pending
        return AppColors.statusNew;
    }
  }

  @override
  Widget build(BuildContext context) {
    final priorityStyle = _getPriorityStyle();
    final statusColor = _getStatusColor();
    final formattedDate = task.dueDate != null
        ? DateFormat('yyyy-MM-dd').format(task.dueDate!.toLocal())
        : 'بدون تاريخ';

    return GestureDetector(
      onTap: () {
        HapticFeedback.lightImpact();
        Navigator.of(context)
            .pushNamed(Routes.taskDetails, arguments: task.taskId);
      },
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: AppColors.cardBackground,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.03),
              blurRadius: 10,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Row(
          children: [
            // Card content
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title
                  Text(
                        task.title,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: AppColors.mainText,
                          fontFamily: 'Cairo',
                        ),
                        textAlign: TextAlign.right,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),

                      const SizedBox(height: 8),

                      // priority and status badges
                      Row(
                        mainAxisAlignment:
                            MainAxisAlignment.start, // Align to Right in RTL
                        children: [
                          // priority badge
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 10, vertical: 4),
                            decoration: BoxDecoration(
                              color: priorityStyle['bg'],
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(Icons.flag_rounded,
                                    size: 14, color: priorityStyle['fg']),
                                const SizedBox(width: 6),
                                Text(
                                  task.priorityArabic,
                                  style: TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.w600,
                                    color: priorityStyle['fg'],
                                    fontFamily: 'Cairo',
                                  ),
                                ),
                              ],
                            ),
                          ),

                          const SizedBox(width: 8),

                          // status badge
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 10, vertical: 4),
                            decoration: BoxDecoration(
                              color: statusColor.withOpacity(0.08),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Text(
                              task.statusArabic,
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                                color: statusColor,
                                fontFamily: 'Cairo',
                              ),
                            ),
                          ),
                        ],
                      ),

                      const SizedBox(height: 8),

                      // location and date row
                      Row(
                        children: [
                          const Icon(Icons.location_on_rounded,
                              size: 14, color: AppColors.primaryBlue),
                          const SizedBox(width: 4),
                          Flexible(
                            child: Text(
                              task.zoneName ?? task.location ?? 'غير محدد',
                              style: const TextStyle(
                                fontSize: 13,
                                color: AppColors.secondaryText,
                                fontFamily: 'Cairo',
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const SizedBox(width: 12),
                          const Icon(Icons.calendar_today_rounded,
                              size: 14, color: AppColors.primaryBlue),
                          const SizedBox(width: 4),
                          Text(
                            formattedDate,
                            style: const TextStyle(
                              fontSize: 13,
                              color: AppColors.secondaryText,
                              fontFamily: 'Cairo',
                            ),
                          ),
                        ],
                      ),

                      // progress bar (show only for InProgress tasks with progress > 0)
                      if (task.status.toLowerCase() == 'inprogress' &&
                          task.progressPercentage > 0) ...[
                        const SizedBox(height: 12),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(
                                  'التقدم: ${task.progressPercentage}%',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.primaryBlue,
                                    fontFamily: 'Cairo',
                                  ),
                                ),
                                if (task.progressPercentage == 100)
                                  const Icon(
                                    Icons.check_circle_rounded,
                                    size: 16,
                                    color: AppColors.success,
                                  ),
                              ],
                            ),
                            const SizedBox(height: 6),
                            ClipRRect(
                              borderRadius: BorderRadius.circular(4),
                              child: LinearProgressIndicator(
                                value: task.progressPercentage / 100,
                                backgroundColor:
                                    AppColors.primaryBlue.withOpacity(0.2),
                                valueColor: AlwaysStoppedAnimation<Color>(
                                  task.progressPercentage == 100
                                      ? AppColors.success
                                      : AppColors.primaryBlue,
                                ),
                                minHeight: 6,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ],
                  ),
                ),
            const SizedBox(width: 8),
            // Arrow indicator (left side in RTL)
            const Icon(
              Icons.arrow_forward_ios,
              size: 16,
              color: AppColors.secondaryText,
            ),
          ],
        ),
      ),
    );
  }
}

