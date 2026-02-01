import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter/services.dart';
import '../../core/theme/app_colors.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../core/routing/app_router.dart';
import '../../providers/attendance_manager.dart';
import '../../providers/auth_manager.dart';
import '../../core/utils/date_formatter.dart';
import 'widgets/live_work_duration.dart';

class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({super.key});

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends State<AttendanceScreen> {
  @override
  void initState() {
    super.initState();
    // load today attendance when screen opens
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AttendanceManager>().loadTodayRecord();
    });
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'بدء وإنهاء العمل',
      showBackButton: true,
      actions: [
        IconButton(
          icon: const Icon(Icons.refresh, color: Colors.white),
          onPressed: () => context.read<AttendanceManager>().refreshTodayData(),
        ),
        IconButton(
          icon: const Icon(Icons.logout, color: Colors.white),
          onPressed: () => _handleLogout(context),
        ),
      ],
      body: Consumer<AttendanceManager>(
        builder: (context, attendanceProvider, child) {
          if (attendanceProvider.isLoading &&
              attendanceProvider.todayRecord == null) {
            return const Center(
                child: CircularProgressIndicator(color: AppColors.primary));
          }

          if (attendanceProvider.errorMessage != null &&
              attendanceProvider.todayRecord == null) {
            return _buildErrorWidget(attendanceProvider);
          }

          return RefreshIndicator(
            onRefresh: () => attendanceProvider.refreshTodayData(),
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  _buildStatusCard(attendanceProvider),
                  const SizedBox(height: 20),
                  _buildActionButton(attendanceProvider),
                  const SizedBox(height: 20),
                  _buildInfoCard(),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildErrorWidget(AttendanceManager provider) {
    final isOffline = provider.errorMessage?.contains('اتصال') ?? false;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              isOffline ? Icons.cloud_off : Icons.error_outline,
              size: 80,
              color: AppColors.error.withOpacity(0.5),
            ),
            const SizedBox(height: 24),
            Text(
              isOffline ? 'أنت تعمل في وضع عدم الاتصال' : 'حدث خطأ غير متوقع',
              textAlign: TextAlign.center,
              style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Cairo'),
            ),
            const SizedBox(height: 12),
            Text(
              isOffline
                  ? 'يمكنك تسجيل الحضور والانصراف بشكل طبيعي، سيتم حفظ البيانات وإرسالها عند توفر الإنترنت.'
                  : (provider.errorMessage ?? 'يرجى المحاولة مرة أخرى لاحقاً'),
              textAlign: TextAlign.center,
              style: const TextStyle(
                  color: AppColors.textSecondary, fontFamily: 'Cairo'),
            ),
            const SizedBox(height: 32),
            ElevatedButton(
              onPressed: () => provider.refreshTodayData(),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
                padding:
                    const EdgeInsets.symmetric(horizontal: 32, vertical: 12),
              ),
              child: const Text('إعادة المحاولة',
                  style: TextStyle(fontFamily: 'Cairo', color: Colors.white)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusCard(AttendanceManager provider) {
    final attendance = provider.todayRecord;
    final isWorking = provider.isCheckedIn && !provider.isCheckedOut;

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
              offset: const Offset(0, 2))
        ],
      ),
      child: Column(
        children: [
          Text(
              'اليوم: ${DateFormatter.formatDate(DateTime.now())}',
              style: const TextStyle(
                  color: AppColors.textSecondary, fontFamily: 'Cairo')),
          const SizedBox(height: 16),
          Icon(
              isWorking
                  ? Icons.check_circle
                  : (attendance != null ? Icons.done_all : Icons.access_time),
              size: 60,
              color: isWorking
                  ? AppColors.success
                  : (attendance != null
                      ? AppColors.primary
                      : AppColors.warning)),
          const SizedBox(height: 16),
          Text(
              isWorking
                  ? 'أنت الآن في فترة العمل'
                  : (attendance != null
                      ? 'تم إنهاء العمل لهذا اليوم'
                      : 'لم تبدأ العمل بعد'),
              style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Cairo')),
          if (attendance != null) ...[
            const SizedBox(height: 8),
            Text('بدأت العمل: ${attendance.checkInTimeFormatted}',
                style: const TextStyle(
                    color: AppColors.textSecondary, fontFamily: 'Cairo')),
            if (isWorking) ...[
              const SizedBox(height: 12),
              LiveWorkDuration(checkInTime: attendance.checkInTime),
            ] else if (attendance.checkOutTime != null) ...[
              const SizedBox(height: 4),
              Text('أنهيت العمل: ${attendance.checkOutTimeFormatted}',
                  style: const TextStyle(
                      color: AppColors.textSecondary, fontFamily: 'Cairo')),
              const SizedBox(height: 8),
              Text('إجمالي مدة العمل: ${attendance.workDurationFormatted}',
                  style: const TextStyle(
                      fontWeight: FontWeight.bold, fontFamily: 'Cairo')),
            ],
          ],
        ],
      ),
    );
  }

  Widget _buildActionButton(AttendanceManager provider) {
    final bool canCheckIn = !provider.isCheckedIn;

    if (provider.isCheckedOut) return const SizedBox.shrink();

    return SizedBox(
      width: double.infinity,
      child: ElevatedButton(
        onPressed: provider.isLoading
            ? null
            : () => _handleAction(provider, canCheckIn),
        style: ElevatedButton.styleFrom(
          backgroundColor: canCheckIn ? AppColors.oliveGreen : AppColors.error,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(vertical: 16),
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
        child: provider.isLoading
            ? const SizedBox(
                height: 24,
                width: 24,
                child: CircularProgressIndicator(
                    color: Colors.white, strokeWidth: 2))
            : Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(canCheckIn ? Icons.play_arrow_rounded : Icons.stop_rounded),
                  const SizedBox(width: 8),
                  Text(canCheckIn ? 'بدء العمل' : 'إنهاء العمل',
                      style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          fontFamily: 'Cairo')),
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
          border: Border.all(color: AppColors.info.withOpacity(0.3))),
      child: const Text('يتم تسجيل الموقع تلقائياً لأغراض التوثيق.',
          textAlign: TextAlign.center,
          style: TextStyle(fontFamily: 'Cairo', fontSize: 13)),
    );
  }

  Future<void> _handleAction(AttendanceManager provider, bool isCheckIn) async {
    HapticFeedback.heavyImpact();

    // Use automatic GPS location (no map picker for attendance)
    final success =
        isCheckIn ? await provider.doCheckIn() : await provider.doCheckOut();
    if (mounted) {
      if (success) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text(
                isCheckIn ? 'تم بدء العمل بنجاح - بالتوفيق!' : 'تم إنهاء العمل بنجاح - شكراً لك!',
                style: const TextStyle(fontFamily: 'Cairo')),
            backgroundColor: AppColors.success));
      } else {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text(
                provider.errorMessage ?? 'فشلت العملية، يرجى المحاولة مرة أخرى',
                style: const TextStyle(fontFamily: 'Cairo')),
            backgroundColor: AppColors.error));
      }
    }
  }

  Future<void> _handleLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.cardBackground,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Text('تسجيل الخروج',
            textAlign: TextAlign.right,
            style: TextStyle(fontFamily: 'Cairo', fontWeight: FontWeight.bold)),
        content: const Text('هل أنت متأكد من رغبتك في تسجيل الخروج من التطبيق؟',
            textAlign: TextAlign.right, style: TextStyle(fontFamily: 'Cairo')),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8)),
            ),
            child: const Text('خروج', style: TextStyle(fontFamily: 'Cairo')),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      if (!context.mounted) return;

      // show loading spinner
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) =>
            const Center(child: CircularProgressIndicator(color: Colors.white)),
      );

      final authManager = context.read<AuthManager>();
      await authManager.doLogout();

      if (!context.mounted) return;

      final navigator = Navigator.of(context);

      // close loading
      navigator.pop();

      // go to login page
      navigator.pushNamedAndRemoveUntil(Routes.login, (route) => false);
    }
  }
}
