import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';

import '../../presentation/widgets/base_screen.dart';
import '../../providers/task_manager.dart';
import '../../data/models/task_model.dart';
import '../../presentation/widgets/fade_in_animation.dart';
import 'widgets/task_card.dart';

class TasksListScreen extends StatefulWidget {
  const TasksListScreen({super.key});

  @override
  State<TasksListScreen> createState() => _TasksListScreenState();
}

class _TasksListScreenState extends State<TasksListScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    // make the tabs we have 4 filters
    _tabController = TabController(length: 4, vsync: this);

    // get tasks when screen opens
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TaskManager>().loadTasks();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BaseScreen(
      title: 'قائمة المهام',
      showBackButton: true,
      body: Column(
        children: [
          _buildTabs(),
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                _buildTasksList('all'),
                _buildTasksList('pending'),
                _buildTasksList('inprogress'),
                _buildTasksList('completed'),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTabs() {
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.only(
          topLeft: Radius.circular(24),
          topRight: Radius.circular(24),
        ),
      ),
      child: TabBar(
        controller: _tabController,
        isScrollable: true,
        tabAlignment: TabAlignment.center,
        labelColor: AppColors.primary,
        unselectedLabelColor: AppColors.textSecondary,
        indicatorColor: AppColors.primary,
        indicatorSize: TabBarIndicatorSize.label,
        indicator: const UnderlineTabIndicator(
          borderSide: BorderSide(color: AppColors.primary, width: 3),
          insets: EdgeInsets.symmetric(horizontal: 0),
        ),
        labelPadding: const EdgeInsets.symmetric(horizontal: 16),
        labelStyle: const TextStyle(
          fontWeight: FontWeight.bold,
          fontFamily: 'Cairo',
          fontSize: 16,
        ),
        tabs: const [
          Tab(text: 'الكل'),
          Tab(text: 'جديد'),
          Tab(text: 'قيد التنفيذ'),
          Tab(text: 'مكتمل'),
        ],
      ),
    );
  }

  Widget _buildTasksList(String filterType) {
    return Consumer<TaskManager>(
      builder: (context, provider, child) {
        if (provider.isLoading && provider.myTasks.isEmpty) {
          return const Center(
            child: CircularProgressIndicator(color: AppColors.primary),
          );
        }

        // show error state if there's an error
        if (provider.errorMessage != null && provider.myTasks.isEmpty) {
          return _buildErrorState(provider);
        }

        List<TaskModel> filteredTasks;
        switch (filterType) {
          case 'pending':
            filteredTasks = provider.pendingTasks;
            break;
          case 'inprogress':
            filteredTasks = provider.inProgressTasks;
            break;
          case 'completed':
            filteredTasks = provider.completedTasks;
            break;
          default:
            filteredTasks = provider.myTasks;
        }

        if (filteredTasks.isEmpty) {
          return _buildEmptyState(filterType);
        }

        return RefreshIndicator(
          onRefresh: () => provider.refreshData(),
          child: ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: filteredTasks.length,
            itemBuilder: (context, index) {
              return FadeInAnimation(
                delay: Duration(milliseconds: 50 * index),
                child: TaskCard(task: filteredTasks[index]),
              );
            },
          ),
        );
      },
    );
  }

  Widget _buildEmptyState(String filterType) {
    String message;
    IconData icon;
    switch (filterType) {
      case 'pending':
        message = 'لا توجد مهام جديدة';
        icon = Icons.inbox_outlined;
        break;
      case 'inprogress':
        message = 'لا توجد مهام قيد التنفيذ';
        icon = Icons.hourglass_empty_rounded;
        break;
      case 'completed':
        message = 'لم تكتمل أي مهام بعد';
        icon = Icons.check_circle_outline;
        break;
      default:
        message = 'لا توجد مهام حالياً';
        icon = Icons.assignment_outlined;
    }

    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: AppColors.primary.withOpacity(0.05),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 64, color: AppColors.primary.withOpacity(0.3)),
          ),
          const SizedBox(height: 16),
          Text(
            message,
            style: const TextStyle(
              fontSize: 16,
              color: AppColors.textSecondary,
              fontFamily: 'Cairo',
            ),
          ),
          const SizedBox(height: 8),
          const Text(
            'اسحب للأسفل للتحديث',
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

  Widget _buildErrorState(TaskManager provider) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: AppColors.error.withOpacity(0.1),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.error_outline, size: 64, color: AppColors.error),
            ),
            const SizedBox(height: 16),
            Text(
              provider.errorMessage ?? 'حدث خطأ أثناء تحميل المهام',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 16,
                color: AppColors.textSecondary,
                fontFamily: 'Cairo',
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () => provider.refreshData(),
              icon: const Icon(Icons.refresh),
              label: const Text('إعادة المحاولة', style: TextStyle(fontFamily: 'Cairo')),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
