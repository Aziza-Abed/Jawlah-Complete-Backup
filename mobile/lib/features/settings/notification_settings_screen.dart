import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../core/theme/app_colors.dart';
import '../../presentation/widgets/base_screen.dart';

/// SR16.5: Notification preferences screen
/// Allows users to configure notification preferences
class NotificationSettingsScreen extends StatefulWidget {
  const NotificationSettingsScreen({super.key});

  @override
  State<NotificationSettingsScreen> createState() =>
      _NotificationSettingsScreenState();
}

class _NotificationSettingsScreenState
    extends State<NotificationSettingsScreen> {
  bool _taskNotifications = true;
  bool _attendanceReminders = true;
  bool _issueUpdates = true;
  bool _deadlineReminders = true;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadPreferences();
  }

  Future<void> _loadPreferences() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      _taskNotifications = prefs.getBool('notif_tasks') ?? true;
      _attendanceReminders = prefs.getBool('notif_attendance') ?? true;
      _issueUpdates = prefs.getBool('notif_issues') ?? true;
      _deadlineReminders = prefs.getBool('notif_deadlines') ?? true;
      _isLoading = false;
    });
  }

  Future<void> _savePreference(String key, bool value) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(key, value);
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'إعدادات الإشعارات',
      showBackButton: true,
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildInfoCard(),
                  const SizedBox(height: 20),
                  _buildSettingsCard(),
                ],
              ),
            ),
    );
  }

  Widget _buildInfoCard() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.info.withOpacity(0.1),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.info.withOpacity(0.3)),
      ),
      child: const Row(
        children: [
          Icon(Icons.info_outline, color: AppColors.info),
          SizedBox(width: 12),
          Expanded(
            child: Text(
              'تحكم في أنواع الإشعارات التي تريد استلامها',
              style: TextStyle(
                fontFamily: 'Cairo',
                fontSize: 14,
                color: AppColors.info,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSettingsCard() {
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
        children: [
          _buildSwitchTile(
            icon: Icons.task_alt,
            title: 'إشعارات المهام',
            subtitle: 'إشعارات عند تعيين مهام جديدة أو تحديث حالتها',
            value: _taskNotifications,
            onChanged: (value) {
              setState(() => _taskNotifications = value);
              _savePreference('notif_tasks', value);
            },
          ),
          _buildDivider(),
          _buildSwitchTile(
            icon: Icons.access_time,
            title: 'تذكيرات الحضور',
            subtitle: 'تذكير بتسجيل الحضور والانصراف',
            value: _attendanceReminders,
            onChanged: (value) {
              setState(() => _attendanceReminders = value);
              _savePreference('notif_attendance', value);
            },
          ),
          _buildDivider(),
          _buildSwitchTile(
            icon: Icons.report_problem,
            title: 'تحديثات البلاغات',
            subtitle: 'إشعارات عند تحديث حالة البلاغات المقدمة',
            value: _issueUpdates,
            onChanged: (value) {
              setState(() => _issueUpdates = value);
              _savePreference('notif_issues', value);
            },
          ),
          _buildDivider(),
          _buildSwitchTile(
            icon: Icons.alarm,
            title: 'تذكيرات المواعيد النهائية',
            subtitle: 'تنبيه قبل اقتراب موعد تسليم المهام',
            value: _deadlineReminders,
            onChanged: (value) {
              setState(() => _deadlineReminders = value);
              _savePreference('notif_deadlines', value);
            },
          ),
        ],
      ),
    );
  }

  Widget _buildSwitchTile({
    required IconData icon,
    required String title,
    required String subtitle,
    required bool value,
    required ValueChanged<bool> onChanged,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: AppColors.primary.withOpacity(0.1),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: AppColors.primary, size: 24),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontFamily: 'Cairo',
                    fontSize: 16,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  subtitle,
                  style: const TextStyle(
                    fontFamily: 'Cairo',
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          Switch(
            value: value,
            onChanged: onChanged,
            activeColor: AppColors.primary,
          ),
        ],
      ),
    );
  }

  Widget _buildDivider() {
    return Divider(
      height: 1,
      indent: 16,
      endIndent: 16,
      color: Colors.grey.withOpacity(0.2),
    );
  }
}
