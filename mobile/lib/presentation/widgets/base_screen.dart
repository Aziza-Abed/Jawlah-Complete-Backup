import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import 'app_header.dart';

class BaseScreen extends StatelessWidget {
  final String title;
  final bool showBackButton;
  final Widget body;
  final List<Widget>? actions;
  final Widget? bottomNavigationBar;

  const BaseScreen({
    super.key,
    required this.title,
    required this.body,
    this.showBackButton = true,
    this.actions,
    this.bottomNavigationBar,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.primary,
      resizeToAvoidBottomInset: true, // Important for keyboard
      body: Column(
        children: [
          AppHeader(
            title: title,
            showBackButton: showBackButton,
            actions: actions,
          ),
          Expanded(
            child: SafeArea(
              top: false,
              child: Container(
                decoration: const BoxDecoration(
                  color: AppColors.background,
                  borderRadius: BorderRadius.only(
                    topLeft: Radius.circular(24),
                    topRight: Radius.circular(24),
                  ),
                ),
                child: body, // Screen content goes here
              ),
            ),
          ),
        ],
      ),
      bottomNavigationBar: bottomNavigationBar,
    );
  }
}
