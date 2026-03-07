import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../providers/auth_manager.dart';
import '../../../../presentation/widgets/authenticated_image.dart';
import 'battery_widget.dart';

class GreetingCard extends StatelessWidget {
  const GreetingCard({super.key});

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final dateStr = DateFormat('EEEE، d MMMM y', 'ar').format(now);

    return Consumer<AuthManager>(
      builder: (context, authProvider, child) {
        final userName = authProvider.userName;
        final photoUrl = authProvider.user?.profilePhotoUrl;
        final hasPhoto = photoUrl != null && photoUrl.isNotEmpty;

        return GestureDetector(
          onTap: () {
            HapticFeedback.lightImpact();
            Navigator.of(context).pushNamed(Routes.profile);
          },
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.cardBackground,
              borderRadius: BorderRadius.circular(18),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.06),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Row(
              children: [
                Container(
                  width: 64,
                  height: 64,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withOpacity(0.1),
                    shape: BoxShape.circle,
                  ),
                  child: Hero(
                    tag: 'profile-avatar',
                    child: hasPhoto
                        ? ClipOval(
                            child: AuthenticatedImage(
                              imageUrl: photoUrl,
                              width: 64,
                              height: 64,
                              fit: BoxFit.cover,
                            ),
                          )
                        : const Icon(
                            Icons.person,
                            size: 34,
                            color: AppColors.primary,
                          ),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Text(
                            'مرحباً ',
                            style: TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textPrimary,
                              fontFamily: 'Cairo',
                            ),
                          ),
                          Flexible(
                            child: Text(
                              userName,
                              style: const TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.bold,
                                color: AppColors.textPrimary,
                                fontFamily: 'Cairo',
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const SizedBox(width: 6),
                          const Text('👋', style: TextStyle(fontSize: 20)),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Text(
                        dateStr,
                        style: const TextStyle(
                          fontSize: 14,
                          color: AppColors.textSecondary,
                          fontFamily: 'Cairo',
                        ),
                      ),
                      const SizedBox(height: 10),
                      // Battery indicator
                      const BatteryWidget(),
                    ],
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
