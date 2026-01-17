import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../../core/theme/app_colors.dart';
import '../../../data/models/task_model.dart';

class TaskDetailsCard extends StatelessWidget {
  final TaskModel task;

  const TaskDetailsCard({super.key, required this.task});

  @override
  Widget build(BuildContext context) {
    final formattedDate = task.dueDate != null
        ? '${task.dueDate!.year}-${task.dueDate!.month.toString().padLeft(2, '0')}-${task.dueDate!.day.toString().padLeft(2, '0')}'
        : 'لا يوجد تاريخ';

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
          _buildDetailRow(
            Icons.flag,
            'الأولوية',
            task.priorityArabic,
            valueColor: _getPriorityColor(task.priority),
          ),
          const Divider(height: 24),
          if (task.zoneName != null || task.location != null) ...[
            _buildDetailRow(
              Icons.location_on,
              'المنطقة',
              task.zoneName ?? 'غير محدد',
            ),
            if (task.location != null && task.location!.isNotEmpty && task.location != task.zoneName) ...[
              const SizedBox(height: 8),
              _buildDetailRow(
                Icons.place,
                'الموقع التفصيلي',
                task.location!,
              ),
            ],
            if (task.hasLocation) ...[
              const SizedBox(height: 12),
              _buildNavigateButton(),
            ],
            const Divider(height: 24),
          ],
          _buildDetailRow(
            Icons.calendar_today,
            'التاريخ',
            formattedDate,
          ),
          const Divider(height: 24),
          _buildDetailRow(
            Icons.check_circle_outline,
            'الحالة',
            task.statusArabic,
            valueColor: _getStatusColor(task.status),
          ),
          if (task.taskType != null && task.taskTypeArabic.isNotEmpty) ...[
            const Divider(height: 24),
            _buildDetailRow(
              Icons.category,
              'نوع المهمة',
              task.taskTypeArabic,
            ),
          ],
          if (task.requiresPhotoProof) ...[
            const Divider(height: 24),
            _buildDetailRow(
              Icons.camera_alt,
              'صورة مطلوبة',
              'نعم',
              valueColor: AppColors.warning,
            ),
          ],
          if (task.assignedTo != null) ...[
            const Divider(height: 24),
            _buildDetailRow(
              Icons.person_outline,
              'مسؤول عن',
              task.assignedTo!,
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildDetailRow(
    IconData icon,
    String label,
    String value, {
    Color? valueColor,
  }) {
    return Row(
      children: [
        Icon(
          icon,
          size: 24,
          color: AppColors.primary,
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                  fontFamily: 'Cairo',
                ),
              ),
              const SizedBox(height: 4),
              Text(
                value,
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: valueColor ?? AppColors.textPrimary,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Color _getStatusColor(String status) {
    final statusLower = status.toLowerCase();
    switch (statusLower) {
      case 'pending':
        return AppColors.info;
      case 'inprogress':
        return AppColors.statusInProgress;
      case 'completed':
        return AppColors.success;
      case 'cancelled':
        return AppColors.error;
      default:
        return AppColors.textSecondary;
    }
  }

  Color _getPriorityColor(String priority) {
    final priorityLower = priority.toLowerCase();
    switch (priorityLower) {
      case 'low':
        return AppColors.info;
      case 'medium':
        return AppColors.priorityMedium;
      case 'high':
        return AppColors.error;
      default:
        return AppColors.textSecondary;
    }
  }

  Widget _buildNavigateButton() {
    return SizedBox(
      width: double.infinity,
      child: ElevatedButton.icon(
        onPressed: _openInMaps,
        icon: const Icon(Icons.navigation_rounded, size: 20),
        label: const Text(
          'الانتقال إلى الموقع',
          style: TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.w600,
            fontFamily: 'Cairo',
          ),
        ),
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
      ),
    );
  }

  Future<void> _openInMaps() async {
    if (task.latitude == null || task.longitude == null) return;

    // Try Google Maps first, fallback to Apple Maps on iOS
    final googleMapsUrl = Uri.parse(
      'https://www.google.com/maps/dir/?api=1&destination=${task.latitude},${task.longitude}&travelmode=driving'
    );

    if (await canLaunchUrl(googleMapsUrl)) {
      await launchUrl(googleMapsUrl, mode: LaunchMode.externalApplication);
    }
  }
}
