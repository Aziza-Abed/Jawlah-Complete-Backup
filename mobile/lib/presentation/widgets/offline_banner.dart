import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/sync_manager.dart';

class OfflineBanner extends StatelessWidget {
  const OfflineBanner({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<SyncManager>(
      builder: (context, connectivity, child) {
        // don't show anything if online and nothing pending
        if (connectivity.isOnline && connectivity.waitingItems == 0) {
          return const SizedBox.shrink();
        }

        return Material(
          elevation: 4,
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            color: connectivity.isOnline ? Colors.orange : Colors.red,
            child: Row(
              children: [
                Icon(
                  connectivity.isOnline
                      ? Icons.cloud_upload_outlined
                      : Icons.cloud_off_outlined,
                  color: Colors.white,
                  size: 20,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    connectivity.isOnline
                        ? 'مزامنة ${connectivity.waitingItems} سجل معلق...'
                        : 'غير متصل بالإنترنت - ${connectivity.waitingItems} سجل معلق',
                    style: const TextStyle(
                      color: Colors.white,
                      fontFamily: 'Cairo',
                      fontSize: 14,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                if (connectivity.isOnline && !connectivity.isSyncingNow)
                  TextButton(
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
                            backgroundColor:
                                result.success ? Colors.green : Colors.red,
                          ),
                        );
                      }
                    },
                    child: const Text(
                      'مزامنة الآن',
                      style: TextStyle(
                        color: Colors.white,
                        fontFamily: 'Cairo',
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                if (connectivity.isSyncingNow)
                  const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}
