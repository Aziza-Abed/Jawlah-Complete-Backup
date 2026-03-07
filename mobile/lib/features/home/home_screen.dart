import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../presentation/widgets/offline_banner.dart';
import '../../presentation/widgets/not_checked_in_banner.dart';
import '../../presentation/widgets/fade_in_animation.dart';
import '../../core/utils/sync_toast_helper.dart';
import '../../providers/attendance_manager.dart';
import '../../providers/task_manager.dart';
import '../../providers/sync_manager.dart';
import '../../providers/notice_manager.dart';
import '../../core/routing/app_router.dart';
import '../../data/services/battery_service.dart';
import '../../data/services/location_service.dart';
import '../../data/services/tracking_service.dart';
import 'widgets/greeting_card.dart';
import 'widgets/sync_status_card.dart';
import 'widgets/attendance_card.dart';
import 'widgets/tasks_summary_card.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  Timer? _workDurationTimer;
  AttendanceManager? _attendanceManager;

  @override
  void initState() {
    super.initState();

    // load all data when screen starts
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      // Request GPS permission from foreground UI — background service cannot show dialogs
      await LocationService.requestPermissions();

      // Request battery optimization exemption so Android doesn't kill background tracking
      await BatteryService().requestBatteryOptimizationExemption();

      if (!mounted) return;

      final syncManager = context.read<SyncManager>();
      _attendanceManager = context.read<AttendanceManager>();

      _attendanceManager!.setSyncManager(syncManager);

      _attendanceManager!.loadTodayRecord();
      _attendanceManager!.listenToBackgroundUpdates();
      context.read<TaskManager>().loadTasks();
      context.read<NoticeManager>().loadNotices();
    });

    // progress tracking and other services already initialized in providers

    // update work time every minute
    _workDurationTimer = Timer.periodic(const Duration(minutes: 1), (timer) {
      if (mounted) {
        setState(() {});
      }
    });
  }

  @override
  void dispose() {
    _workDurationTimer?.cancel();
    _attendanceManager?.cancelBackgroundListener();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'FollowUp',
      showBackButton: false,
      actions: [
        Stack(
          children: [
            IconButton(
              icon: const Icon(
                Icons.notifications_outlined,
                size: 28,
                color: Colors.white,
              ),
              onPressed: () =>
                  Navigator.pushNamed(context, Routes.notifications),
            ),
            Consumer<NoticeManager>(
              builder: (context, provider, child) {
                if (provider.newNoticesCount == 0) {
                  return const SizedBox.shrink();
                }
                return Positioned(
                  right: 8,
                  top: 8,
                  child: Container(
                    padding: const EdgeInsets.all(2),
                    decoration: BoxDecoration(
                      color: Colors.red,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    constraints: const BoxConstraints(
                      minWidth: 16,
                      minHeight: 16,
                    ),
                    child: Text(
                      '${provider.newNoticesCount}',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                      ),
                      textAlign: TextAlign.center,
                    ),
                  ),
                );
              },
            ),
          ],
        ),
        Consumer<SyncManager>(
          builder: (context, connectivity, child) {
            return IconButton(
              onPressed: connectivity.isSyncingNow
                  ? null
                  : () => _handleSync(context, connectivity),
              icon: connectivity.isSyncingNow
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                        color: Colors.white,
                        strokeWidth: 2,
                      ),
                    )
                  : const Icon(Icons.sync, color: Colors.white, size: 28),
              tooltip: 'مزامنة البيانات',
            );
          },
        ),
      ],
      body: Column(
        children: [
          const OfflineBanner(),
          const NotCheckedInBanner(),
          ValueListenableBuilder<bool>(
            valueListenable: TrackingService.isBuffering,
            builder: (context, buffering, _) {
              if (!buffering) return const SizedBox.shrink();
              return Container(
                width: double.infinity,
                color: Colors.orange.shade700,
                padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 16),
                child: const Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    SizedBox(
                      width: 12,
                      height: 12,
                      child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                    ),
                    SizedBox(width: 8),
                    Text(
                      'تخزين الموقع مؤقتاً — سيُرسل عند الاتصال',
                      style: TextStyle(color: Colors.white, fontSize: 12, fontFamily: 'Cairo'),
                    ),
                  ],
                ),
              );
            },
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _handleRefresh,
              child: SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    children: [
                      FadeInAnimation(
                        delay: const Duration(milliseconds: 100),
                        child: const GreetingCard(),
                      ),
                      const SizedBox(height: 16),
                      FadeInAnimation(
                        delay: const Duration(milliseconds: 200),
                        child: const SyncStatusCard(),
                      ),
                      const SizedBox(height: 16),
                      FadeInAnimation(
                        delay: const Duration(milliseconds: 300),
                        child: const AttendanceCard(),
                      ),
                      const SizedBox(height: 16),
                      FadeInAnimation(
                        delay: const Duration(milliseconds: 400),
                        child: const TasksSummaryCard(),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _handleRefresh() async {
    await Future.wait([
      context.read<AttendanceManager>().refreshTodayData(),
      context.read<TaskManager>().refreshData(),
      context.read<NoticeManager>().loadNotices(),
      context.read<SyncManager>().startSync(),
    ]);
  }

  Future<void> _handleSync(
    BuildContext context,
    SyncManager connectivity,
  ) async {
    HapticFeedback.lightImpact();

    if (!connectivity.isOnline) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'لا يوجد اتصال بالإنترنت',
              style: TextStyle(fontFamily: 'Cairo'),
            ),
            backgroundColor: Colors.red,
          ),
        );
      }
      return;
    }

    final result = await connectivity.startSync();

    if (context.mounted) {
      showSyncResultToast(context, result);
    }
  }
}
