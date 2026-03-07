import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:geolocator/geolocator.dart';
import '../../core/config/app_constants.dart';
import '../../core/theme/app_colors.dart';

import '../../presentation/widgets/base_screen.dart';
import '../../presentation/widgets/authenticated_image.dart';
import '../../presentation/widgets/offline_banner.dart';
import '../../presentation/widgets/task_location_map_view.dart';
import '../../providers/task_manager.dart';
import '../../providers/appeal_manager.dart';
import '../../data/models/task_model.dart';
import '../appeals/submit_appeal_screen.dart';
import 'widgets/task_details_card.dart';
import '../../core/routing/app_router.dart';

// screen to see task details and change status
class TaskDetailsScreen extends StatefulWidget {
  const TaskDetailsScreen({super.key});

  @override
  State<TaskDetailsScreen> createState() => _TaskDetailsScreenState();
}

class _TaskDetailsScreenState extends State<TaskDetailsScreen> {
  Future<double?>? _distanceFuture;
  int? _distanceTaskId;

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
      body: Column(
        children: [
          const OfflineBanner(),
          Expanded(
            child: Consumer<TaskManager>(
              builder: (context, provider, child) {
                if (provider.isLoading) {
                  return const Center(
                    child: CircularProgressIndicator(color: AppColors.primary),
                  );
                }

                // show error state
                if (provider.errorMessage != null &&
                    provider.currentTask == null) {
                  return _buildErrorState(provider);
                }

                final task = provider.currentTask;
                if (task == null) {
                  return const Center(
                    child: Text(
                      'لم يتم اختيار مهمة',
                      style: TextStyle(fontFamily: 'Cairo'),
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
                      const SizedBox(height: 16),

                      // View task location on map (if supervisor set a location)
                      if (task.latitude != null && task.longitude != null) ...[
                        _buildViewLocationButton(task),
                        const SizedBox(height: 16),
                      ],

                      // show different buttons based on status
                      if (statusLower == 'pending') ...[
                        _buildPrimaryActionButton(
                          icon: Icons.play_arrow_rounded,
                          label: 'بدء المهمة الآن',
                          color: AppColors.primary,
                          onPressed: () => _handleStartTask(task.taskId),
                        ),
                      ],

                      if (statusLower == 'inprogress') ...[
                        _buildProgressSteps(task),
                        const SizedBox(height: 16),
                        if (task.hasLocation) ...[
                          _buildDistanceInfo(task),
                          const SizedBox(height: 16),
                        ],
                        _buildPrimaryActionButton(
                          icon: Icons.check_circle_rounded,
                          label: 'إرسال الإثبات',
                          color: AppColors.success,
                          onPressed: () => Navigator.of(context).pushNamed(
                            Routes.submitEvidence,
                            arguments: task.taskId,
                          ),
                        ),
                        const SizedBox(height: 12),
                        // Visible pause button
                        SizedBox(
                          width: double.infinity,
                          height: 52,
                          child: OutlinedButton.icon(
                            onPressed: () =>
                                _handleStatusUpdate(task.taskId, 'pending'),
                            icon: const Icon(Icons.pause_rounded, size: 22),
                            label: const Text(
                              'إيقاف مؤقت',
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w600,
                                fontFamily: 'Cairo',
                              ),
                            ),
                            style: OutlinedButton.styleFrom(
                              foregroundColor: AppColors.warning,
                              side: const BorderSide(
                                  color: AppColors.warning, width: 1.5),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(16),
                              ),
                            ),
                          ),
                        ),
                      ],

                      if (statusLower == 'underreview') ...[
                        _buildCompletedSection(task),
                        const SizedBox(height: 16),
                        // Status explanation card
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: AppColors.info.withOpacity(0.1),
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                                color: AppColors.info.withOpacity(0.3)),
                          ),
                          child: const Row(
                            children: [
                              Icon(Icons.hourglass_top_rounded,
                                  color: AppColors.info, size: 24),
                              SizedBox(width: 12),
                              Expanded(
                                child: Text(
                                  'المهمة بانتظار مراجعة المشرف. ستتلقى إشعاراً عند القبول أو الرفض.',
                                  style: TextStyle(
                                    fontSize: 14,
                                    fontFamily: 'Cairo',
                                    color: AppColors.info,
                                    height: 1.5,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],

                      if (statusLower == 'completed') ...[
                        _buildApprovalStatus(true),
                        const SizedBox(height: 16),
                        _buildCompletedSection(task),
                      ],

                      if (statusLower == 'rejected') ...[
                        _buildApprovalStatus(false),
                        const SizedBox(height: 16),

                        // Show rejection reason for ALL rejections
                        if (task.rejectionReason != null &&
                            task.rejectionReason!.isNotEmpty) ...[
                          Container(
                            padding: const EdgeInsets.all(16),
                            decoration: BoxDecoration(
                              color: Colors.red[50],
                              borderRadius: BorderRadius.circular(12),
                              border: Border.all(
                                  color: AppColors.error.withOpacity(0.3)),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    Icon(
                                      task.isAutoRejected
                                          ? Icons.gps_off_rounded
                                          : Icons.feedback_rounded,
                                      color: Colors.red[700],
                                      size: 22,
                                    ),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: Text(
                                        task.isAutoRejected
                                            ? 'رُفض تلقائياً (موقع غير مطابق)'
                                            : 'سبب الرفض',
                                        style: TextStyle(
                                          fontSize: 16,
                                          fontWeight: FontWeight.bold,
                                          color: Colors.red[900],
                                          fontFamily: 'Cairo',
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                                const Divider(height: 24),
                                Text(
                                  task.rejectionReason!,
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontFamily: 'Cairo',
                                    height: 1.5,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 12),
                        ],

                        // Guidance text
                        Container(
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            color: AppColors.warning.withOpacity(0.1),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: const Row(
                            children: [
                              Icon(Icons.lightbulb_outline,
                                  color: AppColors.warning, size: 22),
                              SizedBox(width: 10),
                              Expanded(
                                child: Text(
                                  'يرجى معالجة الملاحظات أعلاه ثم إعادة المحاولة.',
                                  style: TextStyle(
                                    fontSize: 14,
                                    fontFamily: 'Cairo',
                                    color: AppColors.textSecondary,
                                    height: 1.4,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),

                        // Show appeal button for auto-rejected tasks
                        if (task.isAutoRejected) ...[
                          Consumer<AppealManager>(
                            builder: (context, appealManager, _) {
                              final hasAppeal =
                                  appealManager.hasAppealForTask(task.taskId);

                              if (hasAppeal) {
                                final appeal =
                                    appealManager.getAppealForTask(task.taskId);
                                return _buildAppealStatusCard(appeal);
                              }

                              return _buildPrimaryActionButton(
                                icon: Icons.gavel_rounded,
                                label: 'تقديم طعن في الرفض',
                                color: Colors.orange,
                                onPressed: () => _handleSubmitAppeal(task),
                              );
                            },
                          ),
                          const SizedBox(height: 12),
                        ],

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
          ),
        ],
      ),
    );
  }

  Widget _buildErrorState(TaskManager provider) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: AppColors.error.withOpacity(0.1),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.error_outline,
                  size: 64, color: AppColors.error),
            ),
            const SizedBox(height: 16),
            Text(
              provider.errorMessage ?? 'حدث خطأ أثناء تحميل المهمة',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 16,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () {
                final taskId =
                    ModalRoute.of(context)?.settings.arguments as int?;
                if (taskId != null) {
                  provider.getTaskDetails(taskId);
                }
              },
              icon: const Icon(Icons.refresh),
              label: const Text('إعادة المحاولة',
                  style: TextStyle(fontFamily: 'Cairo')),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
              ),
            ),
          ],
        ),
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

  Widget _buildViewLocationButton(TaskModel task) {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        onPressed: () {
          Navigator.of(context).push(
            MaterialPageRoute(
              builder: (context) => TaskLocationMapView(
                latitude: task.latitude!,
                longitude: task.longitude!,
                taskTitle: task.title,
              ),
            ),
          );
        },
        icon: const Icon(Icons.map, size: 20),
        label: const Text(
          'عرض موقع المهمة على الخريطة',
          style: TextStyle(
            fontSize: 15,
            fontFamily: 'Cairo',
            fontWeight: FontWeight.w600,
          ),
        ),
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          side: const BorderSide(color: AppColors.primary, width: 1.5),
          padding: const EdgeInsets.symmetric(vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
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
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('بدء المهمة', style: TextStyle(fontFamily: 'Cairo')),
        content: const Text('هل تريد بدء العمل على هذه المهمة؟',
            style: TextStyle(fontFamily: 'Cairo')),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('بدء',
                style:
                    TextStyle(color: AppColors.primary, fontFamily: 'Cairo')),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

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
    // Use photos array first (modern), fallback to photoUrl (legacy)
    final String imageUrl =
        task.photos.isNotEmpty ? task.photos.first : (task.photoUrl ?? '');

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
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.start,
            children: [
              const Icon(Icons.verified, color: Color(0xFFA3B18A), size: 20),
              const SizedBox(width: 8),
              const Text(
                'إثبات الإنجاز',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF2F2F2F),
                  fontFamily: 'Cairo',
                ),
              ),
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
            Row(
              mainAxisAlignment: MainAxisAlignment.start,
              children: [
                const Icon(Icons.notes, size: 16, color: Color(0xFF6C757D)),
                const SizedBox(width: 8),
                const Text(
                  'ملاحظات الإنجاز',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                    color: Color(0xFF6C757D),
                    fontFamily: 'Cairo',
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              task.completionNotes!,
              textAlign: TextAlign.right,
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
    // show confirmation for pause action
    if (newStatus == 'pending') {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title:
              const Text('إيقاف مؤقت', style: TextStyle(fontFamily: 'Cairo')),
          content: const Text('هل تريد إيقاف العمل على هذه المهمة مؤقتاً؟',
              style: TextStyle(fontFamily: 'Cairo')),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('إيقاف',
                  style:
                      TextStyle(color: AppColors.warning, fontFamily: 'Cairo')),
            ),
          ],
        ),
      );

      if (confirmed != true || !mounted) return;
    }

    if (!mounted) return;
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

  Widget _buildAppealStatusCard(dynamic appeal) {
    if (appeal == null) return const SizedBox.shrink();

    Color statusColor;
    IconData statusIcon;
    String statusText;

    switch (appeal.status) {
      case 'Pending':
        statusColor = Colors.orange;
        statusIcon = Icons.hourglass_empty;
        statusText = 'الطعن قيد المراجعة';
        break;
      case 'Approved':
        statusColor = Colors.green;
        statusIcon = Icons.check_circle;
        statusText = 'تم قبول الطعن';
        break;
      case 'Rejected':
        statusColor = Colors.red;
        statusIcon = Icons.cancel;
        statusText = 'تم رفض الطعن';
        break;
      default:
        statusColor = Colors.grey;
        statusIcon = Icons.help_outline;
        statusText = 'حالة غير معروفة';
    }

    return Card(
      color: statusColor.withOpacity(0.1),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(statusIcon, color: statusColor, size: 24),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    statusText,
                    style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.bold,
                      color: statusColor,
                      fontFamily: 'Cairo',
                    ),
                  ),
                ),
              ],
            ),
            if (appeal.status == 'Rejected' &&
                appeal.reviewNotes != null &&
                appeal.reviewNotes!.isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(
                'سبب الرفض: ${appeal.reviewNotes}',
                style: const TextStyle(
                  fontSize: 13,
                  color: AppColors.secondaryText,
                  fontFamily: 'Cairo',
                ),
                textAlign: TextAlign.right,
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _handleSubmitAppeal(TaskModel task) async {
    // Load appeals first to check if one exists
    final appealManager = context.read<AppealManager>();
    await appealManager.loadMyAppeals();

    if (!mounted) return;

    // Navigate to submit appeal screen
    final result = await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (context) => SubmitAppealScreen(rejectedTask: task),
      ),
    );

    // If appeal was submitted successfully, reload task details
    if (result == true && mounted) {
      await context.read<TaskManager>().getTaskDetails(task.taskId);
      if (mounted) {
        await context.read<AppealManager>().loadMyAppeals();
      }
    }
  }

  // ── Phase 2: Progress Steps ──────────────────────────────────────────

  Widget _buildProgressSteps(TaskModel task) {
    final taskManager = context.read<TaskManager>();
    final canUpdate = taskManager.canUpdateProgress(task.taskId);
    final cooldown = taskManager.progressCooldownMinutes(task.taskId);

    const steps = [0, 25, 50, 75, 100];

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.primary.withOpacity(0.2)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'نسبة الإنجاز',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Cairo',
                  color: AppColors.textPrimary,
                ),
              ),
              Text(
                '${task.progressPercentage}%',
                style: TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Cairo',
                  color: _getProgressColor(task.progressPercentage),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          LinearProgressIndicator(
            value: task.progressPercentage / 100,
            backgroundColor: AppColors.primary.withOpacity(0.1),
            valueColor: AlwaysStoppedAnimation<Color>(
              _getProgressColor(task.progressPercentage),
            ),
            minHeight: 8,
            borderRadius: BorderRadius.circular(4),
          ),
          const SizedBox(height: 16),
          Row(
            children: steps.map((step) {
              final isActive = task.progressPercentage >= step;
              final isCurrent = task.progressPercentage == step;
              final isForward = step > task.progressPercentage;
              // Allow tapping any forward step; 100% always enabled (goes to evidence screen)
              final canTap = (isForward && (canUpdate || step == 100)) || (step == 0 && !isCurrent);
              return Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 3),
                  child: _buildStepButton(
                    step: step,
                    isActive: isActive,
                    isCurrent: isCurrent,
                    enabled: canTap,
                    taskId: task.taskId,
                  ),
                ),
              );
            }).toList(),
          ),
          if (!canUpdate && task.progressPercentage < 100) ...[
            const SizedBox(height: 12),
            Center(
              child: Text(
                'يمكنك التحديث بعد $cooldown دقائق',
                style: const TextStyle(
                  fontSize: 13,
                  fontFamily: 'Cairo',
                  color: AppColors.textSecondary,
                  fontStyle: FontStyle.italic,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildStepButton({
    required int step,
    required bool isActive,
    required bool isCurrent,
    required bool enabled,
    required int taskId,
  }) {
    return Material(
      color: isCurrent
          ? _getProgressColor(step)
          : isActive
              ? _getProgressColor(step).withOpacity(0.15)
              : Colors.grey.withOpacity(0.1),
      borderRadius: BorderRadius.circular(10),
      child: InkWell(
        onTap: enabled
            ? () => _confirmProgressUpdate(taskId, step)
            : null,
        borderRadius: BorderRadius.circular(10),
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 10),
          alignment: Alignment.center,
          child: Text(
            '$step%',
            style: TextStyle(
              fontSize: 13,
              fontWeight: isCurrent ? FontWeight.bold : FontWeight.w600,
              fontFamily: 'Cairo',
              color: isCurrent
                  ? Colors.white
                  : isActive
                      ? _getProgressColor(step)
                      : AppColors.textSecondary,
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _confirmProgressUpdate(int taskId, int step) async {
    // When progress reaches 100%, navigate to evidence screen instead
    if (step == 100) {
      Navigator.of(context).pushNamed(
        Routes.submitEvidence,
        arguments: taskId,
      );
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title:
            const Text('تحديث التقدم', style: TextStyle(fontFamily: 'Cairo')),
        content: Text(
          'هل تريد تحديث نسبة الإنجاز إلى $step%؟',
          style: const TextStyle(fontFamily: 'Cairo'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('تحديث',
                style:
                    TextStyle(color: AppColors.primary, fontFamily: 'Cairo')),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;
    await _handleProgressUpdate(taskId, step);
  }

  // ── Distance info ────────────────────────────────────────────────────

  Widget _buildDistanceInfo(TaskModel task) {
    // Cache the future so GPS doesn't fire on every setState/rebuild
    if (_distanceTaskId != task.taskId) {
      _distanceTaskId = task.taskId;
      _distanceFuture = _calculateDistance(task.latitude!, task.longitude!);
    }
    return FutureBuilder<double?>(
      future: _distanceFuture,
      builder: (context, snapshot) {
        if (!snapshot.hasData) {
          return Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppColors.info.withOpacity(0.1),
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Row(
              children: [
                SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
                SizedBox(width: 12),
                Text(
                  'جاري حساب المسافة...',
                  style: TextStyle(
                    fontSize: 14,
                    fontFamily: 'Cairo',
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          );
        }

        final distanceMeters = snapshot.data!;
        final distanceKm = distanceMeters / 1000;
        final distanceText = distanceKm < 1
            ? '${distanceMeters.toStringAsFixed(0)} متر'
            : '${distanceKm.toStringAsFixed(2)} كم';

        final isNearby = distanceMeters <= AppConstants.nearbyDistanceMeters;
        final willReject =
            distanceMeters > AppConstants.hardRejectDistanceMeters;
        final icon = isNearby
            ? Icons.check_circle
            : (willReject ? Icons.error : Icons.warning);
        final color = isNearby
            ? AppColors.success
            : (willReject ? AppColors.error : AppColors.warning);
        final message = isNearby
            ? 'أنت في نطاق موقع المهمة'
            : willReject
                ? 'أنت بعيد جداً — قد يتم رفض الإثبات تلقائياً'
                : 'أنت بعيد عن موقع المهمة';

        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: color.withOpacity(0.1),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: color.withOpacity(0.3)),
          ),
          child: Row(
            children: [
              Icon(icon, color: color, size: 24),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      message,
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        fontFamily: 'Cairo',
                        color: color,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'المسافة من موقع المهمة: $distanceText',
                      style: const TextStyle(
                        fontSize: 13,
                        fontFamily: 'Cairo',
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Future<double?> _calculateDistance(double taskLat, double taskLon) async {
    try {
      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
        timeLimit: const Duration(seconds: AppConstants.gpsTimeoutSeconds),
      );

      if (position.accuracy > AppConstants.maxGpsAccuracyMeters) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'دقة GPS منخفضة (${position.accuracy.toInt()}م)\n'
                'انتقل إلى مكان مفتوح للحصول على إشارة أفضل',
                textAlign: TextAlign.center,
              ),
              backgroundColor: Colors.orange,
              duration: const Duration(seconds: 4),
            ),
          );
        }
        return null;
      }

      final distanceMeters = Geolocator.distanceBetween(
        position.latitude,
        position.longitude,
        taskLat,
        taskLon,
      );

      return distanceMeters;
    } on TimeoutException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'انتهت مهلة الحصول على الموقع\n'
              'تأكد من تفعيل GPS وأنك في مكان مفتوح',
              textAlign: TextAlign.center,
            ),
            backgroundColor: Colors.red,
            duration: Duration(seconds: 4),
          ),
        );
      }
      if (kDebugMode) debugPrint('GPS timeout: $e');
      return null;
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('خطأ في الحصول على الموقع: ${e.toString()}'),
            backgroundColor: Colors.red,
          ),
        );
      }
      if (kDebugMode) debugPrint('GPS error: $e');
      return null;
    }
  }

  Color _getProgressColor(int progress) {
    if (progress < 30) return AppColors.error;
    if (progress < 70) return AppColors.warning;
    return AppColors.success;
  }

  Future<void> _handleProgressUpdate(int taskId, int progress) async {
    final taskManager = context.read<TaskManager>();
    await taskManager.updateTaskProgress(taskId, progress);

    if (!mounted) return;

    // Show milestone notification if reached (25%, 50%, 75%)
    final milestoneMsg = taskManager.lastMilestoneMessage;
    if (milestoneMsg != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content:
              Text(milestoneMsg, style: const TextStyle(fontFamily: 'Cairo')),
          backgroundColor: AppColors.success,
          behavior: SnackBarBehavior.floating,
        ),
      );
      taskManager.clearMilestoneMessage();
    } else if (taskManager.errorMessage != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            taskManager.errorMessage!,
            style: const TextStyle(fontFamily: 'Cairo'),
          ),
          backgroundColor: AppColors.error,
        ),
      );
    }
  }

}


