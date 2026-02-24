import 'dart:io';
import 'package:flutter_image_compress/flutter_image_compress.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as path;

// compresses images before upload if they exceed the size limit
class ImageCompressor {
  static const int _maxFileSizeBytes = 5 * 1024 * 1024; // 5MB

  // compress image if it's larger than 5MB, otherwise return original
  static Future<File> compressIfNeeded(File imageFile) async {
    try {
      final fileSize = await imageFile.length();
      if (fileSize <= _maxFileSizeBytes) return imageFile;

      final tempDir = await getTemporaryDirectory();
      final targetPath = path.join(
        tempDir.path,
        'compressed_${DateTime.now().millisecondsSinceEpoch}.jpg',
      );

      final result = await FlutterImageCompress.compressAndGetFile(
        imageFile.absolute.path,
        targetPath,
        quality: 70,
        minWidth: 1024,
        minHeight: 1024,
        format: CompressFormat.jpeg,
      );

      return result != null ? File(result.path) : imageFile;
    } catch (_) {
      return imageFile;
    }
  }
}
