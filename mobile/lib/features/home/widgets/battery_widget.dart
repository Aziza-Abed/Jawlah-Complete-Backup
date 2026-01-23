import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/battery_provider.dart';

/// Widget to display current battery level with visual indicator
class BatteryWidget extends StatelessWidget {
  const BatteryWidget({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<BatteryProvider>(
      builder: (context, batteryProvider, child) {
        final level = batteryProvider.batteryLevel;
        final isCharging = batteryProvider.isCharging;
        final isLowBattery = batteryProvider.isLowBattery;

        Color getBatteryColor() {
          if (isCharging) return const Color(0xFF4CAF50);
          if (isLowBattery) return const Color(0xFFE53935);
          if (level <= 50) return const Color(0xFFFFA726);
          return const Color(0xFF4CAF50);
        }

        IconData getBatteryIcon() {
          if (isCharging) return Icons.bolt;
          if (level <= 20) return Icons.battery_alert;
          if (level <= 50) return Icons.battery_3_bar;
          return Icons.battery_full;
        }

        final color = getBatteryColor();

        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
          decoration: BoxDecoration(
            color: isLowBattery && !isCharging
                ? color.withOpacity(0.1)
                : const Color(0xFFF9F8F5),
            borderRadius: BorderRadius.circular(15),
            border: Border.all(
              color:
                  isLowBattery && !isCharging ? color : const Color(0xFFE0E0E0),
              width: 1,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(getBatteryIcon(), size: 16, color: color),
              const SizedBox(width: 4),
              Text(
                '$level%',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  color: color,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
