import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/routing/app_router.dart';
import '../../core/theme/app_colors.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../providers/auth_manager.dart';
import '../../providers/task_manager.dart';
import '../../data/services/attendance_service.dart';
import '../../data/models/attendance_model.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  @override
  Widget build(BuildContext context) {
    final authManager = context.read<AuthManager>();
    final userName = authManager.userName;
    final workerType = authManager.user?.workerTypeArabic;

    return BaseScreen(
      title: 'الإعدادات',
      showBackButton: false,
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            _buildProfileCard(userName, workerType),
            const SizedBox(height: 20),
            _buildSettingsSection(
              title: 'الحساب',
              items: [
                _SettingItem(
                  icon: Icons.person,
                  title: 'الملف الشخصي',
                  onTap: () {
                    Navigator.of(context).pushNamed(Routes.profile);
                  },
                ),
                _SettingItem(
                  icon: Icons.access_time,
                  title: 'الحضور والانصراف',
                  onTap: () {
                    Navigator.of(context).pushNamed(Routes.attendance);
                  },
                ),
              ],
            ),
            const SizedBox(height: 16),
            _buildSettingsSection(
              title: 'التطبيق',
              items: [
                _SettingItem(
                  icon: Icons.language,
                  title: 'اللغة',
                  subtitle: 'العربية',
                  onTap: () {},
                ),
                _SettingItem(
                  icon: Icons.info,
                  title: 'عن التطبيق',
                  subtitle: 'الإصدار 1.0.0',
                  onTap: () {
                    _showAboutDialog(context);
                  },
                ),
              ],
            ),
            const SizedBox(height: 16),
            _buildSettingsSection(
              title: 'الدعم',
              items: [
                _SettingItem(
                  icon: Icons.help,
                  title: 'المساعدة والدعم',
                  onTap: () {},
                ),
                _SettingItem(
                  icon: Icons.phone,
                  title: 'الاتصال بالدعم الفني',
                  onTap: () {},
                ),
              ],
            ),
            const SizedBox(height: 16),
            _buildLogoutButton(context),
          ],
        ),
      ),
    );
  }

  Widget _buildLogoutButton(BuildContext context) {
    return Container(
      width: double.infinity,
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
      child: InkWell(
        onTap: () => _showLogoutDialog(context),
        borderRadius: BorderRadius.circular(16),
        child: const Padding(
          padding: EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Row(
            children: [
              Icon(Icons.logout, size: 24, color: AppColors.error),
              SizedBox(width: 16),
              Text(
                'تسجيل الخروج',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: AppColors.error,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showLogoutDialog(BuildContext context) async {
    // Fetch today's work summary
    final taskManager = context.read<TaskManager>();
    AttendanceModel? todayAttendance;

    try {
      final attendanceService = AttendanceService();
      todayAttendance = await attendanceService.getTodayAttendance();
    } catch (e) {
      // Ignore errors - we'll show summary without attendance info
    }

    if (!context.mounted) return;

    // Calculate work stats
    final completedToday = taskManager.myTasks.where((t) =>
        t.isCompleted &&
        t.completedAt != null &&
        t.completedAt!.day == DateTime.now().day &&
        t.completedAt!.month == DateTime.now().month &&
        t.completedAt!.year == DateTime.now().year
    ).length;

    String workDuration = 'غير محدد';
    String checkInTime = 'غير محدد';
    String checkOutTime = 'غير محدد';
    String lateInfo = '';

    if (todayAttendance != null) {
      final checkIn = todayAttendance.checkInTime;
      checkInTime = '${checkIn.hour.toString().padLeft(2, '0')}:${checkIn.minute.toString().padLeft(2, '0')}';

      if (todayAttendance.checkOutTime != null) {
        final checkOut = todayAttendance.checkOutTime!;
        checkOutTime = '${checkOut.hour.toString().padLeft(2, '0')}:${checkOut.minute.toString().padLeft(2, '0')}';
        final duration = checkOut.difference(checkIn);
        final hours = duration.inHours;
        final minutes = duration.inMinutes % 60;
        workDuration = '$hours ساعة و $minutes دقيقة';
      } else {
        final now = DateTime.now();
        final duration = now.difference(checkIn);
        final hours = duration.inHours;
        final minutes = duration.inMinutes % 60;
        workDuration = '$hours ساعة و $minutes دقيقة (جاري)';
      }

      if (todayAttendance.lateMinutes > 0) {
        lateInfo = 'التأخير: ${todayAttendance.lateMinutes} دقيقة';
      }
    }

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.summarize, color: AppColors.primary),
            SizedBox(width: 8),
            Text('ملخص العمل اليوم', style: TextStyle(fontFamily: 'Cairo', fontWeight: FontWeight.bold)),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildSummaryRow(Icons.login, 'وقت الدخول', checkInTime),
            const SizedBox(height: 8),
            _buildSummaryRow(Icons.logout, 'وقت الخروج', checkOutTime),
            const SizedBox(height: 8),
            _buildSummaryRow(Icons.timer, 'مدة العمل', workDuration),
            const SizedBox(height: 8),
            _buildSummaryRow(Icons.task_alt, 'المهام المكتملة', '$completedToday مهمة'),
            if (lateInfo.isNotEmpty) ...[
              const SizedBox(height: 8),
              _buildSummaryRow(Icons.warning_amber, 'ملاحظة', lateInfo, color: AppColors.warning),
            ],
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.info.withOpacity(0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Row(
                children: [
                  Icon(Icons.info_outline, size: 18, color: AppColors.info),
                  SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'سيتم تسجيل الانصراف تلقائياً عند الخروج',
                      style: TextStyle(fontFamily: 'Cairo', fontSize: 12, color: AppColors.info),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(ctx);
              context.read<AuthManager>().doLogout();
              Navigator.of(context).pushNamedAndRemoveUntil(Routes.login, (route) => false);
            },
            style: ElevatedButton.styleFrom(backgroundColor: AppColors.error),
            child: const Text('تسجيل الخروج', style: TextStyle(fontFamily: 'Cairo', color: Colors.white)),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryRow(IconData icon, String label, String value, {Color? color}) {
    return Row(
      children: [
        Icon(icon, size: 20, color: color ?? AppColors.primary),
        const SizedBox(width: 8),
        Text(
          '$label: ',
          style: const TextStyle(fontFamily: 'Cairo', fontWeight: FontWeight.bold),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(fontFamily: 'Cairo', color: color ?? AppColors.textPrimary),
          ),
        ),
      ],
    );
  }

  Widget _buildProfileCard(String userName, String? workerType) {
    final displayName = userName.isNotEmpty ? userName : 'الملف الشخصي';
    final displayRole = workerType ?? 'عامل ميداني';
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
          Container(
            width: 70,
            height: 70,
            decoration: BoxDecoration(
              color: AppColors.primary.withOpacity(0.1),
              shape: BoxShape.circle,
            ),
            child: const Hero(
              tag: 'profile-avatar',
              child: Icon(
                Icons.person,
                size: 36,
                color: AppColors.primary,
              ),
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  displayName,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                    fontFamily: 'Cairo',
                  ),
                ),
                const SizedBox(height: 12),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                  decoration: BoxDecoration(
                    color: AppColors.success.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    displayRole,
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: AppColors.success,
                      fontFamily: 'Cairo',
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSettingsSection({
    required String title,
    required List<_SettingItem> items,
  }) {
    return Container(
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
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
            child: Text(
              title,
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.bold,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
          ),
          ...items.map((item) => _buildSettingItem(item)),
        ],
      ),
    );
  }

  Widget _buildSettingItem(_SettingItem item) {
    return InkWell(
      onTap: item.onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        child: Row(
          children: [
            Icon(
              item.icon,
              size: 24,
              color: AppColors.primary,
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.title,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textPrimary,
                      fontFamily: 'Cairo',
                    ),
                  ),
                  if (item.subtitle != null) ...[
                    const SizedBox(height: 4),
                    Text(
                      item.subtitle!,
                      style: const TextStyle(
                        fontSize: 14,
                        color: AppColors.textSecondary,
                        fontFamily: 'Cairo',
                      ),
                    ),
                  ],
                ],
              ),
            ),
            const Icon(
              Icons.arrow_forward_ios,
              size: 16,
              color: AppColors.textSecondary,
            ),
          ],
        ),
      ),
    );
  }

  void _showAboutDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (BuildContext dialogContext) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          backgroundColor: AppColors.cardBackground,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          titlePadding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
          contentPadding: const EdgeInsets.fromLTRB(24, 8, 24, 16),
          actionsPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
          title: const Row(
            mainAxisAlignment: MainAxisAlignment.start,
            children: [
              Text(
                'عن التطبيق',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 20,
                  fontFamily: 'Cairo',
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          content: const Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'جولة',
                textAlign: TextAlign.right,
                style: TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: AppColors.primary,
                  fontFamily: 'Cairo',
                ),
              ),
              SizedBox(height: 4),
              Text(
                'نظام إدارة ومتابعة ميدانية ذكي',
                textAlign: TextAlign.right,
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  fontFamily: 'Cairo',
                ),
              ),
              SizedBox(height: 16),
              Text(
                'الإصدار 1.0.0',
                textAlign: TextAlign.right,
                style: TextStyle(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                  fontFamily: 'Cairo',
                ),
              ),
              SizedBox(height: 16),
              Text(
                'بلدية البيرة',
                textAlign: TextAlign.right,
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                  fontFamily: 'Cairo',
                ),
              ),
              SizedBox(height: 8),
              Text(
                'تطبيق لإدارة المهام الميدانية والمتابعة الميدانية الذكية لعمال بلدية البيرة',
                textAlign: TextAlign.right,
                style: TextStyle(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                  height: 1.6,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
          actions: [
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () => Navigator.pop(dialogContext),
                  child: const Text(
                    'حسناً',
                    style: TextStyle(
                      color: AppColors.primary,
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      fontFamily: 'Cairo',
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _SettingItem {
  final IconData icon;
  final String title;
  final String? subtitle;
  final VoidCallback onTap;

  _SettingItem({
    required this.icon,
    required this.title,
    this.subtitle,
    required this.onTap,
  });
}
