import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../data/models/task_model.dart';

class TaskCard extends StatelessWidget {
  final TaskModel task;

  const TaskCard({super.key, required this.task});

  // Helper to get priority color style (bg, text/icon)
  Map<String, Color> _getPriorityStyle() {
    switch (task.priority.toLowerCase()) {
      case 'high':
      case 'عالية':
        return {
          'bg': const Color(0xFFC97A63).withOpacity(0.12),
          'fg': const Color(0xFFC97A63),
        };
      case 'medium':
      case 'متوسطة':
        return {
          'bg': const Color(0xFFC97A63).withOpacity(0.08),
          'fg': const Color(0xFFC97A63),
        };
      default: // low
        return {
          'bg': const Color(0xFF7895B2).withOpacity(0.1),
          'fg': const Color(0xFF7895B2),
        };
    }
  }

  @override
  Widget build(BuildContext context) {
    final priorityStyle = _getPriorityStyle();
    final formattedDate = task.dueDate != null
        ? DateFormat('yyyy-MM-dd').format(task.dueDate!)
        : '2025-12-21'; // Fallback example date

    return GestureDetector(
      onTap: () {
        HapticFeedback.lightImpact();
        Navigator.of(context)
            .pushNamed(Routes.taskDetails, arguments: task.taskId);
      },
      child: Container(
        margin: const EdgeInsets.only(bottom: 16),
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: const Color(0xFFF9F8F5), // card background
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
            // 1. Main Content (Right side in RTL)
            Expanded(
              child: Column(
                crossAxisAlignment:
                    CrossAxisAlignment.start, // Align to Right in RTL
                children: [
                  // Title
                  Text(
                    task.title,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF2F2F2F),
                      fontFamily: 'Cairo',
                    ),
                    textAlign: TextAlign.right,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),

                  const SizedBox(height: 12),

                  // Badges Row (Priority & Status)
                  Row(
                    mainAxisAlignment:
                        MainAxisAlignment.start, // Align to Right in RTL
                    children: [
                      // Priority Badge
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

                      // Status Badge
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF7895B2).withOpacity(0.08),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          task.statusArabic,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF7895B2),
                            fontFamily: 'Cairo',
                          ),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 16),

                  // Location Row
                  Row(
                    mainAxisAlignment: MainAxisAlignment.start,
                    children: [
                      const Icon(Icons.location_on_rounded,
                          size: 16, color: Color(0xFF7895B2)),
                      const SizedBox(width: 8),
                      Flexible(
                        child: Text(
                          task.location ?? 'المنطقة الرياضية',
                          style: const TextStyle(
                            fontSize: 14,
                            color: Color(0xFF6C757D),
                            fontFamily: 'Cairo',
                          ),
                          textAlign: TextAlign.right,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 8),

                  // Date Row
                  Row(
                    mainAxisAlignment: MainAxisAlignment.start,
                    children: [
                      const Icon(Icons.calendar_today_rounded,
                          size: 16, color: Color(0xFF7895B2)),
                      const SizedBox(width: 8),
                      Text(
                        formattedDate,
                        style: const TextStyle(
                          fontSize: 14,
                          color: Color(0xFF6C757D),
                          fontFamily: 'Cairo',
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),

            const SizedBox(width: 12),

            // 2. Left Chevron (Points Left in RTL to indicate "Forward" to details)
            const Icon(
              Icons.arrow_forward_ios,
              size: 20,
              color: Color(0xFF6C757D),
            ),
          ],
        ),
      ),
    );
  }
}
