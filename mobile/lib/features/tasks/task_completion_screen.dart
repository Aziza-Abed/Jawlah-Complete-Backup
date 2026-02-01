import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/photo_picker_helper.dart';
import '../../presentation/widgets/gps_notice_widget.dart';
import '../../data/models/task_model.dart';
import '../../providers/task_manager.dart';

class TaskCompletionScreen extends StatefulWidget {
  final TaskModel task;

  const TaskCompletionScreen({
    super.key,
    required this.task,
  });

  @override
  State<TaskCompletionScreen> createState() => _TaskCompletionScreenState();
}

class _TaskCompletionScreenState extends State<TaskCompletionScreen> {
  final _formKey = GlobalKey<FormState>();
  final _notesController = TextEditingController();

  File? _proofPhoto;
  bool _isSubmitting = false;

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _pickPhoto() async {
    // use default settings from PhotoPickerHelper (1024x1024, quality 70)
    // this ensures all images are compressed consistantly
    final image = await PhotoPickerHelper.pickImageWithChoice(context);

    if (image != null) {
      setState(() {
        _proofPhoto = image;
      });
    }
  }

  Future<void> _submitCompletion() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    if (_proofPhoto == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('يرجى إضافة صورة إثبات للمهمة'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    setState(() {
      _isSubmitting = true;
    });

    try {
      final success = await context.read<TaskManager>().finishTask(
            widget.task.taskId,
            notes: _notesController.text.trim(),
            proofPhoto: _proofPhoto,
          );

      if (mounted) {
        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('تم إكمال المهمة بنجاح'),
              backgroundColor: Colors.green,
            ),
          );
          Navigator.pop(context, true); // Return true to indicate success
        } else {
          // Show actual error message from backend in a dialog
          final taskManager = context.read<TaskManager>();
          final errorMessage = taskManager.errorMessage ??
                               'فشل إكمال المهمة، يرجى المحاولة مرة أخرى';

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
    } catch (e) {
      if (mounted) {
        showDialog(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text(
              'خطأ',
              style: TextStyle(
                fontFamily: 'Cairo',
                fontWeight: FontWeight.bold,
              ),
              textAlign: TextAlign.right,
            ),
            content: Text(
              'خطأ: $e',
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
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('إكمال المهمة'),
        backgroundColor: AppColors.primary,
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // task info card
            Card(
              elevation: 2,
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(
                          _getPriorityIcon(widget.task.priority),
                          color: _getPriorityColor(widget.task.priority),
                          size: 24,
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            widget.task.title,
                            style: const TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ),
                    if (widget.task.description.isNotEmpty) ...[
                      const SizedBox(height: 8),
                      Text(
                        widget.task.description,
                        style: TextStyle(
                          fontSize: 14,
                          color: Colors.grey[600],
                        ),
                      ),
                    ],
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        const Icon(Icons.location_on,
                            size: 16, color: Colors.grey),
                        const SizedBox(width: 4),
                        Expanded(
                          child: Text(
                            widget.task.location ?? 'غير محدد',
                            style: const TextStyle(fontSize: 14),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 24),

            // notes section
            const Text(
              'ملاحظات الإكمال *',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _notesController,
              maxLines: 5,
              decoration: InputDecoration(
                hintText: 'اكتب ملاحظاتك عن إكمال المهمة...',
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
                filled: true,
                fillColor: Colors.grey[50],
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return 'يرجى إدخال ملاحظات الإكمال';
                }
                if (value.trim().length < 10) {
                  return 'يرجى إدخال ملاحظات تفصيلية (10 أحرف على الأقل)';
                }
                if (value.trim().length > 500) {
                  return 'ملاحظات طويلة جداً (500 حرف كحد أقصى)';
                }
                return null;
              },
            ),

            const SizedBox(height: 24),

            // photo section
            const Text(
              'صورة إثبات *',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),

            if (_proofPhoto != null)
              Stack(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(12),
                    child: Image.file(
                      _proofPhoto!,
                      height: 300,
                      width: double.infinity,
                      fit: BoxFit.cover,
                    ),
                  ),
                  Positioned(
                    top: 8,
                    right: 8,
                    child: IconButton(
                      icon: const Icon(Icons.edit, color: Colors.white),
                      style: IconButton.styleFrom(
                        backgroundColor: Colors.black54,
                      ),
                      onPressed: _pickPhoto,
                    ),
                  ),
                ],
              )
            else
              InkWell(
                onTap: _pickPhoto,
                borderRadius: BorderRadius.circular(12),
                child: Container(
                  height: 200,
                  decoration: BoxDecoration(
                    color: Colors.grey[100],
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: Colors.grey[300]!,
                      width: 2,
                      style: BorderStyle.solid,
                    ),
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        Icons.add_photo_alternate,
                        size: 64,
                        color: Colors.grey[400],
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'اضغط لإضافة صورة',
                        style: TextStyle(
                          fontSize: 16,
                          color: Colors.grey[600],
                        ),
                      ),
                    ],
                  ),
                ),
              ),

            const SizedBox(height: 32),

            // send button
            ElevatedButton(
              onPressed: _isSubmitting ? null : _submitCompletion,
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.green,
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: _isSubmitting
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(
                        color: Colors.white,
                        strokeWidth: 2,
                      ),
                    )
                  : const Text(
                      'إكمال المهمة',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
            ),

            const SizedBox(height: 16),

            // gps info note
            const GpsNoticeWidget(
              customMessage:
                  'سيتم تسجيل موقعك الحالي تلقائياً عند إكمال المهمة',
            ),
          ],
        ),
      ),
    );
  }

  IconData _getPriorityIcon(String? priority) {
    switch (priority?.toLowerCase()) {
      case 'high':
      case 'عالية':
        return Icons.priority_high;
      case 'medium':
      case 'متوسطة':
        return Icons.remove;
      case 'low':
      case 'منخفضة':
        return Icons.arrow_downward;
      default:
        return Icons.flag;
    }
  }

  Color _getPriorityColor(String? priority) {
    switch (priority?.toLowerCase()) {
      case 'high':
      case 'عالية':
        return Colors.red;
      case 'medium':
      case 'متوسطة':
        return Colors.orange;
      case 'low':
      case 'منخفضة':
        return Colors.green;
      default:
        return Colors.grey;
    }
  }
}
