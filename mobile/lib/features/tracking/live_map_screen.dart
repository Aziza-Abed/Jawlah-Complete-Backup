import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import '../../core/theme/app_colors.dart';
import '../../providers/tracking_manager.dart';
import '../../data/models/worker_location.dart';

class LiveMapScreen extends StatefulWidget {
  const LiveMapScreen({super.key});

  @override
  State<LiveMapScreen> createState() => _LiveMapScreenState();
}

class _LiveMapScreenState extends State<LiveMapScreen> {
  final MapController _mapController = MapController();
  WorkerLocation? _selectedWorker;

  static const LatLng _centerPalestine = LatLng(31.9066, 35.2034);

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _connectToHub();
    });
  }

  Future<void> _connectToHub() async {
    final trackingProvider = context.read<TrackingManager>();
    await trackingProvider.startConnecting();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: _buildAppBar(),
      body: Consumer<TrackingManager>(
        builder: (context, trackingProvider, _) {
          return Stack(
            children: [
              // map
              _buildMap(trackingProvider),

              // connection status banner
              if (!trackingProvider.isConnected) _buildDisconnectedBanner(),

              // worker count badge
              Positioned(
                top: 16,
                left: 16,
                child: _buildWorkerCountCard(trackingProvider),
              ),

              // worker details panel
              if (_selectedWorker != null)
                Positioned(
                  bottom: 0,
                  left: 0,
                  right: 0,
                  child: _buildWorkerDetailsPanel(_selectedWorker!),
                ),
            ],
          );
        },
      ),
      floatingActionButton: _buildFloatingActions(),
    );
  }

  PreferredSizeWidget _buildAppBar() {
    return AppBar(
      title: const Text('خريطة العمال المباشرة'),
      actions: [
        Consumer<TrackingManager>(
          builder: (context, trackingProvider, _) {
            return IconButton(
              icon: Icon(
                trackingProvider.isConnected
                    ? Icons.cloud_done
                    : Icons.cloud_off,
                color: trackingProvider.isConnected ? Colors.green : Colors.red,
              ),
              onPressed: () {
                if (!trackingProvider.isConnected) {
                  trackingProvider.startConnecting();
                }
              },
              tooltip: trackingProvider.isConnected
                  ? 'متصل بالخادم'
                  : 'إعادة الاتصال',
            );
          },
        ),
      ],
    );
  }

  Widget _buildMap(TrackingManager trackingProvider) {
    return FlutterMap(
      mapController: _mapController,
      options: MapOptions(
        initialCenter: _centerPalestine,
        initialZoom: 13.0,
        minZoom: 10.0,
        maxZoom: 18.0,
        onTap: (_, __) {
          // deselect worker when tapping map
          setState(() {
            _selectedWorker = null;
          });
        },
      ),
      children: [
        // openStreetMap tile layer
        TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: 'com.albireh.jawlah',
          tileBuilder: (context, child, tile) {
            return ColorFiltered(
              colorFilter: ColorFilter.mode(
                Colors.grey.withOpacity(0.1),
                BlendMode.saturation,
              ),
              child: child,
            );
          },
        ),

        // worker markers
        MarkerLayer(
          markers: trackingProvider.activeWorkers
              .map((worker) => _buildWorkerMarker(worker))
              .toList(),
        ),
      ],
    );
  }

  Marker _buildWorkerMarker(WorkerLocation worker) {
    final isSelected = _selectedWorker?.userId == worker.userId;

    return Marker(
      point: LatLng(worker.latitude, worker.longitude),
      width: isSelected ? 80 : 60,
      height: isSelected ? 80 : 60,
      child: GestureDetector(
        onTap: () {
          setState(() {
            _selectedWorker = worker;
          });
          // center map on selected worker
          _mapController.move(
            LatLng(worker.latitude, worker.longitude),
            15.0,
          );
        },
        child: Stack(
          alignment: Alignment.center,
          children: [
            // accuracy circle
            if (worker.hasAccurateLocation)
              Container(
                width: isSelected ? 80 : 60,
                height: isSelected ? 80 : 60,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: AppColors.primary.withOpacity(0.1),
                  border: Border.all(
                    color: AppColors.primary.withOpacity(0.3),
                    width: 2,
                  ),
                ),
              ),

            // main marker
            Container(
              width: isSelected ? 48 : 40,
              height: isSelected ? 48 : 40,
              decoration: BoxDecoration(
                color: worker.isMoving ? Colors.green : AppColors.primary,
                shape: BoxShape.circle,
                border: Border.all(
                  color: Colors.white,
                  width: 3,
                ),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.3),
                    blurRadius: 8,
                    offset: const Offset(0, 2),
                  ),
                ],
              ),
              child: Icon(
                worker.isMoving ? Icons.directions_walk : Icons.person,
                color: Colors.white,
                size: isSelected ? 24 : 20,
              ),
            ),

            // user ID badge
            if (isSelected)
              Positioned(
                bottom: 0,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 6,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: AppColors.primary, width: 1),
                  ),
                  child: Text(
                    '#${worker.userId}',
                    style: const TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildWorkerCountCard(TrackingManager trackingProvider) {
    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.people, color: AppColors.primary),
            const SizedBox(width: 8),
            Text(
              '${trackingProvider.activeWorkerCount} عامل نشط',
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDisconnectedBanner() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      color: Colors.red,
      child: const Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.cloud_off, color: Colors.white),
          SizedBox(width: 8),
          Text(
            'غير متصل بالخادم - محاولة إعادة الاتصال...',
            style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }

  Widget _buildWorkerDetailsPanel(WorkerLocation worker) {
    return Card(
      margin: EdgeInsets.zero,
      elevation: 8,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // header
            Row(
              children: [
                CircleAvatar(
                  radius: 30,
                  backgroundColor:
                      worker.isMoving ? Colors.green : AppColors.primary,
                  child:
                      const Icon(Icons.person, color: Colors.white, size: 30),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        worker.fullName,
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        worker.role,
                        style: TextStyle(
                          fontSize: 14,
                          color: Colors.grey[600],
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () {
                    setState(() {
                      _selectedWorker = null;
                    });
                  },
                ),
              ],
            ),

            const Divider(height: 24),

            // location details
            _buildDetailRow(
              Icons.location_on,
              'الموقع',
              '${worker.latitude.toStringAsFixed(6)}, ${worker.longitude.toStringAsFixed(6)}',
            ),
            const SizedBox(height: 12),
            _buildDetailRow(
              Icons.access_time,
              'آخر تحديث',
              worker.lastUpdate != null
                  ? _formatDateTime(worker.lastUpdate!)
                  : 'غير متوفر',
            ),
            const SizedBox(height: 12),
            _buildDetailRow(
              Icons.info,
              'الحالة',
              worker.statusText,
            ),
            if (worker.accuracy != null) ...[
              const SizedBox(height: 12),
              _buildDetailRow(
                Icons.my_location,
                'دقة الموقع',
                '${worker.accuracy!.toStringAsFixed(1)} متر',
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildDetailRow(IconData icon, String label, String value) {
    return Row(
      children: [
        Icon(icon, size: 20, color: AppColors.primary),
        const SizedBox(width: 8),
        Text(
          '$label: ',
          style: const TextStyle(
            fontWeight: FontWeight.bold,
            fontSize: 14,
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(fontSize: 14),
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }

  Widget _buildFloatingActions() {
    return Column(
      mainAxisAlignment: MainAxisAlignment.end,
      children: [
        // center on Palestine
        FloatingActionButton.small(
          heroTag: 'center',
          onPressed: () {
            _mapController.move(_centerPalestine, 13.0);
          },
          tooltip: 'توسيط الخريطة',
          child: const Icon(Icons.my_location),
        ),
        const SizedBox(height: 8),

        // refresh
        FloatingActionButton(
          heroTag: 'refresh',
          onPressed: () {
            context.read<TrackingManager>().startConnecting();
          },
          tooltip: 'تحديث',
          child: const Icon(Icons.refresh),
        ),
      ],
    );
  }

  String _formatDateTime(DateTime dateTime) {
    final now = DateTime.now();
    final difference = now.difference(dateTime);

    if (difference.inSeconds < 60) {
      return 'منذ ${difference.inSeconds} ثانية';
    } else if (difference.inMinutes < 60) {
      return 'منذ ${difference.inMinutes} دقيقة';
    } else if (difference.inHours < 24) {
      return 'منذ ${difference.inHours} ساعة';
    } else {
      return 'منذ ${difference.inDays} يوم';
    }
  }

  @override
  void dispose() {
    _mapController.dispose();
    super.dispose();
  }
}
