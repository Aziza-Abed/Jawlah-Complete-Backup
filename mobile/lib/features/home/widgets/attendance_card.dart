import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../providers/attendance_manager.dart';

class AttendanceCard extends StatelessWidget {
  const AttendanceCard({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<AttendanceManager>(
      builder: (context, attendanceProvider, child) {
        final attendance = attendanceProvider.todayRecord;

        // not checked in today
        if (attendance == null) {
          return _buildNotCheckedInCard();
        }

        final checkInTime = attendance.checkInTimeFormatted;
        final checkOutTime = attendance.checkOutTimeFormatted;
        final isActive = attendance.isActive;

        // checked out (work completed)
        if (!isActive && checkOutTime != null) {
          return _buildCheckedOutCard(
              checkInTime, checkOutTime, attendance.workDurationFormatted);
        }

        // active (still checked in)
        return _buildActiveCard(
            checkInTime, attendance.workDurationFormatted, isActive);
      },
    );
  }

  Widget _buildNotCheckedInCard() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: _cardDecoration(),
      child: Row(
        children: [
          Container(
            width: 52,
            height: 52,
            decoration: _iconDecoration(AppColors.warning),
            child: const Icon(
              Icons.access_time,
              size: 30,
              color: AppColors.warning,
            ),
          ),
          const SizedBox(width: 16),
          const Expanded(
            child: Text(
              'لم يتم تسجيل الحضور اليوم',
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w600,
                color: AppColors.warning,
                fontFamily: 'Cairo',
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCheckedOutCard(
      String checkInTime, String checkOutTime, String workDuration) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: _cardDecoration(),
      child: Row(
        children: [
          Container(
            width: 52,
            height: 52,
            decoration: _iconDecoration(AppColors.primary),
            child: const Icon(
              Icons.done_all,
              size: 30,
              color: AppColors.primary,
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'تم إنهاء العمل بنجاح. شكراً لك!',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w600,
                    color: AppColors.primary,
                    fontFamily: 'Cairo',
                    height: 1.4,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'مدة العمل: $workDuration',
                  style: const TextStyle(
                    fontSize: 14,
                    color: AppColors.textSecondary,
                    fontFamily: 'Cairo',
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'من $checkInTime إلى $checkOutTime',
                  style: const TextStyle(
                    fontSize: 14,
                    color: AppColors.textSecondary,
                    fontFamily: 'Cairo',
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildActiveCard(
      String checkInTime, String? workDuration, bool isActive) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: _cardDecoration(),
      child: Row(
        children: [
          Container(
            width: 52,
            height: 52,
            decoration: _iconDecoration(AppColors.success),
            child: const Icon(
              Icons.check_circle,
              size: 30,
              color: AppColors.success,
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'تم تسجيل حضورك بنجاح عند الساعة $checkInTime',
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w600,
                    color: AppColors.success,
                    fontFamily: 'Cairo',
                    height: 1.4,
                  ),
                ),
                if (isActive && workDuration != null) ...[
                  const SizedBox(height: 4),
                  Text(
                    'مدة العمل: $workDuration',
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
        ],
      ),
    );
  }

  BoxDecoration _cardDecoration() {
    return BoxDecoration(
      color: AppColors.cardBackground,
      borderRadius: BorderRadius.circular(16),
      boxShadow: [
        BoxShadow(
          color: Colors.black.withOpacity(0.05),
          blurRadius: 10,
          offset: const Offset(0, 2),
        ),
      ],
    );
  }

  BoxDecoration _iconDecoration(Color color) {
    return BoxDecoration(
      color: color.withOpacity(0.1),
      shape: BoxShape.circle,
    );
  }
}
