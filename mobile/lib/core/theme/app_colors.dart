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
  static const Color statusRejected = accentOrange;

  // Priority colors using the palette
  static const Color priorityLow = primaryBlue;
  static const Color priorityMedium = accentOrange;
  static const Color priorityHigh = accentOrange;

  // Semantic colors
  static const Color success = oliveGreen;
  static const Color error = accentOrange;
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

  // Get color for status
  static Color getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'pending':
      case 'new':
      case 'جديد':
        return statusNew;
      case 'inprogress':
      case 'قيد التنفيذ':
        return statusInProgress;
      case 'completed':
      case 'مكتملة':
        return statusCompleted;
      case 'cancelled':
      case 'rejected':
      case 'ملغاة':
      case 'مرفوضة':
        return statusRejected;
      default:
        return mainText;
    }
  }

  // Get label for status
  static String getStatusLabel(String status) {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'جديد';
      case 'inprogress':
        return 'قيد التنفيذ';
      case 'completed':
        return 'مكتملة';
      case 'cancelled':
        return 'ملغاة';
      case 'rejected':
        return 'مرفوضة';
      default:
        return status;
    }
  }

  // Get color for priority
  static Color getPriorityColor(String priority) {
    switch (priority.toLowerCase()) {
      case 'low':
      case 'منخفضة':
        return priorityLow;
      case 'medium':
      case 'متوسطة':
        return priorityMedium;
      case 'high':
      case 'عالية':
        return priorityHigh;
      default:
        return secondaryText;
    }
  }
}
