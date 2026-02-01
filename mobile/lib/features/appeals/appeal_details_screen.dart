import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../data/models/appeal_model.dart';
import '../../core/utils/date_formatter.dart';
import '../../presentation/widgets/info_row.dart';

class AppealDetailsScreen extends StatelessWidget {
  final AppealModel appeal;

  const AppealDetailsScreen({
    super.key,
    required this.appeal,
  });

  @override
  Widget build(BuildContext context) {
    Color statusColor;
    switch (appeal.statusColor) {
      case 'green':
        statusColor = Colors.green;
        break;
      case 'red':
        statusColor = Colors.red;
        break;
      case 'orange':
        statusColor = Colors.orange;
        break;
      default:
        statusColor = Colors.grey;
    }

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('تفاصيل الطعن'),
          centerTitle: true,
        ),
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Status card
              _buildStatusCard(statusColor),
              const SizedBox(height: 16),

              // Task info card
              if (appeal.entityTitle != null) _buildTaskInfoCard(),
              const SizedBox(height: 16),

              // Location info card
              if (appeal.distanceMeters != null) _buildLocationInfoCard(),
              const SizedBox(height: 16),

              // Original rejection card
              if (appeal.originalRejectionReason != null)
                _buildOriginalRejectionCard(),
              const SizedBox(height: 16),

              // Worker explanation card
              _buildExplanationCard(),
              const SizedBox(height: 16),

              // Evidence photo if exists
              if (appeal.evidencePhotoUrl != null) _buildEvidencePhotoCard(),
              const SizedBox(height: 16),

              // Review details if reviewed
              if (appeal.reviewedAt != null)
                _buildReviewCard(statusColor),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStatusCard(Color statusColor) {
    return Card(
      color: statusColor.withOpacity(0.1),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Icon(
              appeal.isPending
                  ? Icons.hourglass_empty
                  : appeal.isApproved
                      ? Icons.check_circle
                      : Icons.cancel,
              size: 48,
              color: statusColor,
            ),
            const SizedBox(height: 12),
            Text(
              appeal.statusName,
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: statusColor,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'تم التقديم: ${DateFormatter.formatDateTime(appeal.submittedAt)}',
              style: TextStyle(
                fontSize: 14,
                color: Colors.grey[700],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTaskInfoCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.assignment, color: Colors.blue),
                SizedBox(width: 8),
                Text(
                  'المهمة',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            Text(
              appeal.entityTitle!,
              style: const TextStyle(fontSize: 16),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLocationInfoCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.place, color: Colors.blue),
                SizedBox(width: 8),
                Text(
                  'معلومات الموقع',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            InfoRow(
              label: 'المسافة من الموقع المتوقع:',
              value: '${appeal.distanceMeters} متر',
            ),
            if (appeal.workerLatitude != null &&
                appeal.workerLongitude != null) ...[
              const SizedBox(height: 12),
              InfoRow(
                label: 'موقعك:',
                value: '${appeal.workerLatitude!.toStringAsFixed(6)}, ${appeal.workerLongitude!.toStringAsFixed(6)}',
              ),
            ],
            if (appeal.expectedLatitude != null &&
                appeal.expectedLongitude != null) ...[
              const SizedBox(height: 12),
              InfoRow(
                label: 'الموقع المتوقع:',
                value: '${appeal.expectedLatitude!.toStringAsFixed(6)}, ${appeal.expectedLongitude!.toStringAsFixed(6)}',
              ),
            ],
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: _openInMaps,
              icon: const Icon(Icons.map),
              label: const Text('عرض على الخريطة'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildOriginalRejectionCard() {
    return Card(
      color: Colors.red[50],
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.error_outline, color: Colors.red),
                SizedBox(width: 8),
                Text(
                  'سبب الرفض الأصلي',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            Text(
              appeal.originalRejectionReason!,
              style: const TextStyle(fontSize: 15),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildExplanationCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.message, color: Colors.blue),
                SizedBox(width: 8),
                Text(
                  'تبريرك',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            Text(
              appeal.workerExplanation,
              style: const TextStyle(fontSize: 15),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildEvidencePhotoCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.photo, color: Colors.blue),
                SizedBox(width: 8),
                Text(
                  'الصورة الداعمة',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: Image.network(
                appeal.evidencePhotoUrl!,
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) {
                  return Container(
                    height: 200,
                    color: Colors.grey[300],
                    child: const Center(
                      child: Icon(Icons.error, size: 48, color: Colors.red),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildReviewCard(Color statusColor) {
    return Card(
      color: statusColor.withOpacity(0.05),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  appeal.isApproved ? Icons.check_circle : Icons.cancel,
                  color: statusColor,
                ),
                const SizedBox(width: 8),
                const Text(
                  'قرار المشرف',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            InfoRow(
              label: 'راجعه:',
              value: appeal.reviewedByName ?? 'المشرف',
            ),
            const SizedBox(height: 12),
            InfoRow(
              label: 'تاريخ المراجعة:',
              value: DateFormatter.formatDateTime(appeal.reviewedAt!),
            ),
            if (appeal.reviewNotes != null && appeal.reviewNotes!.isNotEmpty) ...[
              const Divider(height: 24),
              const Text(
                'ملاحظات المشرف:',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 15,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                appeal.reviewNotes!,
                style: const TextStyle(fontSize: 15),
              ),
            ],
          ],
        ),
      ),
    );
  }

  void _openInMaps() async {
    if (appeal.workerLatitude != null && appeal.workerLongitude != null) {
      final url = Uri.parse(
          'https://www.google.com/maps/search/?api=1&query=${appeal.workerLatitude},${appeal.workerLongitude}');
      if (await canLaunchUrl(url)) {
        await launchUrl(url, mode: LaunchMode.externalApplication);
      }
    }
  }
}
