import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../providers/attendance_manager.dart';

class AttendanceCard extends StatelessWidget {
  const AttendanceCard({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<AttendanceManager>(
      builder: (context, attendanceProvider, child) {
        // show loading card while fetching attendance data
        // this prevents showing "not checked in" message before we know the real status
        if (attendanceProvider.isLoading && attendanceProvider.todayRecord == null) {
          return _buildLoadingCard();
        }

        final attendance = attendanceProvider.todayRecord;

        // user not checked in today
        if (attendance == null) {
          return _buildNotCheckedInCard(context);
        }

        final checkInTime = attendance.checkInTimeFormatted;
        final checkOutTime = attendance.checkOutTimeFormatted;
        final isActive = attendance.isActive;

        // alredy checked out work done
        if (!isActive && checkOutTime != null) {
          return _buildCheckedOutCard(
              context, checkInTime, checkOutTime, attendance.workDurationFormatted);
        }

        // still working now
        return _buildActiveCard(
            context, checkInTime, attendance.workDurationFormatted, isActive);
      },
    );
  }

  Widget _buildLoadingCard() {
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
            child: const SizedBox(
              width: 24,
              height: 24,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: AppColors.primary,
              ),
            ),
          ),
          const SizedBox(width: 16),
          const Expanded(
            child: Text(
              'جاري التحقق من حالة الحضور...',
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w600,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNotCheckedInCard(BuildContext context) {
    return GestureDetector(
      onTap: () => Navigator.pushNamed(context, Routes.attendance),
      child: Container(
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
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'لم تبدأ العمل بعد اليوم',
                    style: TextStyle(
                      fontSize: 17,
                      fontWeight: FontWeight.w600,
                      color: AppColors.warning,
                      fontFamily: 'Cairo',
                      height: 1.4,
                    ),
                  ),
                  SizedBox(height: 4),
                  Text(
                    'اضغط هنا لبدء العمل',
                    style: TextStyle(
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
              color: AppColors.warning,
              size: 20,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCheckedOutCard(
      BuildContext context, String checkInTime, String checkOutTime, String workDuration) {
    return GestureDetector(
      onTap: () => Navigator.pushNamed(context, Routes.attendance),
      child: Container(
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
      ),
    );
  }

  Widget _buildActiveCard(
      BuildContext context, String checkInTime, String? workDuration, bool isActive) {
    return GestureDetector(
      onTap: () => Navigator.pushNamed(context, Routes.attendance),
      child: Container(
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
                    'أنت في العمل منذ الساعة $checkInTime',
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
                      'مدة العمل حتى الآن: $workDuration',
                      style: const TextStyle(
                        fontSize: 14,
                        color: AppColors.textSecondary,
                        fontFamily: 'Cairo',
                      ),
                    ),
                  ],
                  const SizedBox(height: 4),
                  const Text(
                    'اضغط لإنهاء العمل',
                    style: TextStyle(
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
              color: AppColors.success,
              size: 20,
            ),
          ],
        ),
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
