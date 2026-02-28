import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
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
        final savedUsername = await authProvider.getSavedEmployeeId();
        if (!mounted) return;
        if (savedUsername != null && savedUsername.isNotEmpty) {
          setState(() {
            _usernameController.text = savedUsername;
            _rememberMe = true;
          });
        }
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Error loading saved credentials: $e');
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
              Color(0xFFE8E6E1),
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
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: Colors.white.withOpacity(0.6),
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
                        borderRadius: BorderRadius.circular(20),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.06),
                            blurRadius: 16,
                            offset: const Offset(0, 6),
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
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        // Forgot password link
                        TextButton(
                          onPressed: () {
                            Navigator.of(context)
                                .pushNamed(Routes.forgotPassword);
                          },
                          child: const Text(
                            'نسيت كلمة المرور؟',
                            style: TextStyle(
                              fontSize: 13,
                              color: AppColors.primary,
                              fontFamily: 'Cairo',
                            ),
                          ),
                        ),
                        // Remember me
                        Row(
                          mainAxisSize: MainAxisSize.min,
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
                            ? const Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  SizedBox(
                                    width: 20,
                                    height: 20,
                                    child: CircularProgressIndicator(
                                      color: Colors.white,
                                      strokeWidth: 2,
                                    ),
                                  ),
                                  SizedBox(width: 12),
                                  Text(
                                    'جاري تسجيل الدخول...',
                                    style: TextStyle(
                                      fontSize: 14,
                                      fontFamily: 'Cairo',
                                    ),
                                  ),
                                ],
                              )
                            : const Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  Text(
                                    'دخول للنظام',
                                    style: TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.bold,
                                      fontFamily: 'Cairo',
                                    ),
                                  ),
                                  SizedBox(width: 12),
                                  Icon(Icons.arrow_forward_rounded),
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
            'تسجيل الحضور يتم تلقائياً عبر GPS',
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

  // simple login - authenticate only, no check-in
  Future<void> _login() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;

    setState(() {
      _isLoading = true;
    });

    try {
      final username = _usernameController.text.trim();
      final password = _passwordController.text.trim();
      final authProvider = context.read<AuthManager>();

      final success = await authProvider.doLogin(
        username,
        password: password,
      );

      if (!mounted) return;

      if (success) {
        // Check if OTP is required (Two-Factor Authentication)
        if (authProvider.requiresOtp) {
          setState(() => _isLoading = false);
          if (mounted) {
            Navigator.of(context).push(
              MaterialPageRoute(
                builder: (context) => OtpVerificationScreen(
                  sessionToken: authProvider.otpSessionToken!,
                  maskedPhone: authProvider.otpMaskedPhone ?? '****',
                  username: username,
                  rememberMe: _rememberMe,
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

        // Navigate to home
        _showSuccessMessage('تم تسجيل الدخول بنجاح');
        if (mounted) {
          Navigator.of(context).pushReplacementNamed(Routes.home);
        }
      } else {
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
}
