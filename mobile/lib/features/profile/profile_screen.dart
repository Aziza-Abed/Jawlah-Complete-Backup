import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';

import '../../core/utils/photo_picker_helper.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../providers/auth_manager.dart';
import '../../providers/task_manager.dart';
import '../../data/models/user_model.dart';

// worker profile screen with stats and personal info
class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  File? _profileImage;

  @override
  void initState() {
    super.initState();
    // 1. load tasks to show stats in profile
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TaskManager>().loadTasks();
    });
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthManager>();
    final tasksProvider = context.watch<TaskManager>();
    final user = authProvider.user;

    return BaseScreen(
      title: 'الملف الشخصي',
      showBackButton: true,
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
        child: Column(
          children: [
            _buildAvatarCard(authProvider.userName, user?.workerTypeArabic),
            const SizedBox(height: 20),
            _buildPersonalInfoCard(user),
            const SizedBox(height: 16),
            _buildStatisticsCard(tasksProvider),
            const SizedBox(height: 16),
            _buildWorkInfoCard(user),
          ],
        ),
      ),
    );
  }

  Widget _buildAvatarCard(String userName, String? workerType) {
    final displayName = userName.isNotEmpty ? userName : 'الملف الشخصي';

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(24),
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
        children: [
          GestureDetector(
            onTap: _pickProfileImage,
            child: Stack(
              children: [
                Container(
                  width: 100,
                  height: 100,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withOpacity(0.1),
                    shape: BoxShape.circle,
                    border: Border.all(color: AppColors.primary, width: 3),
                    image: _profileImage != null
                        ? DecorationImage(
                            image: FileImage(_profileImage!),
                            fit: BoxFit.cover,
                          )
                        : null,
                  ),
                  child: Hero(
                    tag: 'profile-avatar',
                    child: _profileImage == null
                        ? const Icon(Icons.person,
                            size: 50, color: AppColors.primary)
                        : const SizedBox.shrink(),
                  ),
                ),
                Positioned(
                  bottom: 0,
                  right: 0,
                  child: Container(
                    width: 32,
                    height: 32,
                    decoration: BoxDecoration(
                      color: AppColors.primary,
                      shape: BoxShape.circle,
                      border:
                          Border.all(color: AppColors.cardBackground, width: 2),
                    ),
                    child: const Icon(Icons.camera_alt,
                        size: 16, color: Colors.white),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Text(
            displayName,
            style: const TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            decoration: BoxDecoration(
              color: AppColors.success.withOpacity(0.1),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Text(
              workerType ?? 'عامل ميداني',
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: AppColors.success,
                fontFamily: 'Cairo',
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _pickProfileImage() async {
    final image = await PhotoPickerHelper.pickImageWithChoice(
      context,
      maxWidth: 512,
      maxHeight: 512,
      imageQuality: 85,
    );

    if (image != null) {
      setState(() => _profileImage = image);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('تم تحديث الصورة الشخصية بنجاح',
                style: const TextStyle(fontFamily: 'Cairo')),
            backgroundColor: AppColors.success,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  Widget _buildPersonalInfoCard(UserModel? user) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'المعلومات الشخصية',
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 16),
          _buildInfoRow(Icons.badge, 'رقم الموظف', user?.employeeId ?? '----'),
          const Divider(height: 24),
          _buildInfoRow(
              Icons.phone, 'رقم الهاتف', user?.phoneNumber ?? 'غير محدد'),
          const Divider(height: 24),
          _buildInfoRow(Icons.location_city, 'القسم',
              user?.workerTypeArabic ?? 'غير محدد'),
        ],
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value) {
    return Row(
      children: [
        Icon(icon, size: 24, color: AppColors.primary),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label,
                  style: const TextStyle(
                      fontSize: 14,
                      color: AppColors.textSecondary,
                      fontFamily: 'Cairo')),
              const SizedBox(height: 4),
              Text(value,
                  style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textPrimary,
                      fontFamily: 'Cairo')),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildStatisticsCard(TaskManager tasksProvider) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'إحصائيات الأداء',
            style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
                fontFamily: 'Cairo'),
          ),
          const SizedBox(height: 20),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _buildStatItem('جديد', tasksProvider.pendingCount.toString(),
                  AppColors.info),
              _buildStatItem('قيد التنفيذ',
                  tasksProvider.inProgressCount.toString(), AppColors.warning),
              _buildStatItem('مكتمل', tasksProvider.completedCount.toString(),
                  AppColors.success),
              _buildStatItem('الكل', tasksProvider.totalTasks.toString(),
                  AppColors.primary),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildStatItem(String label, String value, Color color) {
    return Column(
      children: [
        Text(value,
            style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.bold,
                color: color,
                fontFamily: 'Cairo')),
        const SizedBox(height: 4),
        Text(label,
            style: const TextStyle(
                fontSize: 14,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo')),
      ],
    );
  }

  Widget _buildWorkInfoCard(UserModel? user) {
    String joinDate = 'غير محدد';
    if (user?.createdAt != null) {
      final date = user!.createdAt;
      joinDate =
          '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    }

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'معلومات العمل',
            style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
                fontFamily: 'Cairo'),
          ),
          const SizedBox(height: 16),
          _buildInfoRow(Icons.calendar_today, 'تاريخ التعيين', joinDate),
          const Divider(height: 24),
          _buildInfoRow(
              Icons.work_outline, 'الدور', user?.roleArabic ?? 'عامل ميداني'),
          const Divider(height: 24),
          _buildInfoRow(Icons.schedule, 'ساعات العمل', '08:00 - 16:00'),
        ],
      ),
    );
  }
}
