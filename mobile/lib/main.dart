import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:provider/provider.dart';
import 'package:firebase_core/firebase_core.dart';
import 'firebase_options.dart';
import 'core/theme/app_theme.dart';
import 'core/routing/app_router.dart';
import 'core/utils/storage_helper.dart';
import 'core/utils/hive_init.dart';
import 'core/utils/background_service_utils.dart';
import 'data/services/api_service.dart';
import 'data/services/auth_service.dart';
import 'data/services/storage_service.dart';
import 'data/services/firebase_messaging_service.dart';
import 'data/services/sync/sync_service.dart';
import 'data/services/tasks_service.dart';
import 'data/services/issues_service.dart';
import 'data/repositories/local/attendance_local_repository.dart';
import 'data/repositories/local/task_local_repository.dart';
import 'data/repositories/local/issue_local_repository.dart';
import 'providers/auth_manager.dart';
import 'providers/task_manager.dart';
import 'providers/attendance_manager.dart';
import 'providers/issue_manager.dart';
import 'providers/notice_manager.dart';
import 'providers/sync_manager.dart';
import 'providers/battery_provider.dart';
import 'providers/appeal_manager.dart';

// global navigator key for deep linking from notifications
final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

void main() async {
  // ensure bindings are ready
  WidgetsFlutterBinding.ensureInitialized();

  // init services and db
  await _initializeServices();

  // helper to set status bar style
  SystemChrome.setSystemUIOverlayStyle(
    const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
      statusBarBrightness: Brightness.dark,
    ),
  );

  // lock orientation to portrait
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);

  // start app
  runApp(const FollowUpApp());
}

Future<void> _initializeServices() async {
  try {
    // init secure storage
    await StorageHelper.startStorage();

    // init hive
    await HiveInit.initialize();

    // setup api
    ApiService().setUpApi();
    await ApiService().loadToken();

    // init firebase
    try {
      await Firebase.initializeApp(
        options: DefaultFirebaseOptions.currentPlatform,
      );
      await FirebaseMessagingService().initialize();
      debugPrint('Firebase initialized successfully');
    } catch (e) {
      debugPrint(
          'Firebase initialization error (will continue without notifications): $e');
    }

    // initialize Background Tracking Service
    await BackgroundServiceUtils.initializeService();
  } catch (e) {
    debugPrint('Error initializing services: $e');
  }
}

class FollowUpApp extends StatelessWidget {
  const FollowUpApp({super.key});

  @override
  Widget build(BuildContext context) {
    // create service instances
    final authService = AuthService();
    final storageService = StorageService();

    // create local repositories for offline mode
    final attendanceLocalRepo = AttendanceLocalRepository();
    final taskLocalRepo = TaskLocalRepository();
    final issueLocalRepo = IssueLocalRepository();

    // create sync service
    final syncService = SyncService(
      ApiService().dio,
      attendanceLocalRepo,
      taskLocalRepo,
      issueLocalRepo,
      TasksService(),
      IssuesService(),
    );

    return MultiProvider(
      providers: [
        // setup providers with services
        ChangeNotifierProvider(
          create: (_) => AuthManager(authService, storageService),
        ),
        ChangeNotifierProvider(
          create: (_) => SyncManager(syncService),
        ),
        ChangeNotifierProxyProvider<SyncManager, TaskManager>(
          create: (_) => TaskManager(),
          update: (_, syncManager, tasks) =>
              tasks!..setSyncManager(syncManager),
        ),
        ChangeNotifierProxyProvider<SyncManager, AttendanceManager>(
          create: (_) => AttendanceManager(),
          update: (_, syncManager, attendance) =>
              attendance!..setSyncManager(syncManager),
        ),
        ChangeNotifierProxyProvider<SyncManager, IssueManager>(
          create: (_) => IssueManager(),
          update: (_, syncManager, issues) =>
              issues!..setSyncManager(syncManager),
        ),
        ChangeNotifierProvider(
          create: (_) => NoticeManager(),
        ),
        ChangeNotifierProvider(
          create: (_) => BatteryProvider()..initialize(),
        ),
        ChangeNotifierProvider(
          create: (_) => AppealManager(),
        ),
      ],
      child: Directionality(
        textDirection: TextDirection.rtl,
        child: MaterialApp(
          navigatorKey: navigatorKey,
          title: 'FollowUp',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.theme,
          locale: const Locale('ar'),
          supportedLocales: const [
            Locale('ar'),
          ],
          localizationsDelegates: const [
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          initialRoute: Routes.splash,
          onGenerateRoute: AppRouter.generateRoute,
          builder: (context, child) {
            return child ?? const SizedBox.shrink();
          },
        ),
      ),
    );
  }
}
