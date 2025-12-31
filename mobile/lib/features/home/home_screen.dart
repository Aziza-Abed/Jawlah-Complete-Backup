import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../presentation/widgets/offline_banner.dart';
import '../../presentation/widgets/fade_in_animation.dart';
import '../../providers/attendance_manager.dart';
import '../../providers/task_manager.dart';
import '../../providers/sync_manager.dart';
import '../../providers/notice_manager.dart';
import '../../core/routing/app_router.dart';
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

  @override
  void initState() {
    super.initState();

    // when the screen starts, prepare the managers and load data
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final syncManager = context.read<SyncManager>();
      context.read<AttendanceManager>().setSyncManager(syncManager);

      context.read<AttendanceManager>().loadTodayRecord();
      context.read<TaskManager>().loadTasks();
      context.read<NoticeManager>().loadNotices();
    });

    // refresh the work duration timer every minute
    _workDurationTimer = Timer.periodic(const Duration(minutes: 1), (timer) {
      if (mounted) {
        setState(() {});
      }
    });
  }

  @override
  void dispose() {
    _workDurationTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'جولة',
      showBackButton: false,
      actions: [
        Stack(
          children: [
            IconButton(
              icon: const Icon(Icons.notifications_outlined,
                  size: 28, color: Colors.white),
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
                  : const Icon(
                      Icons.sync,
                      color: Colors.white,
                      size: 28,
                    ),
              tooltip: 'مزامنة البيانات',
            );
          },
        ),
      ],
      body: Column(
        children: [
          const OfflineBanner(),
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
      BuildContext context, SyncManager connectivity) async {
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
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            result.success
                ? 'تمت المزامنة بنجاح'
                : 'فشلت المزامنة: ${result.errorMessage}',
            style: const TextStyle(fontFamily: 'Cairo'),
          ),
          backgroundColor: result.success ? Colors.green : Colors.red,
        ),
      );
    }
  }
}
