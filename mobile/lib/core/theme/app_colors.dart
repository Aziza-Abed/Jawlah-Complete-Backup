import 'package:flutter/material.dart';

class AppColors {
  AppColors._();

  // Main palette from image 2
  static const Color primaryBlue = Color(0xFF7895B2); // #7895B2
  static const Color oliveGreen = Color(0xFFA3B18A); // #A3B18A
  static const Color mainBackground = Color(0xFFF3F1ED); // #F3F1ED
  static const Color cardBackground = Color(0xFFF9F8F5); // #F9F8F5
  static const Color mainText = Color(0xFF2F2F2F); // #2F2F2F
  static const Color secondaryText = Color(0xFF6C757D); // #6C757D
  static const Color accentOrange = Color(0xFFC97A63); // #C97A63

  // Status colors using the palette
  static const Color statusNew = primaryBlue;
  static const Color statusInProgress = accentOrange;
  static const Color statusCompleted = oliveGreen;
  static const Color statusRejected = Color(0xFFB74C3B); // distinct red — not same as inProgress
  static const Color statusUnderReview = primaryBlue;

  // Priority colors using the palette
  static const Color priorityLow = primaryBlue;
  static const Color priorityMedium = accentOrange;
  static const Color priorityHigh = Color(0xFFB74C3B); // darker red for high priority

  // Semantic colors
  static const Color success = oliveGreen;
  static const Color error = Color(0xFFB74C3B); // distinct red — not same as warning
  static const Color warning = accentOrange;
  static const Color info = primaryBlue;

  // Basic colors
  static const Color white = Color(0xFFFFFFFF);
  static const Color black = Color(0xFF000000);

  // Aliases for consistency
  static const Color primary = primaryBlue;
  static const Color background = mainBackground;
  static const Color textPrimary = mainText;
  static const Color textSecondary = secondaryText;

}
