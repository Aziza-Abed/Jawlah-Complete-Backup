import 'dart:io';
import 'package:flutter_image_compress/flutter_image_compress.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as path;

import '../config/app_constants.dart';

// compresses images before upload if they exceed the size limit
class ImageCompressor {
  // compress image if it's larger than the max size, otherwise return original
  static Future<File> compressIfNeeded(File imageFile) async {
    try {
      final fileSize = await imageFile.length();
      if (fileSize <= AppConstants.maxImageSizeBytes) return imageFile;

      final tempDir = await getTemporaryDirectory();
      final targetPath = path.join(
        tempDir.path,
        'compressed_${DateTime.now().millisecondsSinceEpoch}.jpg',
      );

      final result = await FlutterImageCompress.compressAndGetFile(
        imageFile.absolute.path,
        targetPath,
        quality: AppConstants.imageCompressionQuality,
        minWidth: AppConstants.imageMaxDimension,
        minHeight: AppConstants.imageMaxDimension,
        format: CompressFormat.jpeg,
      );

      return result != null ? File(result.path) : imageFile;
    } catch (_) {
      return imageFile;
    }
  }
}
