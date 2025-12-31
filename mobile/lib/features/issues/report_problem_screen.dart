import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../presentation/widgets/gps_notice_widget.dart';
import '../../providers/issue_manager.dart';
import '../../providers/sync_manager.dart';
import '../../core/utils/photo_picker_helper.dart';

class ReportProblemScreen extends StatefulWidget {
  const ReportProblemScreen({super.key});

  @override
  State<ReportProblemScreen> createState() => _ReportProblemScreenState();
}

class _ReportProblemScreenState extends State<ReportProblemScreen> {
  final _formKey = GlobalKey<FormState>();
  final _descriptionController = TextEditingController();
  String _selectedProblemType = 'Infrastructure';
  File? _photo1;
  File? _photo2;
  File? _photo3;
  bool _isSubmitting = false;

  final Map<String, String> _problemTypes = {
    'Infrastructure': 'بنية تحتية',
    'Safety': 'سلامة',
    'Sanitation': 'نظافة',
    'Equipment': 'معدات',
    'Other': 'أخرى',
  };

  @override
  void dispose() {
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'الإبلاغ عن مشكلة',
      showBackButton: false, // Don't show back button when it's a bottom tab
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildProblemTypeDropdown(),
              const SizedBox(height: 20),
              _buildDescriptionField(),
              const SizedBox(height: 20),
              _buildPhotoUpload(),
              const SizedBox(height: 20),
              _buildGpsNotice(),
              const SizedBox(height: 24),
              _buildSubmitButton(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildProblemTypeDropdown() {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'نوع المشكلة',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            decoration: BoxDecoration(
              border:
                  Border.all(color: AppColors.textSecondary.withOpacity(0.3)),
              borderRadius: BorderRadius.circular(12),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                value: _selectedProblemType,
                isExpanded: true,
                icon: const Icon(Icons.arrow_drop_down),
                style: const TextStyle(
                  fontSize: 16,
                  color: AppColors.textPrimary,
                  fontFamily: 'Cairo',
                ),
                items: _problemTypes.entries.map((entry) {
                  return DropdownMenuItem(
                    value: entry.key,
                    child: Text(entry.value),
                  );
                }).toList(),
                onChanged: (value) {
                  if (value != null) {
                    setState(() {
                      _selectedProblemType = value;
                    });
                  }
                },
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDescriptionField() {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'وصف المشكلة (اختياري)',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _descriptionController,
            maxLines: 5,
            textDirection: TextDirection.rtl,
            style: const TextStyle(
              fontSize: 16,
              fontFamily: 'Cairo',
            ),
            decoration: InputDecoration(
              hintText: 'اشرح تفاصيل المشكلة هنا... (اختياري)',
              hintStyle: TextStyle(
                color: AppColors.textSecondary.withOpacity(0.5),
                fontFamily: 'Cairo',
              ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: BorderSide(
                  color: AppColors.textSecondary.withOpacity(0.3),
                ),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: BorderSide(
                  color: AppColors.textSecondary.withOpacity(0.3),
                ),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(
                  color: AppColors.primary,
                  width: 2,
                ),
              ),
            ),
            validator: (value) {
              if (value != null && value.trim().isNotEmpty) {
                if (value.trim().length < 10) {
                  return 'الوصف قصير جداً (10 أحرف على الأقل)';
                }
                if (value.trim().length > 500) {
                  return 'الوصف طويل جداً (500 حرف كحد أقصى)';
                }
              }
              return null;
            },
          ),
        ],
      ),
    );
  }

  Widget _buildPhotoUpload() {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'إضافة صور (1 إلزامية، 2 اختيارية)',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 12),
          _buildPhotoSlot(1, _photo1, true),
          const SizedBox(height: 12),
          _buildPhotoSlot(2, _photo2, false),
          const SizedBox(height: 12),
          _buildPhotoSlot(3, _photo3, false),
        ],
      ),
    );
  }

  Widget _buildPhotoSlot(int slot, File? photo, bool isMandatory) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          isMandatory ? 'صورة $slot (إلزامية)' : 'صورة $slot (اختيارية)',
          style: TextStyle(
            fontSize: 14,
            color: isMandatory ? AppColors.error : AppColors.textSecondary,
            fontFamily: 'Cairo',
          ),
        ),
        const SizedBox(height: 8),
        if (photo != null)
          Stack(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: Image.file(
                  photo,
                  width: double.infinity,
                  height: 120,
                  fit: BoxFit.cover,
                ),
              ),
              Positioned(
                top: 8,
                left: 8,
                child: InkWell(
                  onTap: () {
                    setState(() {
                      if (slot == 1) _photo1 = null;
                      if (slot == 2) _photo2 = null;
                      if (slot == 3) _photo3 = null;
                    });
                  },
                  child: Container(
                    padding: const EdgeInsets.all(6),
                    decoration: BoxDecoration(
                      color: Colors.black.withOpacity(0.6),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.close,
                      color: Colors.white,
                      size: 16,
                    ),
                  ),
                ),
              ),
            ],
          )
        else
          InkWell(
            onTap: () => _pickImage(slot),
            child: Container(
              width: double.infinity,
              height: 80,
              decoration: BoxDecoration(
                border: Border.all(
                  color: isMandatory
                      ? AppColors.error.withOpacity(0.5)
                      : AppColors.textSecondary.withOpacity(0.3),
                  style: BorderStyle.solid,
                  width: 2,
                ),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.add_photo_alternate,
                    size: 32,
                    color: isMandatory ? AppColors.error : AppColors.primary,
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'اضغط لإضافة صورة',
                    style: TextStyle(
                      fontSize: 14,
                      color: AppColors.textSecondary,
                      fontFamily: 'Cairo',
                    ),
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }

  Widget _buildGpsNotice() {
    return const GpsNoticeWidget(
      customMessage: 'سيتم إرسال موقعك الحالي تلقائياً مع البلاغ',
    );
  }

  Widget _buildSubmitButton() {
    return SizedBox(
      width: double.infinity,
      child: ElevatedButton(
        onPressed: _isSubmitting ? null : _submitReport,
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          elevation: 0,
        ),
        child: _isSubmitting
            ? const SizedBox(
                height: 24,
                width: 24,
                child: CircularProgressIndicator(
                  color: Colors.white,
                  strokeWidth: 2,
                ),
              )
            : const Text(
                'إرسال البلاغ',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Cairo',
                ),
              ),
      ),
    );
  }

  Future<void> _pickImage(int slot) async {
    final image = await PhotoPickerHelper.pickImageWithChoice(context);

    if (image != null) {
      setState(() {
        if (slot == 1) _photo1 = image;
        if (slot == 2) _photo2 = image;
        if (slot == 3) _photo3 = image;
      });
    }
  }

  Future<void> _submitReport() async {
    // 1. check if the form fields are valid
    if (!_formKey.currentState!.validate()) {
      return;
    }

    // 2. make sure at least one photo is picked
    if (_photo1 == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'يرجى إضافة الصورة الأولى (إلزامية)',
            style: TextStyle(fontFamily: 'Cairo'),
          ),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }

    _showConfirmDialog();
  }

  void _resetForm() {
    setState(() {
      _descriptionController.clear();
      _selectedProblemType = 'Infrastructure';
      _photo1 = null;
      _photo2 = null;
      _photo3 = null;
      _isSubmitting = false;
    });
  }

  void _showConfirmDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title:
            const Text('تأكيد الإرسال', style: TextStyle(fontFamily: 'Cairo')),
        content: const Text('هل أنت متأكد من رغبتك في إرسال هذا البلاغ؟',
            style: TextStyle(fontFamily: 'Cairo')),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(context);
              _executeSubmit();
            },
            style: ElevatedButton.styleFrom(backgroundColor: AppColors.primary),
            child: const Text('إرسال',
                style: TextStyle(color: Colors.white, fontFamily: 'Cairo')),
          ),
        ],
      ),
    );
  }

  Future<void> _executeSubmit() async {
    setState(() => _isSubmitting = true);

    // 3. call the manager to send the report
    final success = await context.read<IssueManager>().sendIssueReport(
          description: _descriptionController.text.trim(),
          type: _selectedProblemType,
          photo1: _photo1!,
          photo2: _photo2,
          photo3: _photo3,
        );

    setState(() => _isSubmitting = false);

    if (!mounted) return;

    if (success) {
      // 4. if success, show a message
      final isOnline = context.read<SyncManager>().isOnline;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isOnline
                ? 'تم إرسال البلاغ بنجاح'
                : 'تم حفظ البلاغ محلياً وسيتم إرساله عند الاتصال',
            style: const TextStyle(fontFamily: 'Cairo'),
          ),
          backgroundColor: AppColors.success,
        ),
      );

      // Reset form instead of popping to avoid black screen
      _resetForm();
    } else {
      // 5. if failed, show what went wrong
      final provider = context.read<IssueManager>();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            provider.errorMessage ?? 'فشل إرسال البلاغ',
            style: const TextStyle(fontFamily: 'Cairo'),
          ),
          backgroundColor: AppColors.error,
        ),
      );
    }
  }
}
