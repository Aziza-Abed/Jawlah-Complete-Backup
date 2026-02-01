import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../../core/theme/app_colors.dart';

/// Read-only map view showing where a task is located
/// Workers use this to see exactly where they need to go
/// Uses OpenStreetMap (free, no API key required)
class TaskLocationMapView extends StatefulWidget {
  final double latitude;
  final double longitude;
  final String? taskTitle;

  const TaskLocationMapView({
    super.key,
    required this.latitude,
    required this.longitude,
    this.taskTitle,
  });

  @override
  State<TaskLocationMapView> createState() => _TaskLocationMapViewState();
}

class _TaskLocationMapViewState extends State<TaskLocationMapView> {
  late final MapController _mapController;

  @override
  void initState() {
    super.initState();
    _mapController = MapController();
  }

  @override
  Widget build(BuildContext context) {
    final taskLocation = LatLng(widget.latitude, widget.longitude);

    return Scaffold(
      appBar: AppBar(
        title: Text(
          widget.taskTitle ?? 'موقع المهمة',
          style: const TextStyle(
            fontFamily: 'Cairo',
            fontWeight: FontWeight.bold,
          ),
        ),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      body: Column(
        children: [
          // Info banner
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(16),
            color: AppColors.info.withOpacity(0.1),
            child: Row(
              children: [
                const Icon(
                  Icons.location_on,
                  color: AppColors.primary,
                  size: 24,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    'هذا هو الموقع الدقيق للمهمة كما حدده المشرف',
                    style: const TextStyle(
                      fontFamily: 'Cairo',
                      fontSize: 14,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ),
              ],
            ),
          ),

          // OpenStreetMap (read-only)
          Expanded(
            child: Stack(
              children: [
                FlutterMap(
                  mapController: _mapController,
                  options: MapOptions(
                    initialCenter: taskLocation,
                    initialZoom: 17.0,
                    minZoom: 5.0,
                    maxZoom: 18.0,
                    // Allow user to explore the map
                    interactionOptions: const InteractionOptions(
                      flags: InteractiveFlag.all,
                    ),
                  ),
                  children: [
                    // OpenStreetMap tiles
                    TileLayer(
                      urlTemplate:
                          'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                      userAgentPackageName: 'com.example.followup',
                      // Respectful tile loading
                      maxNativeZoom: 19,
                    ),

                    // Task location marker
                    MarkerLayer(
                      markers: [
                        Marker(
                          point: taskLocation,
                          width: 50,
                          height: 50,
                          child: const Icon(
                            Icons.location_on,
                            color: Colors.red,
                            size: 50,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _mapController.dispose();
    super.dispose();
  }
}
