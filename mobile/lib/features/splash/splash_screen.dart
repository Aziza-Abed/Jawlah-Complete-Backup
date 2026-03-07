import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/routing/app_router.dart';
import '../../core/theme/app_colors.dart';
import '../../providers/auth_manager.dart';
import '../../providers/task_manager.dart';
import '../../providers/issue_manager.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  bool _visible = false;

  @override
  void initState() {
    super.initState();
    // start animation after delay
    Future.delayed(const Duration(milliseconds: 100), () {
      if (mounted) setState(() => _visible = true);
    });
    _navigateToHome();
  }

  Future<void> _navigateToHome() async {
    final authManager = context.read<AuthManager>();

    // Register logout callbacks so provider caches are cleared on logout
    authManager.addLogoutCallback(() => context.read<TaskManager>().clearCache());
    authManager.addLogoutCallback(() => context.read<IssueManager>().clearCache());

    // wait for both: minimum splash display (1.5s) AND session restore to finish
    await Future.wait([
      Future.delayed(const Duration(milliseconds: 1500)),
      authManager.sessionRestored,
    ]);

    if (!mounted) return;

    if (authManager.token != null) {
      Navigator.of(context).pushReplacementNamed(Routes.home);
    } else {
      Navigator.of(context).pushReplacementNamed(Routes.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        width: double.infinity,
        height: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              AppColors.primary,
              Color(0xFF5D738A), // darker variation of primary blue
            ],
          ),
        ),
        child: AnimatedOpacity(
          opacity: _visible ? 1.0 : 0.0,
          duration: const Duration(milliseconds: 800),
          child: AnimatedScale(
            scale: _visible ? 1.0 : 0.8,
            duration: const Duration(milliseconds: 800),
            curve: Curves.easeOutBack,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                // app logo
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.white.withOpacity(0.12),
                    shape: BoxShape.circle,
                  ),
                  child: Image.asset(
                    'assets/images/logo.png',
                    width: 140,
                    height: 140,
                    errorBuilder: (context, error, stackTrace) => const Icon(
                      Icons.account_balance,
                      size: 140,
                      color: Colors.white,
                    ),
                  ),
                ),
                const SizedBox(height: 32),
                const Text(
                  'FollowUp',
                  style: TextStyle(
                    fontSize: 42,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                    fontFamily: 'Cairo',
                    letterSpacing: 2,
                  ),
                ),
                const SizedBox(height: 48),
                const SizedBox(
                  width: 30,
                  height: 30,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    valueColor: AlwaysStoppedAnimation<Color>(Colors.white70),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
