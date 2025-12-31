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

// global navigator key for deep linking from notifications
final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

void main() async {
  // 1. make sure flutter is ready
  WidgetsFlutterBinding.ensureInitialized();

  // 3. start the background services and database
  await _initializeServices();

  // 4. set the status bar style
  SystemChrome.setSystemUIOverlayStyle(
    const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
      statusBarBrightness: Brightness.dark,
    ),
  );

  // 5. lock the phone to vertical mode
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);

  // 6. run the app
  runApp(const JawlahApp());
}

Future<void> _initializeServices() async {
  try {
    // 1. start the small local storage
    await StorageHelper.startStorage();

    // 2. start Hive for offline mode
    await HiveInit.initialize();

    // 3. set up our API client
    ApiService().setUpApi();
    await ApiService().loadToken();

    // 4. initialize Firebase with the generated options
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

class JawlahApp extends StatelessWidget {
  const JawlahApp({super.key});

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
      ],
      child: Directionality(
        textDirection: TextDirection.rtl,
        child: MaterialApp(
          navigatorKey: navigatorKey,
          title: 'جولة - Jawlah',
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
