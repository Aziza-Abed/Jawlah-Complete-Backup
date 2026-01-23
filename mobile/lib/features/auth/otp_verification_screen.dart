import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../../core/routing/app_router.dart';
import '../../core/theme/app_colors.dart';
import '../../providers/auth_manager.dart';

/// OTP Verification Screen for Two-Factor Authentication
/// Displayed when login requires SMS OTP verification (Admin/Supervisor always, Worker on new device)
class OtpVerificationScreen extends StatefulWidget {
  final String sessionToken;
  final String maskedPhone;
  final String username;
  final String password;
  final double? latitude;
  final double? longitude;
  final double? accuracy;

  const OtpVerificationScreen({
    super.key,
    required this.sessionToken,
    required this.maskedPhone,
    required this.username,
    required this.password,
    this.latitude,
    this.longitude,
    this.accuracy,
  });

  @override
  State<OtpVerificationScreen> createState() => _OtpVerificationScreenState();
}

class _OtpVerificationScreenState extends State<OtpVerificationScreen> {
  final List<TextEditingController> _otpControllers = List.generate(
    6,
    (index) => TextEditingController(),
  );
  final List<FocusNode> _focusNodes = List.generate(6, (index) => FocusNode());

  bool _isLoading = false;
  bool _isResending = false;
  int _resendCooldown = 0;
  Timer? _cooldownTimer;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _startResendCooldown(60); // Initial 60 second cooldown
  }

  @override
  void dispose() {
    for (var controller in _otpControllers) {
      controller.dispose();
    }
    for (var node in _focusNodes) {
      node.dispose();
    }
    _cooldownTimer?.cancel();
    super.dispose();
  }

  void _startResendCooldown(int seconds) {
    setState(() {
      _resendCooldown = seconds;
    });

    _cooldownTimer?.cancel();
    _cooldownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_resendCooldown > 0) {
        setState(() {
          _resendCooldown--;
        });
      } else {
        timer.cancel();
      }
    });
  }

  String get _otpCode {
    return _otpControllers.map((c) => c.text).join();
  }

  bool get _isOtpComplete {
    return _otpCode.length == 6;
  }

  void _onOtpChanged(int index, String value) {
    setState(() {
      _errorMessage = null;
    });

    if (value.isNotEmpty && index < 5) {
      // Move to next field
      _focusNodes[index + 1].requestFocus();
    } else if (value.isEmpty && index > 0) {
      // Move to previous field on delete
      _focusNodes[index - 1].requestFocus();
    }

    // Auto-submit when all digits entered
    if (_isOtpComplete) {
      _verifyOtp();
    }
  }

  Future<void> _verifyOtp() async {
    if (!_isOtpComplete || _isLoading) return;

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final authManager = context.read<AuthManager>();
      final success = await authManager.verifyOtp(
        sessionToken: widget.sessionToken,
        otpCode: _otpCode,
        latitude: widget.latitude,
        longitude: widget.longitude,
        accuracy: widget.accuracy,
      );

      if (!mounted) return;

      if (success) {
        // Navigate to home
        Navigator.of(context).pushReplacementNamed(Routes.home);
      } else {
        setState(() {
          _errorMessage = authManager.errorMessage ?? 'رمز التحقق غير صحيح';
        });
        // Clear OTP fields on error
        for (var controller in _otpControllers) {
          controller.clear();
        }
        _focusNodes[0].requestFocus();
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'حدث خطأ. يرجى المحاولة مرة أخرى';
      });
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _resendOtp() async {
    if (_resendCooldown > 0 || _isResending) return;

    setState(() {
      _isResending = true;
      _errorMessage = null;
    });

    try {
      final authManager = context.read<AuthManager>();
      final result = await authManager.resendOtp(sessionToken: widget.sessionToken);

      if (!mounted) return;

      if (result != null) {
        _startResendCooldown(result.cooldownSeconds);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              result.message,
              style: const TextStyle(fontFamily: 'Cairo'),
            ),
            backgroundColor: AppColors.success,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
        );
      } else {
        setState(() {
          _errorMessage = authManager.errorMessage ?? 'فشل إعادة الإرسال';
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'فشل إعادة الإرسال. يرجى المحاولة لاحقاً';
      });
    } finally {
      if (mounted) {
        setState(() {
          _isResending = false;
        });
      }
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
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
            child: Column(
              children: [
                const SizedBox(height: 40),
                // Lock icon
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: AppColors.primary.withOpacity(0.1),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.lock_outline_rounded,
                    size: 60,
                    color: AppColors.primary,
                  ),
                ),
                const SizedBox(height: 32),
                const Text(
                  'التحقق بخطوتين',
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    color: AppColors.mainText,
                    fontFamily: 'Cairo',
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  'تم إرسال رمز التحقق إلى رقم الهاتف',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 16,
                    color: AppColors.secondaryText.withOpacity(0.8),
                    fontFamily: 'Cairo',
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  widget.maskedPhone,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                    color: AppColors.primary,
                    fontFamily: 'Cairo',
                    letterSpacing: 2,
                  ),
                ),
                const SizedBox(height: 40),
                // OTP Input Fields
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
                    children: [
                      const Text(
                        'أدخل رمز التحقق المكون من 6 أرقام',
                        style: TextStyle(
                          fontSize: 14,
                          color: AppColors.secondaryText,
                          fontFamily: 'Cairo',
                        ),
                      ),
                      const SizedBox(height: 24),
                      Directionality(
                        textDirection: TextDirection.ltr,
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                          children: List.generate(6, (index) {
                            return SizedBox(
                              width: 45,
                              height: 55,
                              child: TextField(
                                controller: _otpControllers[index],
                                focusNode: _focusNodes[index],
                                keyboardType: TextInputType.number,
                                textAlign: TextAlign.center,
                                maxLength: 1,
                                style: const TextStyle(
                                  fontSize: 24,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.mainText,
                                ),
                                decoration: InputDecoration(
                                  counterText: '',
                                  filled: true,
                                  fillColor: const Color(0xFFF3F1ED),
                                  border: OutlineInputBorder(
                                    borderRadius: BorderRadius.circular(12),
                                    borderSide: BorderSide.none,
                                  ),
                                  focusedBorder: OutlineInputBorder(
                                    borderRadius: BorderRadius.circular(12),
                                    borderSide: const BorderSide(
                                      color: AppColors.primary,
                                      width: 2,
                                    ),
                                  ),
                                ),
                                inputFormatters: [
                                  FilteringTextInputFormatter.digitsOnly,
                                ],
                                onChanged: (value) => _onOtpChanged(index, value),
                              ),
                            );
                          }),
                        ),
                      ),
                      if (_errorMessage != null) ...[
                        const SizedBox(height: 16),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 8,
                          ),
                          decoration: BoxDecoration(
                            color: AppColors.error.withOpacity(0.1),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const Icon(
                                Icons.error_outline,
                                color: AppColors.error,
                                size: 18,
                              ),
                              const SizedBox(width: 8),
                              Flexible(
                                child: Text(
                                  _errorMessage!,
                                  style: const TextStyle(
                                    color: AppColors.error,
                                    fontSize: 13,
                                    fontFamily: 'Cairo',
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: 32),
                // Verify Button
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
                    onPressed: (_isLoading || !_isOtpComplete) ? null : _verifyOtp,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.primary,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                      ),
                      elevation: 0,
                      disabledBackgroundColor: AppColors.primary.withOpacity(0.5),
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
                                'جاري التحقق...',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontFamily: 'Cairo',
                                ),
                              ),
                            ],
                          )
                        : const Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(Icons.verified_user_outlined),
                              SizedBox(width: 12),
                              Text(
                                'تأكيد',
                                style: TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                  fontFamily: 'Cairo',
                                ),
                              ),
                            ],
                          ),
                  ),
                ),
                const SizedBox(height: 24),
                // Resend OTP
                TextButton(
                  onPressed: (_resendCooldown > 0 || _isResending)
                      ? null
                      : _resendOtp,
                  child: _isResending
                      ? const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                              ),
                            ),
                            SizedBox(width: 8),
                            Text(
                              'جاري الإرسال...',
                              style: TextStyle(
                                fontFamily: 'Cairo',
                                fontSize: 14,
                              ),
                            ),
                          ],
                        )
                      : Text(
                          _resendCooldown > 0
                              ? 'إعادة إرسال الرمز ($_resendCooldown ثانية)'
                              : 'إعادة إرسال الرمز',
                          style: TextStyle(
                            fontFamily: 'Cairo',
                            fontSize: 14,
                            color: _resendCooldown > 0
                                ? AppColors.secondaryText
                                : AppColors.primary,
                            fontWeight: _resendCooldown > 0
                                ? FontWeight.normal
                                : FontWeight.bold,
                          ),
                        ),
                ),
                const SizedBox(height: 16),
                // Back to login
                TextButton.icon(
                  onPressed: () {
                    Navigator.of(context).pop();
                  },
                  icon: const Icon(
                    Icons.arrow_back_rounded,
                    size: 18,
                  ),
                  label: const Text(
                    'العودة لتسجيل الدخول',
                    style: TextStyle(
                      fontFamily: 'Cairo',
                      fontSize: 14,
                    ),
                  ),
                ),
                const SizedBox(height: 32),
                // Security note
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppColors.primary.withOpacity(0.05),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Row(
                    children: [
                      Icon(
                        Icons.security_rounded,
                        color: AppColors.primary,
                        size: 24,
                      ),
                      SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          'رمز التحقق صالح لمدة 5 دقائق فقط للحفاظ على أمان حسابك',
                          style: TextStyle(
                            fontSize: 12,
                            color: AppColors.secondaryText,
                            fontFamily: 'Cairo',
                          ),
                        ),
                      ),
                    ],
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
