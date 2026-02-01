import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../data/models/appeal_model.dart';
import '../../providers/appeal_manager.dart';
import '../../core/utils/date_formatter.dart';
import 'appeal_details_screen.dart';

class AppealsListScreen extends StatefulWidget {
  const AppealsListScreen({super.key});

  @override
  State<AppealsListScreen> createState() => _AppealsListScreenState();
}

class _AppealsListScreenState extends State<AppealsListScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _loadAppeals();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadAppeals() async {
    final appealManager = context.read<AppealManager>();
    await appealManager.loadMyAppeals();
  }

  Future<void> _refreshAppeals() async {
    final appealManager = context.read<AppealManager>();
    await appealManager.refreshData();
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('طعوني'),
          centerTitle: true,
          bottom: TabBar(
            controller: _tabController,
            tabs: [
              Tab(
                child: Consumer<AppealManager>(
                  builder: (context, manager, _) => Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text('قيد المراجعة'),
                      if (manager.pendingCount > 0) ...[
                        const SizedBox(width: 8),
                        CircleAvatar(
                          radius: 10,
                          backgroundColor: Colors.orange,
                          child: Text(
                            '${manager.pendingCount}',
                            style: const TextStyle(fontSize: 10, color: Colors.white),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              const Tab(text: 'مقبول'),
              const Tab(text: 'مرفوض'),
            ],
          ),
        ),
        body: Consumer<AppealManager>(
          builder: (context, appealManager, child) {
            if (appealManager.isLoading && appealManager.myAppeals.isEmpty) {
              return const Center(child: CircularProgressIndicator());
            }

            if (appealManager.hasError && appealManager.myAppeals.isEmpty) {
              return _buildErrorView(appealManager.errorMessage);
            }

            if (!appealManager.hasAppeals) {
              return _buildEmptyView();
            }

            return TabBarView(
              controller: _tabController,
              children: [
                _buildAppealsList(appealManager.pendingAppeals),
                _buildAppealsList(appealManager.approvedAppeals),
                _buildAppealsList(appealManager.rejectedAppeals),
              ],
            );
          },
        ),
      ),
    );
  }

  Widget _buildAppealsList(List<AppealModel> appeals) {
    if (appeals.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.inbox, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'لا توجد طعون',
              style: TextStyle(
                fontSize: 18,
                color: Colors.grey[600],
              ),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _refreshAppeals,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: appeals.length,
        separatorBuilder: (context, index) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final appeal = appeals[index];
          return _buildAppealCard(appeal);
        },
      ),
    );
  }

  Widget _buildAppealCard(AppealModel appeal) {
    Color statusColor;
    switch (appeal.statusColor) {
      case 'green':
        statusColor = Colors.green;
        break;
      case 'red':
        statusColor = Colors.red;
        break;
      case 'orange':
        statusColor = Colors.orange;
        break;
      default:
        statusColor = Colors.grey;
    }

    return Card(
      elevation: 2,
      child: InkWell(
        onTap: () {
          Navigator.of(context).push(
            MaterialPageRoute(
              builder: (context) => AppealDetailsScreen(appeal: appeal),
            ),
          );
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header with status badge
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: statusColor.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: statusColor),
                    ),
                    child: Text(
                      appeal.statusName,
                      style: TextStyle(
                        color: statusColor,
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                  ),
                  const Spacer(),
                  Text(
                    DateFormatter.formatDate(appeal.submittedAt),
                    style: TextStyle(
                      fontSize: 12,
                      color: Colors.grey[600],
                    ),
                  ),
                ],
              ),
              const Divider(height: 24),

              // Task title
              if (appeal.entityTitle != null) ...[
                Row(
                  children: [
                    const Icon(Icons.assignment, size: 20, color: Colors.blue),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        appeal.entityTitle!,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
              ],

              // Distance info if available
              if (appeal.distanceMeters != null) ...[
                Row(
                  children: [
                    Icon(Icons.place, size: 20, color: Colors.grey[600]),
                    const SizedBox(width: 8),
                    Text(
                      'المسافة: ${appeal.distanceMeters} متر',
                      style: TextStyle(
                        fontSize: 14,
                        color: Colors.grey[700],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
              ],

              // Explanation preview
              Text(
                appeal.workerExplanation,
                style: TextStyle(
                  fontSize: 14,
                  color: Colors.grey[800],
                ),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),

              // Review info if reviewed
              if (appeal.reviewedAt != null) ...[
                const Divider(height: 24),
                Row(
                  children: [
                    Icon(
                      appeal.isApproved ? Icons.check_circle : Icons.cancel,
                      size: 16,
                      color: statusColor,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'تمت المراجعة بواسطة ${appeal.reviewedByName ?? "المشرف"}',
                        style: TextStyle(
                          fontSize: 12,
                          color: Colors.grey[600],
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildEmptyView() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.question_answer, size: 80, color: Colors.grey[400]),
          const SizedBox(height: 16),
          Text(
            'لا توجد طعون',
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: Colors.grey[700],
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'سيتم عرض طعونك هنا',
            style: TextStyle(
              fontSize: 16,
              color: Colors.grey[600],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorView(String? errorMessage) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.error_outline, size: 80, color: Colors.red),
          const SizedBox(height: 16),
          Text(
            errorMessage ?? 'حدث خطأ',
            style: const TextStyle(fontSize: 16, color: Colors.red),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 16),
          ElevatedButton(
            onPressed: _loadAppeals,
            child: const Text('إعادة المحاولة'),
          ),
        ],
      ),
    );
  }
}
