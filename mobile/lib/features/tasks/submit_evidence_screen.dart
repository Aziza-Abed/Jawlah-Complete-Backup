import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';
import '../../presentation/widgets/base_screen.dart';
import '../../presentation/widgets/gps_notice_widget.dart';
import '../../presentation/widgets/offline_banner.dart';
import '../../providers/task_manager.dart';
import '../../providers/sync_manager.dart';
import '../../core/utils/photo_picker_helper.dart';

class SubmitEvidenceScreen extends StatefulWidget {
  final int? taskId;

  const SubmitEvidenceScreen({super.key, this.taskId});

  @override
  State<SubmitEvidenceScreen> createState() => _SubmitEvidenceScreenState();
}

class _SubmitEvidenceScreenState extends State<SubmitEvidenceScreen> {
  final _formKey = GlobalKey<FormState>();
  final _notesController = TextEditingController();
  File? _selectedImage;
  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (widget.taskId != null) {
        context.read<TaskManager>().getTaskDetails(widget.taskId!);
      }
    });
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'إرسال إثبات',
      showBackButton: true,
      body: Column(
        children: [
          const OfflineBanner(),
          Expanded(
            child: Consumer<TaskManager>(
              builder: (context, provider, child) {
                if (provider.isLoading && provider.currentTask == null) {
                  return const Center(
                    child: CircularProgressIndicator(
                      color: AppColors.primary,
                    ),
                  );
                }

                final task = provider.currentTask;
                if (task == null) {
                  return const Center(
                    child: Text(
                      'لم يتم اختيار مهمة',
                      style: TextStyle(
                        fontSize: 18,
                        color: AppColors.textSecondary,
                        fontFamily: 'Cairo',
                      ),
                    ),
                  );
                }

                return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildTaskInfoCard(task.title),
                  const SizedBox(height: 20),
                  _buildPhotoUpload(),
                  const SizedBox(height: 20),
                  _buildNotesField(),
                  const SizedBox(height: 20),
                  _buildGpsNotice(),
                  const SizedBox(height: 24),
                  _buildSubmitButton(),
                ],
              ),
            ),
          );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTaskInfoCard(String taskTitle) {
    return Container(
      width: double.infinity,
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
            'المهمة',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: AppColors.textSecondary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 8),
          Text(
            taskTitle,
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
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
            'صورة الإثبات',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 12),
          if (_selectedImage != null)
            _buildPhotoPreview()
          else
            _buildAddPhotoButton(),
        ],
      ),
    );
  }

  Widget _buildAddPhotoButton() {
    return InkWell(
      onTap: _pickImage,
      child: Container(
        height: 200,
        decoration: BoxDecoration(
          border: Border.all(
            color: AppColors.textSecondary.withOpacity(0.3),
            style: BorderStyle.solid,
            width: 2,
          ),
          borderRadius: BorderRadius.circular(12),
        ),
        child: const Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.camera_alt,
                size: 48,
                color: AppColors.primary,
              ),
              SizedBox(height: 8),
              Text(
                'اضغط لالتقاط صورة الآن',
                style: TextStyle(
                  fontSize: 16,
                  color: AppColors.textSecondary,
                  fontFamily: 'Cairo',
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPhotoPreview() {
    return Stack(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(12),
          child: Image.file(
            _selectedImage!,
            width: double.infinity,
            height: 200,
            fit: BoxFit.cover,
          ),
        ),
        Positioned(
          top: 8,
          left: 8,
          child: InkWell(
            onTap: () {
              setState(() {
                _selectedImage = null;
              });
            },
            child: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.black.withOpacity(0.6),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.close,
                color: Colors.white,
                size: 20,
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildNotesField() {
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
            'ملاحظات (اختيارية)',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _notesController,
            maxLines: 4,
            textDirection: TextDirection.rtl,
            style: const TextStyle(
              fontSize: 16,
              fontFamily: 'Cairo',
            ),
            validator: (value) {
              // notes is optional
              return null;
            },
            decoration: InputDecoration(
              hintText: 'أضف ملاحظات عن إنجاز المهمة... (اختياري)',
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
              focusedBorder: const OutlineInputBorder(
                borderRadius: BorderRadius.all(Radius.circular(12)),
                borderSide: BorderSide(
                  color: AppColors.primary,
                  width: 2,
                ),
              ),
              errorBorder: const OutlineInputBorder(
                borderRadius: BorderRadius.all(Radius.circular(12)),
                borderSide: BorderSide(
                  color: AppColors.error,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGpsNotice() {
    return const GpsNoticeWidget(
      customMessage: 'سيتم إرسال موقعك الحالي تلقائياً مع الإثبات',
    );
  }

  Widget _buildSubmitButton() {
    return SizedBox(
      width: double.infinity,
      child: ElevatedButton(
        onPressed: _isSubmitting ? null : _submitEvidence,
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          elevation: 0,
          disabledBackgroundColor: AppColors.textSecondary.withOpacity(0.3),
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
                'إرسال الإثبات',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Cairo',
                ),
              ),
      ),
    );
  }

  // camera only no gallery
  // uses default settings from PhotoPickerHelper (1024x1024, quality 70)
  Future<void> _pickImage() async {
    final image = await PhotoPickerHelper.pickImageCameraOnly(context);

    if (image != null) {
      setState(() {
        _selectedImage = image;
      });
    }
  }

  // send task completion
  Future<void> _submitEvidence() async {
    // validate notes they are optional
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final task = context.read<TaskManager>().currentTask;
    if (task == null) return;

    // check if photo is needed
    if (task.requiresPhotoProof && _selectedImage == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text(
            'هذه المهمة تتطلب صورة. يرجى إضافة صورة على الأقل.',
            style: TextStyle(fontFamily: 'Cairo'),
          ),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }

    // show confirmation dialog
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('تأكيد إرسال الإثبات', style: TextStyle(fontFamily: 'Cairo')),
        content: const Text('هل أنت متأكد من إرسال هذا الإثبات؟ لا يمكن التراجع بعد الإرسال.', style: TextStyle(fontFamily: 'Cairo')),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('إلغاء', style: TextStyle(fontFamily: 'Cairo')),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('إرسال', style: TextStyle(color: AppColors.primary, fontFamily: 'Cairo')),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() => _isSubmitting = true);

    // finish the task with automatic GPS location
    final taskManager = context.read<TaskManager>();
    final success = await taskManager.finishTask(
          task.taskId,
          notes: _notesController.text.trim(),
          proofPhoto: _selectedImage,
        );

    setState(() => _isSubmitting = false);

    if (success && mounted) {
      // show sucess and go back
      final isOnline = context.read<SyncManager>().isOnline;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isOnline
                ? 'تم إكمال المهمة بنجاح'
                : 'تم حفظ الإثبات محلياً وسيتم رفعه عند الاتصال',
            style: const TextStyle(fontFamily: 'Cairo'),
          ),
          backgroundColor: AppColors.success,
        ),
      );
      Navigator.of(context).pop();
      Navigator.of(context).pop();
    } else if (mounted) {
      // show error in dialog for better readability of long messages
      final provider = context.read<TaskManager>();
      final errorMessage = provider.errorMessage ?? 'فشل إكمال المهمة';

      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text(
            'تنبيه',
            style: TextStyle(
              fontFamily: 'Cairo',
              fontWeight: FontWeight.bold,
            ),
            textAlign: TextAlign.right,
          ),
          content: Text(
            errorMessage,
            style: const TextStyle(
              fontFamily: 'Cairo',
              fontSize: 16,
            ),
            textAlign: TextAlign.right,
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text(
                'حسناً',
                style: TextStyle(
                  fontFamily: 'Cairo',
                  fontSize: 16,
                ),
              ),
            ),
          ],
        ),
      );
    }
  }
}
