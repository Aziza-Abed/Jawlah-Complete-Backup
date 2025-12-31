import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../providers/task_manager.dart';

class NewAssignmentsCard extends StatelessWidget {
  const NewAssignmentsCard({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<TaskManager>(
      builder: (context, tasksProvider, child) {
        final pendingCount = tasksProvider.pendingCount;

        // hide card if no pending tasks
        if (pendingCount == 0) {
          return const SizedBox.shrink();
        }

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
          child: Row(
            children: [
              Stack(
                children: [
                  Container(
                    width: 50,
                    height: 50,
                    decoration: BoxDecoration(
                      color: AppColors.warning.withOpacity(0.1),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.notifications,
                      size: 28,
                      color: AppColors.warning,
                    ),
                  ),
                  Positioned(
                    top: 0,
                    right: 0,
                    child: Container(
                      width: 20,
                      height: 20,
                      decoration: BoxDecoration(
                        color: AppColors.error,
                        shape: BoxShape.circle,
                        border: Border.all(
                          color: AppColors.cardBackground,
                          width: 2,
                        ),
                      ),
                      child: Center(
                        child: Text(
                          '$pendingCount',
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                            fontFamily: 'Cairo',
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'لديك مهام جديدة',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                        fontFamily: 'Cairo',
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'تم تعيين $pendingCount ${pendingCount == 1 ? 'مهمة' : 'مهام'} جديدة لك',
                      style: const TextStyle(
                        fontSize: 14,
                        color: AppColors.textSecondary,
                        fontFamily: 'Cairo',
                      ),
                    ),
                  ],
                ),
              ),
              const Icon(
                Icons.arrow_back_ios,
                size: 20,
                color: AppColors.textSecondary,
              ),
            ],
          ),
        );
      },
    );
  }
}
