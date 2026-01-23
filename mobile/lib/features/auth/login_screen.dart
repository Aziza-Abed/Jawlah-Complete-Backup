import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:provider/provider.dart';
import '../../core/routing/app_router.dart';
import '../../core/theme/app_colors.dart';
import '../../providers/auth_manager.dart';
import 'otp_verification_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _rememberMe = false;
  bool _isLoading = false;
  bool _obscurePassword = true;
  String _loadingMessage = 'جاري تسجيل الدخول...';

  @override
  void initState() {
    super.initState();
    _loadSavedCredentials();
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _loadSavedCredentials() async {
    try {
      final authProvider = context.read<AuthManager>();
      final rememberMe = await authProvider.getRememberMe();
      if (rememberMe) {
        final savedUsername = await authProvider
            .getSavedEmployeeId(); // Will rename this method later
        if (savedUsername != null && savedUsername.isNotEmpty) {
          setState(() {
            _usernameController.text = savedUsername;
            _rememberMe = true;
          });
        }
      }
    } catch (e) {
      debugPrint('Error loading saved credentials: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        height: double.infinity,
        width: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              AppColors.mainBackground,
              Color(0xFFE8E6E1), // slightly darker shade of background
            ],
          ),
        ),
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
              child: Form(
                key: _formKey,
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    _LogoFloater(
                      child: Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.white.withOpacity(0.5),
                          shape: BoxShape.circle,
                        ),
                        child: Image.asset(
                          'assets/images/logo.png',
                          width: 100,
                          height: 100,
                          errorBuilder: (context, error, stackTrace) {
                            return const Icon(
                              Icons.account_balance,
                              size: 100,
                              color: AppColors.primary,
                            );
                          },
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    const Text(
                      'أهلاً بك في FollowUp',
                      style: TextStyle(
                        fontSize: 28,
                        fontWeight: FontWeight.bold,
                        color: AppColors.mainText,
                        fontFamily: 'Cairo',
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'سجل دخولك للمتابعة مع نظام FollowUp',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 16,
                        color: AppColors.secondaryText,
                        fontFamily: 'Cairo',
                      ),
                    ),
                    const SizedBox(height: 40),
                    Container(
                      padding: const EdgeInsets.all(24),
                      decoration: BoxDecoration(
                        color: AppColors.cardBackground,
                        borderRadius: BorderRadius.circular(24),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.04),
                            blurRadius: 20,
                            offset: const Offset(0, 10),
                          ),
                        ],
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Username field
                          const Text(
                            'اسم المستخدم',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: AppColors.secondaryText,
                              fontFamily: 'Cairo',
                            ),
                          ),
                          const SizedBox(height: 12),
                          TextFormField(
                            controller: _usernameController,
                            keyboardType: TextInputType.text,
                            textDirection: TextDirection.ltr,
                            textAlign: TextAlign.right,
                            autofocus: _usernameController.text.isEmpty,
                            style: const TextStyle(
                              fontSize: 16,
                              color: AppColors.mainText,
                              fontFamily: 'Cairo',
                            ),
                            decoration: InputDecoration(
                              hintText: 'أدخل اسم المستخدم',
                              hintStyle: TextStyle(
                                color: AppColors.secondaryText.withOpacity(0.5),
                              ),
                              filled: true,
                              fillColor: const Color(0xFFF3F1ED),
                              border: OutlineInputBorder(
                                borderRadius: BorderRadius.circular(16),
                                borderSide: BorderSide.none,
                              ),
                              prefixIcon: const Icon(
                                Icons.person_outline_rounded,
                                color: AppColors.primary,
                              ),
                            ),
                            validator: (value) {
                              if (value == null || value.trim().isEmpty) {
                                return 'يرجى إدخال اسم المستخدم';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 20),
                          // Password field
                          const Text(
                            'كلمة المرور',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: AppColors.secondaryText,
                              fontFamily: 'Cairo',
                            ),
                          ),
                          const SizedBox(height: 12),
                          TextFormField(
                            controller: _passwordController,
                            obscureText: _obscurePassword,
                            keyboardType: TextInputType.text,
                            textDirection: TextDirection.ltr,
                            textAlign: TextAlign.right,
                            style: const TextStyle(
                              fontSize: 16,
                              color: AppColors.mainText,
                              fontFamily: 'Cairo',
                            ),
                            decoration: InputDecoration(
                              hintText: 'أدخل كلمة المرور',
                              hintStyle: TextStyle(
                                color: AppColors.secondaryText.withOpacity(0.5),
                              ),
                              filled: true,
                              fillColor: const Color(0xFFF3F1ED),
                              border: OutlineInputBorder(
                                borderRadius: BorderRadius.circular(16),
                                borderSide: BorderSide.none,
                              ),
                              prefixIcon: const Icon(
                                Icons.lock_outline_rounded,
                                color: AppColors.primary,
                              ),
                              suffixIcon: IconButton(
                                icon: Icon(
                                  _obscurePassword
                                      ? Icons.visibility_off_outlined
                                      : Icons.visibility_outlined,
                                  color: AppColors.secondaryText,
                                ),
                                onPressed: () {
                                  setState(() {
                                    _obscurePassword = !_obscurePassword;
                                  });
                                },
                              ),
                            ),
                            validator: (value) {
                              if (value == null || value.trim().isEmpty) {
                                return 'يرجى إدخال كلمة المرور';
                              }
                              return null;
                            },
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 20),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Checkbox(
                          value: _rememberMe,
                          onChanged: (value) {
                            setState(() => _rememberMe = value ?? false);
                          },
                          activeColor: AppColors.primary,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(4),
                          ),
                        ),
                        const SizedBox(width: 4),
                        const Text(
                          'تذكر بياناتي',
                          style: TextStyle(
                            fontSize: 15,
                            color: AppColors.secondaryText,
                            fontFamily: 'Cairo',
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 32),
                    Container(
                      width: double.infinity,
                      height: 60,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(16),
                        boxShadow: [
                          BoxShadow(
                            color: AppColors.primary.withOpacity(0.3),
                            blurRadius: 12,
                            offset: const Offset(0, 6),
                          ),
                        ],
                      ),
                      child: ElevatedButton(
                        onPressed: _isLoading ? null : _login,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                          ),
                          elevation: 0,
                        ),
                        child: _isLoading
                            ? Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  const SizedBox(
                                    width: 20,
                                    height: 20,
                                    child: CircularProgressIndicator(
                                      color: Colors.white,
                                      strokeWidth: 2,
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Text(
                                    _loadingMessage,
                                    style: const TextStyle(
                                      fontSize: 14,
                                      fontFamily: 'Cairo',
                                    ),
                                  ),
                                ],
                              )
                            : Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  const Text(
                                    'دخول للنظام',
                                    style: TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.bold,
                                      fontFamily: 'Cairo',
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  const Icon(Icons.arrow_forward_rounded),
                                ],
                              ),
                      ),
                    ),
                    const SizedBox(height: 32),
                    _buildInfoNote(),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildInfoNote() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: AppColors.primary.withOpacity(0.05),
        borderRadius: BorderRadius.circular(12),
      ),
      child: const Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.info_outline_rounded, size: 18, color: AppColors.primary),
          SizedBox(width: 8),
          Text(
            'تأكد من تفعيل الـ GPS بالهاتف',
            style: TextStyle(
              fontSize: 13,
              color: AppColors.secondaryText,
              fontFamily: 'Cairo',
            ),
          ),
        ],
      ),
    );
  }

  // Get current location with timeout
  Future<Position?> _getLocation() async {
    try {
      // Check location permission
      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) {
          return null;
        }
      }

      if (permission == LocationPermission.deniedForever) {
        return null;
      }

      // Check if location service is enabled
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        return null;
      }

      // Get position with timeout
      return await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
        timeLimit: const Duration(seconds: 10),
      );
    } catch (e) {
      debugPrint('Error getting location: $e');
      return null;
    }
  }

  // Login to the app with optional auto check-in
  Future<void> _login() async {
    // check form is valid
    if (!(_formKey.currentState?.validate() ?? false)) return;

    setState(() {
      _isLoading = true;
      _loadingMessage = 'جاري تحديد الموقع...';
    });

    try {
      final username = _usernameController.text.trim();
      final password = _passwordController.text.trim();
      final authProvider = context.read<AuthManager>();

      // Try to get location for auto check-in
      final position = await _getLocation();

      if (!mounted) return;

      setState(() {
        _loadingMessage = 'جاري تسجيل الدخول...';
      });

      // Try to login with location - Now passing username and password
      final success = await authProvider.doLogin(
        username,
        password: password, // NEW: password parameter
        latitude: position?.latitude,
        longitude: position?.longitude,
        accuracy: position?.accuracy,
      );

      if (!mounted) return;

      if (success) {
        // Check if OTP is required (Two-Factor Authentication)
        if (authProvider.requiresOtp) {
          // Navigate to OTP verification screen
          if (mounted) {
            setState(() => _isLoading = false);
            Navigator.of(context).push(
              MaterialPageRoute(
                builder: (context) => OtpVerificationScreen(
                  sessionToken: authProvider.otpSessionToken!,
                  maskedPhone: authProvider.otpMaskedPhone ?? '****',
                  username: username,
                  password: password,
                  latitude: position?.latitude,
                  longitude: position?.longitude,
                  accuracy: position?.accuracy,
                ),
              ),
            );
          }
          return;
        }

        // save username if remember me checked
        if (_rememberMe) {
          await authProvider.saveEmployeeId(username);
          await authProvider.setRememberMe(true);
        } else {
          await authProvider.clearSavedEmployeeId();
          await authProvider.setRememberMe(false);
        }

        // Check if check-in was successful or needs manual fallback
        if (authProvider.isCheckedIn) {
          // Show success message with lateness info if applicable
          if (authProvider.lastLoginIsLate) {
            _showWarningMessage(
              'تم تسجيل الدخول والحضور (متأخر ${authProvider.lastLoginLateMinutes} دقيقة)',
            );
          } else if (authProvider.lastLoginRequiresApproval) {
            _showWarningMessage(
              'تم تسجيل الدخول - الحضور بانتظار موافقة المشرف',
            );
          } else {
            _showSuccessMessage('تم تسجيل الدخول والحضور بنجاح');
          }

          // go to home
          if (mounted) {
            Navigator.of(context).pushReplacementNamed(Routes.home);
          }
        } else if (authProvider.checkInFailed) {
          // GPS check-in failed - offer manual check-in option
          if (mounted) {
            setState(() => _isLoading = false);
            final shouldUseManual = await _showGpsFailureDialog(
              authProvider.lastCheckInFailureReason ?? 'فشل تحديد الموقع',
            );

            if (shouldUseManual != null && shouldUseManual.isNotEmpty) {
              // Retry login with manual check-in
              setState(() {
                _isLoading = true;
                _loadingMessage = 'جاري تسجيل الحضور اليدوي...';
              });

              await authProvider.doLogin(
                username,
                password: password, // NEW: password parameter
                allowManualCheckIn: true,
                manualCheckInReason: shouldUseManual,
              );

              if (mounted) {
                _showWarningMessage(
                    'تم تسجيل الدخول - الحضور بانتظار موافقة المشرف');
                Navigator.of(context).pushReplacementNamed(Routes.home);
              }
            } else {
              // User cancelled - still logged in but without check-in
              if (mounted) {
                _showWarningMessage(
                    'تم تسجيل الدخول - يرجى تسجيل الحضور من الشاشة الرئيسية');
                Navigator.of(context).pushReplacementNamed(Routes.home);
              }
            }
          }
        } else {
          // No location provided - logged in but without check-in
          _showWarningMessage(
              'تم تسجيل الدخول - يرجى تسجيل الحضور للبدء بالعمل');
          if (mounted) {
            Navigator.of(context).pushReplacementNamed(Routes.home);
          }
        }
      } else {
        // show error
        _showErrorMessage(authProvider.errorMessage ?? 'فشل تسجيل الدخول');
      }
    } catch (e) {
      if (mounted) {
        _showErrorMessage('حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى');
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  // Show dialog when GPS check-in fails
  Future<String?> _showGpsFailureDialog(String failureReason) async {
    final reasonController = TextEditingController();

    return showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.location_off, color: AppColors.warning),
            SizedBox(width: 8),
            Expanded(
              child: Text(
                'فشل تسجيل الحضور التلقائي',
                style: TextStyle(
                  fontFamily: 'Cairo',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              failureReason,
              style: const TextStyle(
                fontFamily: 'Cairo',
                color: AppColors.secondaryText,
              ),
            ),
            const SizedBox(height: 16),
            const Text(
              'يمكنك إدخال سبب للحضور اليدوي (يتطلب موافقة المشرف):',
              style: TextStyle(
                fontFamily: 'Cairo',
                fontSize: 13,
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: reasonController,
              maxLines: 2,
              decoration: InputDecoration(
                hintText: 'مثال: GPS لا يعمل في الموقع',
                hintStyle: const TextStyle(fontFamily: 'Cairo', fontSize: 13),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
              ),
              style: const TextStyle(fontFamily: 'Cairo'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(null),
            child: const Text(
              'تخطي',
              style: TextStyle(fontFamily: 'Cairo'),
            ),
          ),
          ElevatedButton(
            onPressed: () {
              final reason = reasonController.text.trim();
              if (reason.isEmpty) {
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text(
                      'يرجى إدخال سبب الحضور اليدوي',
                      style: TextStyle(fontFamily: 'Cairo'),
                    ),
                  ),
                );
                return;
              }
              Navigator.of(context).pop(reason);
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
            ),
            child: const Text(
              'تسجيل يدوي',
              style: TextStyle(fontFamily: 'Cairo', color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }

  void _showErrorMessage(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          style: const TextStyle(fontFamily: 'Cairo'),
        ),
        backgroundColor: AppColors.error,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }

  void _showSuccessMessage(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          style: const TextStyle(fontFamily: 'Cairo'),
        ),
        backgroundColor: AppColors.success,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }

  void _showWarningMessage(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          style: const TextStyle(fontFamily: 'Cairo'),
        ),
        backgroundColor: AppColors.warning,
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 4),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }
}

// Simple wrapper widget - just displays the child (no animation needed)
class _LogoFloater extends StatelessWidget {
  final Widget child;
  const _LogoFloater({required this.child});

  @override
  Widget build(BuildContext context) {
    return child;
  }
}
