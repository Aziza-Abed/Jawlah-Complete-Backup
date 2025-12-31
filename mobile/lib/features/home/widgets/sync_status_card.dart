import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../providers/sync_manager.dart';

class SyncStatusCard extends StatelessWidget {
  const SyncStatusCard({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<SyncManager>(
      builder: (context, connectivity, child) {
        final pendingCount = connectivity.waitingItems;
        final isSyncing = connectivity.isSyncingNow;

        // hide card if nothing to sync
        if (pendingCount == 0 && !isSyncing) {
          return const SizedBox.shrink();
        }

        return Container(
          width: double.infinity,
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: AppColors.cardBackground,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: AppColors.warning.withOpacity(0.3),
              width: 1.5,
            ),
            boxShadow: [
              BoxShadow(
                color: AppColors.warning.withOpacity(0.1),
                blurRadius: 10,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Row(
            children: [
              Container(
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  color: AppColors.warning.withOpacity(0.1),
                  shape: BoxShape.circle,
                ),
                child: isSyncing
                    ? const Padding(
                        padding: EdgeInsets.all(12.0),
                        child: CircularProgressIndicator(
                          color: AppColors.warning,
                          strokeWidth: 3,
                        ),
                      )
                    : const Icon(
                        Icons.cloud_upload,
                        size: 28,
                        color: AppColors.warning,
                      ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isSyncing
                          ? 'جاري المزامنة...'
                          : 'بيانات بانتظار المزامنة',
                      style: const TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w600,
                        color: AppColors.warning,
                        fontFamily: 'Cairo',
                      ),
                    ),
                    if (!isSyncing) ...[
                      const SizedBox(height: 4),
                      Text(
                        'لديك $pendingCount ${pendingCount == 1 ? 'عنصر' : 'عناصر'} بانتظار المزامنة',
                        style: const TextStyle(
                          fontSize: 14,
                          color: AppColors.textSecondary,
                          fontFamily: 'Cairo',
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              if (!isSyncing && connectivity.isOnline) ...[
                const SizedBox(width: 8),
                IconButton(
                  onPressed: () async {
                    final result = await connectivity.startSync();
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(
                            result.success
                                ? 'تمت المزامنة بنجاح'
                                : 'فشلت المزامنة: ${result.errorMessage}',
                            style: const TextStyle(fontFamily: 'Cairo'),
                          ),
                          backgroundColor: result.success
                              ? AppColors.success
                              : AppColors.error,
                        ),
                      );
                    }
                  },
                  icon: const Icon(
                    Icons.sync,
                    color: AppColors.warning,
                    size: 24,
                  ),
                  tooltip: 'مزامنة الآن',
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}
