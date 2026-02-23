import 'package:flutter/foundation.dart';

/// Utility functions for formatting and parsing dates consistently across the app
class DateFormatter {
  /// Format date only (e.g., "19/1/2026")
  static String formatDate(DateTime date) {
    return '${date.day}/${date.month}/${date.year}';
  }

  /// Format date with time (e.g., "19/1/2026 08:05")
  static String formatDateTime(DateTime date) {
    return '${date.day}/${date.month}/${date.year} ${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
  }

  /// Format time only (e.g., "08:05")
  static String formatTime(DateTime date) {
    return '${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
  }

  /// Format time in 12-hour Arabic format (e.g., "2:30 م")
  static String formatTime12h(DateTime utcTime) {
    final local = utcTime.toLocal();
    final hour = local.hour > 12
        ? local.hour - 12
        : (local.hour == 0 ? 12 : local.hour);
    final min = local.minute.toString().padLeft(2, '0');
    return '$hour:$min ${local.hour < 12 ? "ص" : "م"}';
  }

  /// Parse a date string as UTC - ensures Z suffix for correct parsing
  static DateTime parseUtc(String dateStr) {
    return DateTime.parse(dateStr.endsWith('Z') ? dateStr : '${dateStr}Z');
  }

  /// Safe parse that handles nulls and malformed dates gracefully
  static DateTime? tryParseUtc(dynamic value) {
    if (value == null) return null;
    try {
      final str = value.toString();
      if (str.isEmpty) return null;
      final utcStr = str.endsWith('Z') ? str : '${str}Z';
      return DateTime.parse(utcStr);
    } catch (e) {
      if (kDebugMode) debugPrint('Failed to parse DateTime: $value - $e');
      return null;
    }
  }
}
