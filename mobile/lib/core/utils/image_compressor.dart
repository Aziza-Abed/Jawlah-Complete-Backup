import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter_image_compress/flutter_image_compress.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as path;

// handles image compression for upload
// if file bigger than 5MB we compress it
class ImageCompressor {
  // max file size before we compress (5MB)
  static const int maxFileSizeBytes = 5 * 1024 * 1024;

  // compression settings from discussion comments
  static const int targetQuality = 70;
  static const int maxWidth = 1024;
  static const int maxHeight = 1024;

  // compress image if its too big
  // returns compressed file or original if already small enought
  static Future<File> compressIfNeeded(File imageFile) async {
    try {
      // check file size first
      final fileSize = await imageFile.length();
      debugPrint('Image size: ${(fileSize / 1024 / 1024).toStringAsFixed(2)} MB');

      if (fileSize <= maxFileSizeBytes) {
        // file is small enought no need to compress
        debugPrint('Image under 5MB, no compression needed');
        return imageFile;
      }

      // file is too big we need to compress it
      debugPrint('Image over 5MB, compressing...');
      return await _compressImage(imageFile);
    } catch (e) {
      debugPrint('Error checking/compressing image: $e');
      // if something goes wrong just return original
      return imageFile;
    }
  }

  // force compress even if under 5MB
  // usefull when we want consistant size
  static Future<File> forceCompress(File imageFile) async {
    try {
      return await _compressImage(imageFile);
    } catch (e) {
      debugPrint('Error compressing image: $e');
      return imageFile;
    }
  }

  // do the actual compression
  static Future<File> _compressImage(File imageFile) async {
    // get temp directory for compressed file
    final tempDir = await getTemporaryDirectory();
    final targetPath = path.join(
      tempDir.path,
      'compressed_${DateTime.now().millisecondsSinceEpoch}.jpg',
    );

    // compress the image
    final result = await FlutterImageCompress.compressAndGetFile(
      imageFile.absolute.path,
      targetPath,
      quality: targetQuality,
      minWidth: maxWidth,
      minHeight: maxHeight,
      format: CompressFormat.jpeg,
    );

    if (result != null) {
      final compressedFile = File(result.path);
      final newSize = await compressedFile.length();
      debugPrint(
        'Compressed: ${(newSize / 1024 / 1024).toStringAsFixed(2)} MB',
      );
      return compressedFile;
    }

    // compression faild return original
    debugPrint('Compression failed, using original');
    return imageFile;
  }

  // check if file is too big for upload
  static Future<bool> isFileTooLarge(File imageFile) async {
    final fileSize = await imageFile.length();
    return fileSize > maxFileSizeBytes;
  }

  // get file size in MB as string
  static Future<String> getFileSizeString(File imageFile) async {
    final fileSize = await imageFile.length();
    return '${(fileSize / 1024 / 1024).toStringAsFixed(2)} MB';
  }
}
