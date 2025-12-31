import 'package:flutter/material.dart';

import '../../features/splash/splash_screen.dart';
import '../../features/auth/login_screen.dart';
import '../../features/profile/profile_screen.dart';
import '../../features/attendance/attendance_screen.dart';
import '../../features/tasks/tasks_list_screen.dart';
import '../../features/tasks/task_details_screen.dart';
import '../../features/issues/report_problem_screen.dart';
import '../../features/tasks/submit_evidence_screen.dart';
import '../../features/notifications/notifications_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../presentation/widgets/bottom_navigation.dart';

class Routes {
  Routes._();

  static const String splash = '/';
  static const String login = '/login';

  static const String home = '/home';

  static const String tasksList = '/tasks';
  static const String taskDetails = '/tasks/details';
  static const String taskProof = '/tasks/proof';

  static const String reportIssue = '/field/report';

  static const String notifications = '/notifications';

  static const String attendance = '/attendance';

  static const String profile = '/profile';
  static const String settings = '/settings';
}

class AppRouter {
  // this method decides which screen to show based on the route name
  static Route<dynamic> generateRoute(RouteSettings settings) {
    switch (settings.name) {
      case Routes.splash:
        return _buildRoute(
          const SplashScreen(),
          settings,
        );

      case Routes.login:
        return _buildRoute(
          const LoginScreen(),
          settings,
        );

      case Routes.home:
        return _buildRoute(
          const BottomNavigationScreen(),
          settings,
        );

      case Routes.tasksList:
        return _buildRoute(
          const TasksListScreen(),
          settings,
        );

      case Routes.taskDetails:
        return _buildRoute(
          const TaskDetailsScreen(),
          settings,
        );

      case Routes.taskProof:
        return _buildRoute(
          const SubmitEvidenceScreen(),
          settings,
        );

      case Routes.reportIssue:
        return _buildRoute(
          const ReportProblemScreen(),
          settings,
        );

      case Routes.notifications:
        return _buildRoute(
          const NotificationsScreen(),
          settings,
        );

      case Routes.attendance:
        return _buildRoute(
          const AttendanceScreen(),
          settings,
        );

      case Routes.profile:
        return _buildRoute(
          const ProfileScreen(),
          settings,
        );

      case Routes.settings:
        return _buildRoute(
          const SettingsScreen(),
          settings,
        );

      default:
        return MaterialPageRoute(
          builder: (context) => Scaffold(
            appBar: AppBar(
              title: Text('خطأ'),
            ),
            body: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 64, color: Colors.red),
                  const SizedBox(height: 16),
                  Text(
                    "${'الصفحة غير موجودة'}: ${settings.name}",
                    style: const TextStyle(fontSize: 18, color: Colors.red),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 24),
                  ElevatedButton(
                    onPressed: () =>
                        Navigator.of(context).pushReplacementNamed(Routes.home),
                    child: Text('العودة للصفحة الرئيسية'),
                  ),
                ],
              ),
            ),
          ),
          settings: settings,
        );
    }
  }

  static MaterialPageRoute _buildRoute(Widget screen, RouteSettings settings) {
    return MaterialPageRoute(
      builder: (_) => screen,
      settings: settings,
    );
  }
}
