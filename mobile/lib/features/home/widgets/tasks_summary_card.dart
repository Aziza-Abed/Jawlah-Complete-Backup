import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../providers/task_manager.dart';

class TasksSummaryCard extends StatelessWidget {
  const TasksSummaryCard({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<TaskManager>(
      builder: (context, tasksProvider, child) {
        final total = tasksProvider.totalTasks;
        final pending = tasksProvider.pendingCount;
        final inProgress = tasksProvider.inProgressCount;
        final completed = tasksProvider.completedCount;

        return Container(
          width: double.infinity,
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: AppColors.cardBackground,
            borderRadius: BorderRadius.circular(16),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.05),
                blurRadius: 10,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(
                    Icons.assignment,
                    size: 24,
                    color: AppColors.primary,
                  ),
                  SizedBox(width: 8),
                  Text(
                    'ملخص المهام',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                      fontFamily: 'Cairo',
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceAround,
                children: [
                  _buildTaskStat('الكُل', '$total', AppColors.textPrimary),
                  _buildTaskStat('جديد', '$pending', AppColors.info),
                  _buildTaskStat(
                      'قيد التنفيذ', '$inProgress', AppColors.statusInProgress),
                  _buildTaskStat('مكتمل', '$completed', AppColors.success),
                ],
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () {
                    HapticFeedback.mediumImpact();
                    Navigator.of(context).pushNamed(Routes.tasksList);
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    elevation: 0,
                  ),
                  child: const Text(
                    'عرض المهام',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      fontFamily: 'Cairo',
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildTaskStat(String label, String count, Color color) {
    return Column(
      children: [
        Text(
          count,
          style: TextStyle(
            fontSize: 24,
            fontWeight: FontWeight.bold,
            color: color,
            fontFamily: 'Cairo',
          ),
        ),
        const SizedBox(height: 4),
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            color: AppColors.textSecondary,
            fontFamily: 'Cairo',
          ),
        ),
      ],
    );
  }
}
