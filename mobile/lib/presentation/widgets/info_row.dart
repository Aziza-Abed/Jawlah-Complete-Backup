import 'package:flutter/material.dart';

/// Reusable widget for displaying label-value pairs in a row
class InfoRow extends StatelessWidget {
  final String label;
  final String value;
  final double fontSize;

  const InfoRow({
    super.key,
    required this.label,
    required this.value,
    this.fontSize = 15,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            fontSize: fontSize,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            value,
            style: TextStyle(fontSize: fontSize),
          ),
        ),
      ],
    );
  }
}
