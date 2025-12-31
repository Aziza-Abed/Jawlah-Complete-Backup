import 'dart:async';
import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';

class LiveWorkDuration extends StatefulWidget {
  final DateTime checkInTime;

  const LiveWorkDuration({super.key, required this.checkInTime});

  @override
  State<LiveWorkDuration> createState() => _LiveWorkDurationState();
}

class _LiveWorkDurationState extends State<LiveWorkDuration> {
  late Timer _timer;
  late String _durationStr;

  @override
  void initState() {
    super.initState();
    _updateDuration();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (mounted) {
        setState(() => _updateDuration());
      }
    });
  }

  void _updateDuration() {
    final diff = DateTime.now().difference(widget.checkInTime);
    final hours = diff.inHours;
    final minutes = diff.inMinutes.remainder(60);
    final seconds = diff.inSeconds.remainder(60);
    _durationStr =
        '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
  }

  @override
  void dispose() {
    _timer.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Text(
      'مدة العمل: $_durationStr',
      style: const TextStyle(
        fontSize: 16,
        fontWeight: FontWeight.w600,
        color: AppColors.primary,
        fontFamily: 'Cairo',
      ),
    );
  }
}
