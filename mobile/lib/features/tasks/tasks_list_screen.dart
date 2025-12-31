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
    // set up the tabs (we have 4 filters)
    _tabController = TabController(length: 4, vsync: this);

    // load the tasks when the screen opens
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
          return Center(
            child: Text(
              'لا توجد مهام حالياً',
              style: const TextStyle(fontFamily: 'Cairo'),
            ),
          );
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
}
