import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../data/models/task_model.dart';
import '../../providers/appeal_manager.dart';
import '../../core/utils/date_formatter.dart';
import '../../presentation/widgets/info_row.dart';

class SubmitAppealScreen extends StatefulWidget {
  final TaskModel rejectedTask;

  const SubmitAppealScreen({
    super.key,
    required this.rejectedTask,
  });

  @override
  State<SubmitAppealScreen> createState() => _SubmitAppealScreenState();
}

class _SubmitAppealScreenState extends State<SubmitAppealScreen> {
  final _formKey = GlobalKey<FormState>();
  final _explanationController = TextEditingController();
  final _picker = ImagePicker();
  File? _evidencePhoto;
  bool _isSubmitting = false;

  @override
  void dispose() {
    _explanationController.dispose();
    super.dispose();
  }

  Future<void> _pickImage() async {
    try {
      final XFile? pickedFile = await _picker.pickImage(
        source: ImageSource.camera,
        maxWidth: 1920,
        maxHeight: 1080,
        imageQuality: 85,
      );

      if (pickedFile != null) {
        setState(() {
          _evidencePhoto = File(pickedFile.path);
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('فشل التقاط الصورة')),
        );
      }
    }
  }

  Future<void> _pickFromGallery() async {
    try {
      final XFile? pickedFile = await _picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 1920,
        maxHeight: 1080,
        imageQuality: 85,
      );

      if (pickedFile != null) {
        setState(() {
          _evidencePhoto = File(pickedFile.path);
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('فشل اختيار الصورة')),
        );
      }
    }
  }

  void _removePhoto() {
    setState(() {
      _evidencePhoto = null;
    });
  }

  Future<void> _submitAppeal() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() => _isSubmitting = true);

    final appealManager = context.read<AppealManager>();
    final success = await appealManager.submitAppeal(
      taskId: widget.rejectedTask.taskId,
      explanation: _explanationController.text.trim(),
      evidencePhoto: _evidencePhoto,
    );

    if (!mounted) return;

    setState(() => _isSubmitting = false);

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('✅ تم إرسال الطعن بنجاح'),
          backgroundColor: Colors.green,
        ),
      );
      Navigator.of(context).pop(true); // Return true to indicate success
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(appealManager.errorMessage ?? 'فشل في إرسال الطعن'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('تقديم طعن'),
          centerTitle: true,
        ),
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(16.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Rejection info card
                _buildRejectionInfoCard(),
                const SizedBox(height: 24),

                // Explanation section
                const Text(
                  'تبرير الطعن',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'اشرح سبب عدم تطابق موقعك مع موقع المهمة',
                  style: TextStyle(
                    fontSize: 14,
                    color: Colors.grey,
                  ),
                ),
                const SizedBox(height: 12),

                TextFormField(
                  controller: _explanationController,
                  maxLines: 5,
                  maxLength: 1000,
                  decoration: const InputDecoration(
                    hintText:
                        'مثال: كنت في الموقع الصحيح ولكن إشارة GPS كانت ضعيفة بسبب المباني العالية',
                    border: OutlineInputBorder(),
                    counterText: '',
                  ),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'الرجاء كتابة تبرير';
                    }
                    if (value.trim().length < 10) {
                      return 'التبرير قصير جداً (الحد الأدنى 10 أحرف)';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 8),
                Text(
                  '${_explanationController.text.length}/1000 حرف',
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[600],
                  ),
                  textAlign: TextAlign.left,
                ),
                const SizedBox(height: 24),

                // Evidence photo section
                const Text(
                  'صورة داعمة (اختياري)',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'يمكنك إرفاق صورة تثبت وجودك في الموقع الصحيح',
                  style: TextStyle(
                    fontSize: 14,
                    color: Colors.grey,
                  ),
                ),
                const SizedBox(height: 12),

                if (_evidencePhoto != null)
                  _buildPhotoPreview()
                else
                  _buildPhotoButtons(),

                const SizedBox(height: 32),

                // Submit button
                ElevatedButton(
                  onPressed: _isSubmitting ? null : _submitAppeal,
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    backgroundColor: Colors.blue,
                    foregroundColor: Colors.white,
                  ),
                  child: _isSubmitting
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            valueColor:
                                AlwaysStoppedAnimation<Color>(Colors.white),
                          ),
                        )
                      : const Text(
                          'إرسال الطعن',
                          style: TextStyle(fontSize: 18),
                        ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildRejectionInfoCard() {
    return Card(
      color: Colors.red[50],
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.error_outline, color: Colors.red),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    widget.rejectedTask.title,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            InfoRow(
              label: 'سبب الرفض:',
              value: widget.rejectedTask.rejectionReason ?? 'غير محدد',
              fontSize: 14,
            ),
            if (widget.rejectedTask.rejectionDistanceMeters != null) ...[
              const SizedBox(height: 8),
              InfoRow(
                label: 'المسافة من الموقع المتوقع:',
                value: '${widget.rejectedTask.rejectionDistanceMeters} متر',
                fontSize: 14,
              ),
            ],
            if (widget.rejectedTask.rejectedAt != null) ...[
              const SizedBox(height: 8),
              InfoRow(
                label: 'تاريخ الرفض:',
                value: DateFormatter.formatDateTime(widget.rejectedTask.rejectedAt!),
                fontSize: 14,
              ),
            ],
          ],
        ),
      ),
    );
  }


  Widget _buildPhotoButtons() {
    return Row(
      children: [
        Expanded(
          child: OutlinedButton.icon(
            onPressed: _pickImage,
            icon: const Icon(Icons.camera_alt),
            label: const Text('التقاط صورة'),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: OutlinedButton.icon(
            onPressed: _pickFromGallery,
            icon: const Icon(Icons.photo_library),
            label: const Text('اختر من المعرض'),
          ),
        ),
      ],
    );
  }

  Widget _buildPhotoPreview() {
    return Stack(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: Image.file(
            _evidencePhoto!,
            height: 200,
            width: double.infinity,
            fit: BoxFit.cover,
          ),
        ),
        Positioned(
          top: 8,
          left: 8,
          child: IconButton(
            onPressed: _removePhoto,
            icon: const Icon(Icons.close),
            style: IconButton.styleFrom(
              backgroundColor: Colors.red,
              foregroundColor: Colors.white,
            ),
          ),
        ),
      ],
    );
  }

}
