import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

// reusable gps location notice widget
class GpsNoticeWidget extends StatelessWidget {
  final String? customMessage;

  const GpsNoticeWidget({
    super.key,
    this.customMessage,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.info.withOpacity(0.1),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.info.withOpacity(0.3)),
      ),
      child: Row(
        children: [
          const Icon(
            Icons.location_on,
            color: AppColors.info,
            size: 20,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              customMessage ?? 'سيتم تسجيل موقعك تلقائياً عند الإرسال',
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.info,
                fontFamily: 'Cairo',
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
