/// Utility functions for formatting dates consistently across the app
class DateFormatter {
  /// Format date only (e.g., "19/1/2026")
  static String formatDate(DateTime date) {
    return '${date.day}/${date.month}/${date.year}';
  }

  /// Format date with time (e.g., "19/1/2026 14:30")
  static String formatDateTime(DateTime date) {
    return '${date.day}/${date.month}/${date.year} ${date.hour}:${date.minute.toString().padLeft(2, '0')}';
  }

  /// Format time only (e.g., "14:30")
  static String formatTime(DateTime date) {
    return '${date.hour}:${date.minute.toString().padLeft(2, '0')}';
  }
}
