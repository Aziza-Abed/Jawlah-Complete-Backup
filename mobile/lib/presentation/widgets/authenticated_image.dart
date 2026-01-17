import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/storage_helper.dart';
import 'package:dio/dio.dart';

// widget for displaying images that require authentication
// handles both network images (with JWT token) and local file images
class AuthenticatedImage extends StatefulWidget {
  final String imageUrl;
  final double? width;
  final double? height;
  final BoxFit fit;

  const AuthenticatedImage({
    super.key,
    required this.imageUrl,
    this.width,
    this.height,
    this.fit = BoxFit.cover,
  });

  @override
  State<AuthenticatedImage> createState() => _AuthenticatedImageState();
}

class _AuthenticatedImageState extends State<AuthenticatedImage> {
  Uint8List? _imageBytes;
  bool _isLoading = true;
  bool _hasError = false;

  @override
  void initState() {
    super.initState();
    _loadImage();
  }

  @override
  void didUpdateWidget(AuthenticatedImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.imageUrl != widget.imageUrl) {
      setState(() {
        _isLoading = true;
        _hasError = false;
        _imageBytes = null;
      });
      _loadImage();
    }
  }

  Future<void> _loadImage() async {
    try {
      final imageUrl = widget.imageUrl;

      // check if it's a local file path
      if (!imageUrl.startsWith('http://') && !imageUrl.startsWith('https://')) {
        final file = File(imageUrl);
        if (await file.exists()) {
          final bytes = await file.readAsBytes();
          if (mounted) {
            setState(() {
              _imageBytes = bytes;
              _isLoading = false;
              _hasError = false;
            });
          }
          return;
        } else {
          throw Exception('Local file not found: $imageUrl');
        }
      }

      // load network image with authentication
      final token = await StorageHelper.getToken();
      if (token == null) {
        throw Exception('Authentication token not found');
      }

      final dio = Dio();
      final response = await dio.get(
        imageUrl,
        options: Options(
          headers: {
            'Authorization': 'Bearer $token',
          },
          responseType: ResponseType.bytes,
        ),
      );

      if (response.statusCode == 200) {
        if (mounted) {
          setState(() {
            _imageBytes = Uint8List.fromList(response.data);
            _isLoading = false;
            _hasError = false;
          });
        }
      } else {
        throw Exception('Failed to load image: ${response.statusCode}');
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error loading authenticated image: $e');
        debugPrint('Image URL: ${widget.imageUrl}');
      }
      if (mounted) {
        setState(() {
          _isLoading = false;
          _hasError = true;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Container(
        width: widget.width,
        height: widget.height,
        color: AppColors.textSecondary.withOpacity(0.05),
        child: const Center(
          child: CircularProgressIndicator(
            color: AppColors.primary,
          ),
        ),
      );
    }

    if (_hasError || _imageBytes == null) {
      return Container(
        width: widget.width,
        height: widget.height,
        color: AppColors.textSecondary.withOpacity(0.1),
        child: const Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.broken_image,
              size: 48,
              color: AppColors.textSecondary,
            ),
            SizedBox(height: 8),
            Text(
              'فشل تحميل الصورة',
              style: TextStyle(
                fontSize: 14,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
          ],
        ),
      );
    }

    return Image.memory(
      _imageBytes!,
      width: widget.width,
      height: widget.height,
      fit: widget.fit,
      errorBuilder: (context, error, stackTrace) {
        if (kDebugMode) {
          debugPrint('Error displaying image bytes: $error');
        }
        return Container(
          width: widget.width,
          height: widget.height,
          color: AppColors.textSecondary.withOpacity(0.1),
          child: const Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.broken_image,
                size: 48,
                color: AppColors.textSecondary,
              ),
              SizedBox(height: 8),
              Text(
                'فشل تحميل الصورة',
                style: TextStyle(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
