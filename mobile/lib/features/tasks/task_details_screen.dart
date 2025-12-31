import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';

import '../../core/routing/app_router.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../presentation/widgets/authenticated_image.dart';
import '../../providers/task_manager.dart';
import '../../data/models/task_model.dart';
import 'widgets/task_details_card.dart';

// screens for viewing single task details and updating status
class TaskDetailsScreen extends StatefulWidget {
  const TaskDetailsScreen({super.key});

  @override
  State<TaskDetailsScreen> createState() => _TaskDetailsScreenState();
}

class _TaskDetailsScreenState extends State<TaskDetailsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final taskId = ModalRoute.of(context)?.settings.arguments as int?;
      if (taskId != null) {
        context.read<TaskManager>().getTaskDetails(taskId);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'تفاصيل المهمة',
      showBackButton: true,
      body: Consumer<TaskManager>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            );
          }

          final task = provider.currentTask;
          if (task == null) {
            return Center(
              child: Text(
                'لم يتم اختيار مهمة',
                style: const TextStyle(fontFamily: 'Cairo'),
              ),
            );
          }

          final statusLower = task.status.toLowerCase();

          return SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  task.title,
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                    fontFamily: 'Cairo',
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  task.description,
                  style: const TextStyle(
                    fontSize: 16,
                    color: AppColors.textSecondary,
                    height: 1.7,
                    fontFamily: 'Cairo',
                  ),
                ),
                const SizedBox(height: 24),
                TaskDetailsCard(task: task),
                const SizedBox(height: 32),

                // actions depending on status
                if (statusLower == 'pending') ...[
                  _buildPrimaryActionButton(
                    icon: Icons.play_arrow_rounded,
                    label: 'بدء المهمة الآن',
                    color: AppColors.primary,
                    onPressed: () => _handleStartTask(task.taskId),
                  ),
                ],

                if (statusLower == 'inprogress') ...[
                  _buildPrimaryActionButton(
                    icon: Icons.check_circle_rounded,
                    label: 'إكمال وإرسال الإثبات',
                    color: AppColors.success,
                    onPressed: () => Navigator.of(context)
                        .pushNamed(Routes.taskProof, arguments: task.taskId),
                  ),
                  const SizedBox(height: 12),
                  _buildSecondaryActionButton(
                    icon: Icons.pause_rounded,
                    label: 'توقف مؤقتاً (إعادة إلى جديد)',
                    onPressed: () =>
                        _handleStatusUpdate(task.taskId, 'pending'),
                  ),
                ],

                if (statusLower == 'completed') ...[
                  _buildCompletedSection(task),
                ],

                if (statusLower == 'approved') ...[
                  _buildApprovalStatus(true),
                ],

                if (statusLower == 'rejected') ...[
                  _buildApprovalStatus(false),
                  const SizedBox(height: 16),
                  _buildPrimaryActionButton(
                    icon: Icons.refresh_rounded,
                    label: 'إعادة المحاولة (بدء من جديد)',
                    color: AppColors.primary,
                    onPressed: () =>
                        _handleStatusUpdate(task.taskId, 'inprogress'),
                  ),
                ],
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildPrimaryActionButton({
    required IconData icon,
    required String label,
    required Color color,
    required VoidCallback onPressed,
  }) {
    return Container(
      width: double.infinity,
      height: 60,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: color.withOpacity(0.3),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: ElevatedButton.icon(
        onPressed: onPressed,
        icon: Icon(icon, size: 28),
        label: Text(
          label,
          style: const TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            fontFamily: 'Cairo',
          ),
        ),
        style: ElevatedButton.styleFrom(
          backgroundColor: color,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          elevation: 0,
        ),
      ),
    );
  }

  Widget _buildSecondaryActionButton({
    required IconData icon,
    required String label,
    required VoidCallback onPressed,
  }) {
    return SizedBox(
      width: double.infinity,
      child: TextButton.icon(
        onPressed: onPressed,
        icon: Icon(icon, size: 20, color: AppColors.textSecondary),
        label: Text(
          label,
          style: const TextStyle(
            fontSize: 15,
            color: AppColors.textSecondary,
            fontFamily: 'Cairo',
            decoration: TextDecoration.underline,
          ),
        ),
        style: TextButton.styleFrom(
          padding: const EdgeInsets.symmetric(vertical: 12),
        ),
      ),
    );
  }

  Widget _buildApprovalStatus(bool isApproved) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: isApproved
            ? AppColors.success.withOpacity(0.1)
            : AppColors.error.withOpacity(0.1),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isApproved
              ? AppColors.success.withOpacity(0.3)
              : AppColors.error.withOpacity(0.3),
        ),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            isApproved ? Icons.verified_rounded : Icons.report_problem_rounded,
            color: isApproved ? AppColors.success : AppColors.error,
          ),
          const SizedBox(width: 12),
          Text(
            isApproved ? 'تم اعتماد هذه المهمة' : 'تم رفض هذه المهمة',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: isApproved ? AppColors.success : AppColors.error,
              fontFamily: 'Cairo',
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _handleStartTask(int taskId) async {
    final success = await context.read<TaskManager>().startWorkOnTask(taskId);
    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('تم بدء المهمة، بالتوفيق!',
              style: TextStyle(fontFamily: 'Cairo')),
          backgroundColor: AppColors.primary,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  Widget _buildCompletedSection(TaskModel task) {
    final String imageUrl =
        task.photoUrl ?? (task.photos.isNotEmpty ? task.photos.first : '');

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: const Color(0xFFF9F8F5), // card background
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              const Text(
                'إثبات الإنجاز',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF2F2F2F),
                  fontFamily: 'Cairo',
                ),
              ),
              const SizedBox(width: 8),
              const Icon(Icons.verified, color: Color(0xFFA3B18A), size: 20),
            ],
          ),
          const SizedBox(height: 16),
          if (imageUrl.isNotEmpty) ...[
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: AuthenticatedImage(
                imageUrl: imageUrl,
                width: double.infinity,
                height: 240,
                fit: BoxFit.cover,
              ),
            ),
            const SizedBox(height: 16),
          ],
          if (task.completionNotes != null &&
              task.completionNotes!.isNotEmpty) ...[
            const Divider(height: 32),
            const Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                Text(
                  'ملاحظات الإنجاز',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                    color: Color(0xFF6C757D),
                    fontFamily: 'Cairo',
                  ),
                ),
                SizedBox(width: 8),
                Icon(Icons.notes, size: 16, color: Color(0xFF6C757D)),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              task.completionNotes!,
              style: const TextStyle(
                fontSize: 16,
                color: Color(0xFF2F2F2F),
                fontFamily: 'Cairo',
                height: 1.5,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _handleStatusUpdate(int taskId, String newStatus) async {
    final success =
        await context.read<TaskManager>().changeTaskStatus(taskId, newStatus);
    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('تم تحديث الحالة بنجاح',
              style: TextStyle(fontFamily: 'Cairo')),
          backgroundColor: AppColors.success,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }
}
