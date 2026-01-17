import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../features/home/home_screen.dart';
import '../../features/tasks/tasks_list_screen.dart';
import '../../features/issues/report_problem_screen.dart';
import '../../features/notifications/notifications_screen.dart';
import '../../features/settings/settings_screen.dart';

class BottomNavigationScreen extends StatefulWidget {
  const BottomNavigationScreen({super.key});

  @override
  State<BottomNavigationScreen> createState() => _BottomNavigationScreenState();
}

class _BottomNavigationScreenState extends State<BottomNavigationScreen> {
  int _currentIndex = 0;

  final List<Widget> _screens = [
    const HomeScreen(),
    const TasksListScreen(),
    const ReportProblemScreen(),
    const NotificationsScreen(),
    const SettingsScreen(),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: _screens[_currentIndex],
      bottomNavigationBar: _buildBottomNavigationBar(),
    );
  }

  Widget _buildBottomNavigationBar() {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.background,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.06),
            blurRadius: 6,
            offset: const Offset(0, -1),
          ),
        ],
      ),
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _buildNavItem(
                index: 0,
                icon: Icons.home_outlined,
                label: 'الرئيسية',
              ),
              _buildNavItem(
                index: 1,
                icon: Icons.assignment_outlined,
                label: 'المهام',
              ),
              _buildCenterNavItem(
                index: 2,
                label: 'إبلاغ',
              ),
              _buildNavItem(
                index: 3,
                icon: Icons.notifications_none,
                label: 'الإشعارات',
              ),
              _buildNavItem(
                index: 4,
                icon: Icons.settings_outlined,
                label: 'الإعدادات',
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildNavItem({
    required int index,
    required IconData icon,
    required String label,
  }) {
    final isActive = _currentIndex == index;

    return Expanded(
      child: InkWell(
        onTap: () => setState(() => _currentIndex = index),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 30,
              color: isActive ? AppColors.primary : AppColors.textSecondary,
            ),
            const SizedBox(height: 4),
            Text(
              label,
              style: TextStyle(
                fontSize: 12,
                fontWeight: isActive ? FontWeight.bold : FontWeight.normal,
                color: isActive ? AppColors.primary : AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCenterNavItem({
    required int index,
    required String label,
  }) {
    final isActive = _currentIndex == index;

    return Expanded(
      child: InkWell(
        onTap: () => setState(() => _currentIndex = index),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Transform.translate(
              offset: const Offset(0, -12),
              child: Container(
                width: 65,
                height: 65,
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  shape: BoxShape.circle,
                  boxShadow: [
                    BoxShadow(
                      color: AppColors.primary.withOpacity(0.35),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: const Icon(
                  Icons.camera_alt_rounded,
                  size: 32,
                  color: Colors.white,
                ),
              ),
            ),
            Text(
              label,
              style: TextStyle(
                fontSize: 11,
                fontWeight: isActive ? FontWeight.bold : FontWeight.normal,
                color: isActive ? AppColors.primary : AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
          ],
        ),
      ),
    );
  }
}
