import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../theme/app_colors.dart';
import 'image_compressor.dart';

// centralized photo picker helper
// compresses images if they are bigger than 5MB
class PhotoPickerHelper {
  static final ImagePicker _picker = ImagePicker();

  // standard settings for all images (from discussion comments)
  static const int _standardMaxWidth = 1024;
  static const int _standardMaxHeight = 1024;
  static const int _standardQuality = 70;

  // pick image with camera only
  static Future<File?> pickImageCameraOnly(
    BuildContext context, {
    int maxWidth = _standardMaxWidth,
    int maxHeight = _standardMaxHeight,
    int imageQuality = _standardQuality,
  }) async {
    try {
      final XFile? image = await _picker.pickImage(
        source: ImageSource.camera,
        maxWidth: maxWidth.toDouble(),
        maxHeight: maxHeight.toDouble(),
        imageQuality: imageQuality,
      );

      if (image != null) {
        // compress if file is too big
        final file = File(image.path);
        return await ImageCompressor.compressIfNeeded(file);
      }
      return null;
    } catch (e) {
      if (context.mounted) {
        _showErrorSnackbar(
          context,
          'حدث خطأ أثناء التقاط الصورة',
        );
      }
      return null;
    }
  }

  // pick image with modal choice (camera or gallery)
  static Future<File?> pickImageWithChoice(
    BuildContext context, {
    int maxWidth = _standardMaxWidth,
    int maxHeight = _standardMaxHeight,
    int imageQuality = _standardQuality,
  }) async {
    // show modal bottom sheet to choose source
    final ImageSource? source = await showModalBottomSheet<ImageSource>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => Container(
        decoration: const BoxDecoration(
          color: AppColors.cardBackground,
          borderRadius: BorderRadius.only(
            topLeft: Radius.circular(20),
            topRight: Radius.circular(20),
          ),
        ),
        child: SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ListTile(
                leading: const Icon(Icons.camera_alt, color: AppColors.primary),
                title: const Text(
                  'التقاط صورة',
                  style: TextStyle(fontFamily: 'Cairo'),
                ),
                onTap: () => Navigator.pop(context, ImageSource.camera),
              ),
              ListTile(
                leading:
                    const Icon(Icons.photo_library, color: AppColors.primary),
                title: const Text(
                  'اختيار من المعرض',
                  style: TextStyle(fontFamily: 'Cairo'),
                ),
                onTap: () => Navigator.pop(context, ImageSource.gallery),
              ),
            ],
          ),
        ),
      ),
    );

    // if user cancelled, return null
    if (source == null) return null;

    try {
      final XFile? image = await _picker.pickImage(
        source: source,
        maxWidth: maxWidth.toDouble(),
        maxHeight: maxHeight.toDouble(),
        imageQuality: imageQuality,
      );

      if (image != null) {
        // compress if file is too big
        final file = File(image.path);
        return await ImageCompressor.compressIfNeeded(file);
      }
      return null;
    } catch (e) {
      if (context.mounted) {
        _showErrorSnackbar(
          context,
          'حدث خطأ أثناء التقاط الصورة',
        );
      }
      return null;
    }
  }

  // show error snackbar
  static void _showErrorSnackbar(BuildContext context, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          style: const TextStyle(fontFamily: 'Cairo'),
        ),
        backgroundColor: AppColors.error,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }
}
