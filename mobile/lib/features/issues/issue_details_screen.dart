import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';
import '../../presentation/widgets/authenticated_image.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../presentation/widgets/offline_banner.dart';
import '../../providers/issue_manager.dart';
import '../../data/models/issue_model.dart';

class IssueDetailsScreen extends StatefulWidget {
  const IssueDetailsScreen({super.key});

  @override
  State<IssueDetailsScreen> createState() => _IssueDetailsScreenState();
}

class _IssueDetailsScreenState extends State<IssueDetailsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final issueId = ModalRoute.of(context)?.settings.arguments as int?;
      if (issueId != null) {
        context.read<IssueManager>().getIssueDetails(issueId);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'تفاصيل البلاغ',
      showBackButton: true,
      body: Column(
        children: [
          const OfflineBanner(),
          Expanded(
            child: Consumer<IssueManager>(
              builder: (context, provider, _) {
                if (provider.isLoading) {
                  return const Center(
                    child: CircularProgressIndicator(color: AppColors.primary),
                  );
                }

                final issue = provider.currentIssue;
                if (issue == null) {
                  return const Center(
                    child: Text(
                      'تعذّر تحميل البلاغ',
                      style: TextStyle(
                        fontFamily: 'Cairo',
                        color: AppColors.textSecondary,
                        fontSize: 16,
                      ),
                    ),
                  );
                }

                return _buildContent(issue);
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildContent(IssueModel issue) {
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 40),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildStatusBanner(issue),
          const SizedBox(height: 16),
          _buildInfoCard(issue),
          if (issue.hasPhoto) ...[
            const SizedBox(height: 16),
            _buildPhotoCard(issue),
          ],
          if (issue.forwardedToDepartmentName != null) ...[
            const SizedBox(height: 16),
            _buildForwardingCard(issue),
          ],
          if (issue.resolutionNotes != null) ...[
            const SizedBox(height: 16),
            _buildResolutionCard(issue),
          ],
        ],
      ),
    );
  }

  Widget _buildStatusBanner(IssueModel issue) {
    Color color;
    IconData icon;
    if (issue.isResolved || issue.isClosed) {
      color = AppColors.success;
      icon = Icons.check_circle_outline;
    } else if (issue.isForwarded || issue.isUnderReview) {
      color = AppColors.info;
      icon = Icons.forward_outlined;
    } else if (issue.isInProgress) {
      color = AppColors.warning;
      icon = Icons.hourglass_empty;
    } else {
      color = AppColors.primary;
      icon = Icons.report_outlined;
    }

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color.withOpacity(0.3)),
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 28),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                issue.statusArabic,
                style: TextStyle(
                  color: color,
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                  fontFamily: 'Cairo',
                ),
              ),
              Text(
                _formatDate(issue.updatedAt),
                style: const TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 12,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildInfoCard(IssueModel issue) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            issue.title,
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 8),
          Text(
            issue.description,
            style: const TextStyle(
              fontSize: 14,
              color: AppColors.textSecondary,
              fontFamily: 'Cairo',
              height: 1.6,
            ),
          ),
          const Divider(height: 24),
          _buildRow(Icons.category_outlined, 'النوع', issue.typeArabic),
          const SizedBox(height: 12),
          _buildRow(Icons.warning_amber_outlined, 'الخطورة', issue.severityArabic),
          if (issue.location != null) ...[
            const SizedBox(height: 12),
            _buildRow(Icons.location_on_outlined, 'الموقع', issue.location!),
          ],
          const SizedBox(height: 12),
          _buildRow(Icons.calendar_today_outlined, 'تاريخ الإبلاغ', _formatDate(issue.createdAt)),
        ],
      ),
    );
  }

  Widget _buildPhotoCard(IssueModel issue) {
    final photos = issue.allPhotos;
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'الصور المرفقة',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 160,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: photos.length,
              separatorBuilder: (_, _) => const SizedBox(width: 8),
              itemBuilder: (_, i) => ClipRRect(
                borderRadius: BorderRadius.circular(10),
                child: AuthenticatedImage(
                  imageUrl: photos[i],
                  width: 160,
                  height: 160,
                  fit: BoxFit.cover,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildForwardingCard(IssueModel issue) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.info.withOpacity(0.05),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.info.withOpacity(0.2)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.forward, color: AppColors.info, size: 20),
              SizedBox(width: 8),
              Text(
                'تم التحويل إلى',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppColors.info,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            issue.forwardedToDepartmentName!,
            style: const TextStyle(
              fontSize: 14,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          if (issue.forwardedAt != null)
            Text(
              _formatDate(issue.forwardedAt!),
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
          if (issue.forwardingNotes != null) ...[
            const SizedBox(height: 8),
            Text(
              issue.forwardingNotes!,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildResolutionCard(IssueModel issue) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.success.withOpacity(0.05),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.success.withOpacity(0.2)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.check_circle_outline, color: AppColors.success, size: 20),
              SizedBox(width: 8),
              Text(
                'ملاحظات الحل',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppColors.success,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            issue.resolutionNotes!,
            style: const TextStyle(
              fontSize: 13,
              color: AppColors.textSecondary,
              fontFamily: 'Cairo',
              height: 1.5,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRow(IconData icon, String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: AppColors.primary),
        const SizedBox(width: 10),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
            Text(
              value,
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: AppColors.textPrimary,
                fontFamily: 'Cairo',
              ),
            ),
          ],
        ),
      ],
    );
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    return '${local.year}-${local.month.toString().padLeft(2, '0')}-${local.day.toString().padLeft(2, '0')}';
  }
}
